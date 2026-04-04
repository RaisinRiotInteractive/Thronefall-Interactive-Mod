using System.Collections.Generic;
using System.IO;
using Pathfinding;
using UnityEngine;

namespace TikTokGiftsToEnemies
{
    public class SpawnOrchestrator
    {
        public static SpawnOrchestrator Instance { get; private set; }

        private Queue<EnemySpawnRequest> _pendingQueue = new Queue<EnemySpawnRequest>();
        private bool _spawnsDumped = false;

        // Persists prefab references across level loads so any enemy ever seen can be
        // spawned even when the level that normally contains it isn't loaded.
        // Unity will not garbage-collect these assets while we hold a managed reference.
        private static readonly Dictionary<string, GameObject> _prefabCache =
            new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);

        public SpawnOrchestrator() { Instance = this; }

        public void HandleSpawnRequest(EnemySpawnRequest request)
        {
            string mode = PluginConfig.SpawnMode.Value ?? "NightAware";

            if (mode.Equals("Immediate", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!SpawnImmediately(request))
                    EnqueueForNextWave(request);
            }
            else if (mode.Equals("Queue", System.StringComparison.OrdinalIgnoreCase))
            {
                EnqueueForNextWave(request);
            }
            else // NightAware (default)
            {
                bool isNight = DayNightCycle.Instance != null &&
                               DayNightCycle.Instance.CurrentTimestate == DayNightCycle.Timestate.Night;
                if (isNight)
                {
                    if (!SpawnImmediately(request))
                        EnqueueForNextWave(request);
                }
                else
                {
                    EnqueueForNextWave(request);
                }
            }
        }

