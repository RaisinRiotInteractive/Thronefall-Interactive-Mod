using System.Collections;
using UnityEngine;

namespace TikTokGiftsToEnemies
{
    /// <summary>
    /// Runs once per game launch in the background (while the player is on the level
    /// select / main menu) to cache enemy prefabs from every level scene.
    /// This means all enemies are available to spawn without the player ever needing
    /// to manually visit the level that normally contains them.
    /// </summary>
    public class PrefabScanner : MonoBehaviour
    {
        public static PrefabScanner Instance { get; private set; }
        public string Status { get; private set; } = "Scan pending...";

        void Awake()
        {
            Instance = this;
        }

        IEnumerator Start()
        {
            // Let the game fully initialise before scanning
            yield return new WaitForSeconds(2f);

            ScanResidentAssets();
        }

        // ── Pass 1: resident assets ──────────────────────────────────────────────
        void ScanResidentAssets()
        {
            int added = 0;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                // Assets (prefabs) have no valid scene; skip live instances
                if (go == null || go.scene.IsValid()) continue;

                var n = go.name;
                if (n.Length > 2 && n[0] == 'E' && n[1] == ' ')
                    if (SpawnOrchestrator.AddToCache(n, go)) added++;
            }

            if (added > 0)
                Log($"Pass 1: found {added} enemy prefabs already in memory " +
                    $"(total cached: {SpawnOrchestrator.CacheCount})");
        }

        static void Log(string msg) =>
            TikTokGiftsPlugin.Instance.Logger.LogInfo($"[PrefabScanner] {msg}");
    }
}