        private bool SpawnImmediately(EnemySpawnRequest request)
        {
            try
            {
                if (EnemySpawner.instance == null || EnemySpawner.instance.waves == null)
                {
                    TikTokGiftsPlugin.Instance.Logger.LogWarning("EnemySpawner not ready.");
                    return false;
                }

                TryDumpSpawnInfo();

                // ── Scan current level's waves ────────────────────────────────────
                // Always refresh the cache with whatever this level has, and keep any
                // spawn object handy so we have a valid spawn line for positioning.
                Spawn matchedSpawn  = null;
                Spawn fallbackSpawn = null;

                foreach (var wave in EnemySpawner.instance.waves)
                {
                    if (wave.spawns == null) continue;
                    foreach (var s in wave.spawns)
                    {
                        if (s.enemyPrefab == null) continue;

                        // Add to persistent cache (new entries only — don't overwrite valid refs)
                        if (!_prefabCache.ContainsKey(s.enemyPrefab.name))
                            _prefabCache[s.enemyPrefab.name] = s.enemyPrefab;

                        // Keep a fallback spawn position from whatever is available this level
                        if (fallbackSpawn == null) fallbackSpawn = s;

                        if (s.enemyPrefab.name.Equals(request.PrefabName, System.StringComparison.OrdinalIgnoreCase))
                            matchedSpawn = s;
                    }
                }

                // ── Resolve prefab + spawn position ──────────────────────────────
                GameObject prefabToSpawn;
                Spawn      spawnForPosition;

                if (matchedSpawn != null)
                {
                    // Enemy is native to this level — use its own spawn line
                    prefabToSpawn   = matchedSpawn.enemyPrefab;
                    spawnForPosition = matchedSpawn;
                }
                else if (_prefabCache.TryGetValue(request.PrefabName, out var cached) &&
                         cached != null && fallbackSpawn != null)
                {
                    // Enemy was seen in a previous level — use cached prefab with a
                    // spawn line from the current level for positioning
                    prefabToSpawn   = cached;
                    spawnForPosition = fallbackSpawn;
                    TikTokGiftsPlugin.Instance.Logger.LogInfo(
                        $"'{request.PrefabName}' not in current level — spawning from cache");
                }
                else
                {
                    TikTokGiftsPlugin.Instance.Logger.LogWarning(
                        $"No prefab found for '{request.PrefabName}'. " +
                        $"Play a level that contains this enemy first so it gets cached.");
                    return false;
                }

                // ── Instantiate ───────────────────────────────────────────────────
                for (int i = 0; i < request.Count; i++)
                {
                    var go = UnityEngine.Object.Instantiate(
                        prefabToSpawn,
                        spawnForPosition.GetRandomPointOnSpawnLine(false, NNConstraint.Walkable),
                        Quaternion.identity) as GameObject;

                    if (go != null)
                        go.AddComponent<EnemyNameLabel>().Init(request.SenderName, request.ProfilePicUrl);
                }

                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"Spawned {request.Count}x {request.PrefabName} ({request.GiftName}) for {request.SenderName}");

                NotificationManager.Instance?.Show(
                    $"@{request.SenderName} spawned {request.Count} enemies!");

                return true;
            }
            catch (System.Exception ex)
            {
                TikTokGiftsPlugin.Instance.Logger.LogError($"Error in SpawnImmediately: {ex.Message}");
                return false;
            }
        }

        private void EnqueueForNextWave(EnemySpawnRequest request)
        {
            _pendingQueue.Enqueue(request);
            TikTokGiftsPlugin.Instance.Logger.LogInfo(
                $"Queued {request.Count}x {request.PrefabName} from {request.SenderName} for next wave");
            NotificationManager.Instance?.Show(
                $"@{request.SenderName} queued enemies for next wave!");
        }

        public void FlushQueue()
        {
            if (_pendingQueue.Count == 0) return;

            TikTokGiftsPlugin.Instance.Logger.LogInfo($"Flushing queue of {_pendingQueue.Count} requests");
            int count = _pendingQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var req = _pendingQueue.Dequeue();
                if (!SpawnImmediately(req))
                    _pendingQueue.Enqueue(req);
            }
        }

        public void Tick() { } // reserved for future per-frame work

        public void ResetSpawnDump() { _spawnsDumped = false; }

        public static int      CacheCount        => _prefabCache.Count;
        public static System.Collections.Generic.IEnumerable<string> CachedEnemyNames
            => _prefabCache.Keys;

        public static bool AddToCache(string name, GameObject go)
        {
            if (string.IsNullOrEmpty(name) || go == null || _prefabCache.ContainsKey(name)) return false;
            _prefabCache[name] = go;
            return true;
        }

        // Called by the EnemySpawner patch on every level load — builds the cache
        // without needing a gift to trigger it.
        public void CachePrefabsFromSpawner(EnemySpawner spawner)
        {
            if (spawner?.waves == null) return;
            int added = 0;
            foreach (var wave in spawner.waves)
            {
                if (wave?.spawns == null) continue;
                foreach (var s in wave.spawns)
                {
                    if (s?.enemyPrefab != null && !_prefabCache.ContainsKey(s.enemyPrefab.name))
                    {
                        _prefabCache[s.enemyPrefab.name] = s.enemyPrefab;
                        added++;
                    }
                }
            }
            if (added > 0)
                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"[Cache] Added {added} new enemy prefab(s) from {spawner.name} — total cached: {_prefabCache.Count}");
        }

        public void SimulateSpawn(string prefabName, int count)
        {
            HandleSpawnRequest(new EnemySpawnRequest
            {
                PrefabName    = prefabName,
                Count         = count,
                SenderName    = "Simulation",
                GiftName      = "Test",
                TotalDiamonds = 0,
                ProfilePicUrl = "https://raw.githubusercontent.com/BepinEx/BepInEx/master/icon.png" // Sample image
            });
        }

        // Dumps available monster names per wave to BepInEx/config/interactive_spawns.json
        private void TryDumpSpawnInfo()
        {
            if (_spawnsDumped) return;
            if (EnemySpawner.instance?.waves == null) return;

            try
            {
                string path = System.IO.Path.Combine(
                    BepInEx.Paths.ConfigPath,
                    "interactive_spawns.json");

                // Seed with any names already persisted from previous sessions / manual edits
                var allNames = new System.Collections.Generic.HashSet<string>(
                    System.StringComparer.OrdinalIgnoreCase);

                if (File.Exists(path))
                {
                    var existing = File.ReadAllText(path);
                    var m = System.Text.RegularExpressions.Regex.Match(
                        existing, "\"allEnemies\"\\s*:\\s*\\[([^\\]]*?)\\]");
                    if (m.Success)
                    {
                        foreach (var n in m.Groups[1].Value.Split(','))
                        {
                            var name = n.Trim().Trim('"');
                            if (name.Length > 0) allNames.Add(name);
                        }
                    }
                }

                // Add everything from the current level's spawner
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"waves\": {");

                var waves = EnemySpawner.instance.waves;
                for (int w = 0; w < waves.Count; w++)
                {
                    sb.Append($"    \"{w}\": [");
                    var spawns = waves[w].spawns;
                    for (int s = 0; s < spawns.Count; s++)
                    {
                        string prefabName = spawns[s].enemyPrefab != null
                            ? spawns[s].enemyPrefab.name
                            : "unknown";
                        allNames.Add(prefabName);
                        sb.Append($"\"{prefabName}\"");
                        if (s < spawns.Count - 1) sb.Append(", ");
                    }
                    sb.Append("]");
                    if (w < waves.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }

                sb.AppendLine("  },");
                sb.Append("  \"allEnemies\": [");
                var nameList = new List<string>(allNames);
                nameList.Sort(System.StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < nameList.Count; i++)
                {
                    sb.Append($"\"{nameList[i]}\"");
                    if (i < nameList.Count - 1) sb.Append(", ");
                }
                sb.AppendLine("]");
                sb.AppendLine("}");

                File.WriteAllText(path, sb.ToString());
                _spawnsDumped = true;

                TikTokGiftsPlugin.Instance.Logger.LogInfo(
                    $"Spawn info written to {path} ({allNames.Count} total enemies)");
            }
            catch (System.Exception ex)
            {
                TikTokGiftsPlugin.Instance.Logger.LogWarning(
                    $"Could not write spawn info: {ex.Message}");
            }
        }
    }
}
