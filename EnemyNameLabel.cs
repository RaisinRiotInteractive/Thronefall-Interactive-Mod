using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Pathfinding;

namespace TikTokGiftsToEnemies
{
    public class EnemyNameLabel : MonoBehaviour
    {
        private Transform _textTransform;
        private Transform _imageTransform;
        private Vector3   _cachedPosition;

        public void Init(string senderName, string profilePicUrl)
        {
            TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] Initializing for {senderName}, PicURL: {profilePicUrl ?? "null"}");
            // ── Name text ──────────────────────────────────────────────────────
            var textGo = new GameObject("_NameText");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            var tm = textGo.AddComponent<TextMesh>();
            tm.text          = "@" + senderName;
            tm.characterSize = 0.22f;
            tm.fontSize      = 56;
            tm.color         = new Color(1f, 0.85f, 0f);
            tm.anchor        = TextAnchor.LowerCenter;
            tm.alignment     = TextAlignment.Center;
            _textTransform   = textGo.transform;

            // ── Profile picture quad ───────────────────────────────────────────
            if (!string.IsNullOrEmpty(profilePicUrl))
            {
                TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] Creating quad for {senderName}...");
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "_ProfilePic";
                quad.transform.SetParent(transform, false);
                // Position it above the name text
                quad.transform.localPosition = new Vector3(0f, 5.0f, 0f);
                quad.transform.localScale    = new Vector3(3.0f, 3.0f, 1f);

                // Remove the collider so it doesn't interfere with gameplay
                var col = quad.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Try to find a suitable shader
                var rend = quad.GetComponent<Renderer>();
                
                // Try Sprites/Default first as it's very reliable for flat textures
                var shader = Shader.Find("Sprites/Default")
                          ?? Shader.Find("Unlit/Texture") 
                          ?? Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Standard");
                
                if (shader != null)
                    TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] Using shader: {shader.name}");
                else
                    TikTokGiftsPlugin.Instance.Logger.LogWarning("[EnemyLabel] Could not find any suitable shader!");

                rend.material = new Material(shader ?? Shader.Find("Hidden/InternalErrorShader")) { color = Color.white };
                
                // For URP, we often need to set the texture on _BaseMap
                if (shader != null && shader.name.Contains("Universal"))
                {
                    rend.material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                }

                _imageTransform = quad.transform;
                StartCoroutine(DownloadAndApply(profilePicUrl, rend));
            }
            else
            {
                TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] No profile pic URL for {senderName}");
            }
        }

        private IEnumerator DownloadAndApply(string url, Renderer rend)
        {
            if (string.IsNullOrEmpty(url)) yield break;
            if (rend == null) yield break;
            
            TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] Requesting pic: {url}");
            
            // Standard UnityWebRequest instead of GetTexture to handle raw data manually
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
            
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                byte[] data = req.downloadHandler.data;
                if (data != null && data.Length > 0)
                {
                    TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] Received {data.Length} bytes.");
                    
                    // Create texture from raw bytes
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(data))
                    {
                        TikTokGiftsPlugin.Instance.Logger.LogInfo($"[EnemyLabel] Successfully decoded {tex.width}x{tex.height} texture.");
                        if (rend != null && rend.material != null)
                        {
                            rend.material.mainTexture = tex;
                            if (rend.material.HasProperty("_BaseMap"))
                                rend.material.SetTexture("_BaseMap", tex);
                        }
                    }
                    else
                    {
                        TikTokGiftsPlugin.Instance.Logger.LogWarning("[EnemyLabel] LoadImage failed to decode the data (unsupported format).");
                        Destroy(tex);
                    }
                }
                else
                {
                    TikTokGiftsPlugin.Instance.Logger.LogWarning("[EnemyLabel] Downloaded data is empty.");
                }
            }
            else
            {
                TikTokGiftsPlugin.Instance.Logger.LogWarning($"[EnemyLabel] Download failed: {req.error}");
                if (_imageTransform != null)
                    _imageTransform.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            _cachedPosition = transform.position;
            if (Camera.main == null) return;
            
            // Billboard rotation: face the camera plane
            var rot = Camera.main.transform.rotation;
            
            if (_textTransform != null) 
                _textTransform.rotation = rot;
                
            if (_imageTransform != null) 
                // Quads face -Z, so we rotate 180 to face the camera
                _imageTransform.rotation = rot * Quaternion.Euler(0, 180, 0);
        }

        void OnDestroy()
        {
            // Guard: only run during active gameplay, not on scene unload
            if (EnemySpawner.instance == null) return;

            // 20 % chance to drop a coin
            if (Random.value > 0.20f) return;

            TryDropCoin();
        }

        void TryDropCoin()
        {
            try
            {
                // Capture the current position immediately
                var currentPos = transform.position;
                
                var spawner = FindObjectOfType<CoinSpawner>();
                var playerInt = FindObjectOfType<PlayerInteraction>();

                if (spawner == null) return;

                // ── Ground positioning ───────────────────────────────────────────
                // Start from way above the current position to find the real ground
                var spawnPos = currentPos;
                bool grounded = false;

                // Raycast down from 10 units above to find the highest non-enemy surface
                RaycastHit[] hits = Physics.RaycastAll(spawnPos + Vector3.up * 10f, Vector3.down, 20f);
                float bestY = -1000f;
                foreach (var h in hits)
                {
                    if (h.collider.isTrigger) continue;
                    if (h.transform.IsChildOf(transform)) continue;
                    if (h.collider.gameObject.layer == gameObject.layer) continue;
                    
                    // We want the highest surface that isn't significantly above where we died
                    if (h.point.y > bestY && h.point.y < spawnPos.y + 1.5f)
                    {
                        bestY = h.point.y;
                    }
                }

                if (bestY > -500f)
                {
                    spawnPos.y = bestY;
                    grounded = true;
                }
                else if (AstarPath.active != null)
                {
                    var info = AstarPath.active.GetNearest(spawnPos, NNConstraint.None);
                    if (info.node != null)
                    {
                        spawnPos = info.position;
                        grounded = true;
                    }
                }

                // ── Spawning ─────────────────────────────────────────────────────
                // Resolve the coinPrefab for manual spawning if TriggerCoinSpawn doesn't work or isn't positional
                if (!_prefabResolved)
                {
                    _prefabResolved = true;
                    var field = typeof(CoinSpawner).GetField("coinPrefab",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public   |
                        System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                        _coinPrefab = field.GetValue(spawner) as GameObject;
                }

                if (_coinPrefab == null) return;

                // ── Spawning ─────────────────────────────────────────────────────
                // Try to use the game's TriggerCoinSpawn so the count actually goes up
                bool gameSpawned = false;
                if (spawner != null && playerInt != null)
                {
                    try
                    {
                        var triggerMethod = spawner.GetType().GetMethod("TriggerCoinSpawn",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public   |
                            System.Reflection.BindingFlags.NonPublic);
                        
                        if (triggerMethod != null)
                        {
                            // Move the spawner temporarily to the death position if it uses its transform
                            // (Risk: might interfere if spawner is a singleton that other things use simultaneously)
                            // But usually spawner methods take a position or spawn at player.
                            // Let's just try calling it first.
                            triggerMethod.Invoke(spawner, new object[] { 1, playerInt });
                            gameSpawned = true;
                            TikTokGiftsPlugin.Instance.Logger.LogInfo("[CoinDrop] Invoked CoinSpawner.TriggerCoinSpawn(1, player)");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        TikTokGiftsPlugin.Instance.Logger.LogWarning($"[CoinDrop] TriggerCoinSpawn failed: {ex.Message}");
                    }
                }

                // If game spawn didn't happen or we want to ensure it drops exactly here:
                // Note: TriggerCoinSpawn might spawn at player. If so, we still want our visual drop.
                // If the user said "coin count not going up", then TriggerCoinSpawn is essential.

                // Instantiate coin manually as a fallback or for visual effect
                var coin = UnityEngine.Object.Instantiate(_coinPrefab, spawnPos + Vector3.up * 0.1f, Quaternion.identity);
                if (coin != null)
                {
                    coin.SetActive(true);
                    coin.layer = 0;
                    coin.AddComponent<CoinMover>();

                    var rb = coin.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.WakeUp();
                    }

                    // Try to trigger the "Spawn" logic
                    foreach (var comp in coin.GetComponents<MonoBehaviour>())
                    {
                        var method = comp.GetType().GetMethod("Spawn", 
                            System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.Public   | 
                            System.Reflection.BindingFlags.NonPublic);
                        method?.Invoke(comp, null);
                    }
                }

                TikTokGiftsPlugin.Instance.Logger.LogInfo($"[CoinDrop] Coin dropped at {spawnPos}");
            }
            catch (System.Exception ex)
            {
                TikTokGiftsPlugin.Instance.Logger.LogWarning($"[CoinDrop] {ex.Message}");
            }
        }

        private static GameObject _coinPrefab;
        private static bool       _prefabResolved;
    }

    /// <summary>
    /// Component added to dropped coins to move them towards the player.
    /// </summary>
    public class CoinMover : MonoBehaviour
    {
        private Transform _playerTransform;
        private float     _startTime;
        private float     _speed = 12f;
        private float     _delay = 0.4f; // Wait a bit before flying
        private bool      _collecting = false;

        void Start()
        {
            _startTime = Time.time;
            
            // Try common Thronefall player patterns
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                _playerTransform = playerGo.transform;
            }
            else
            {
                // Fallback: search for something that looks like a player movement script
                var pm = FindObjectOfType<PlayerMovement>() as MonoBehaviour;
                if (pm != null) _playerTransform = pm.transform;
            }

            // Set to a layer often used for pickable items if Default (0) isn't working
            // In many games, layer 10 or 11 is used for gold/loot.
            // We'll try to find a collider and make it a trigger just in case
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                c.isTrigger = true;
            }
        }

        void Update()
        {
            if (_playerTransform == null || Time.time < _startTime + _delay || _collecting) return;

            // Target the player's center
            var targetPos = _playerTransform.position + Vector3.up * 0.8f;
            float dist = Vector3.Distance(transform.position, targetPos);

            // Move towards player
            transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);

            // If we are very close, try to trigger the game's collection logic
            if (dist < 0.6f)
            {
                _collecting = true;
                TriggerCollection();
            }
        }

        private void TriggerCollection()
        {
            // Try to find a 'Collect' or 'PickUp' method on any component
            foreach (var comp in GetComponents<MonoBehaviour>())
            {
                // Common method names for loot collection
                string[] methodNames = { "Collect", "PickUp", "OnPickedUp", "OnCollected", "Execute" };
                foreach (var name in methodNames)
                {
                    // Try one that takes a GameObject or Transform (the player)
                    var methodWithArg = comp.GetType().GetMethod(name, 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public   | 
                        System.Reflection.BindingFlags.NonPublic,
                        null, new System.Type[] { typeof(GameObject) }, null);

                    if (methodWithArg != null && _playerTransform != null)
                    {
                        try {
                            methodWithArg.Invoke(comp, new object[] { _playerTransform.gameObject });
                            TikTokGiftsPlugin.Instance.Logger.LogInfo($"[CoinDrop] Invoked {name}(Player) on {comp.GetType().Name}");
                            goto Done;
                        } catch { }
                    }

                    // Try the parameterless one
                    var method = comp.GetType().GetMethod(name, 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public   | 
                        System.Reflection.BindingFlags.NonPublic,
                        null, System.Type.EmptyTypes, null);
                    
                    if (method != null)
                    {
                        try {
                            method.Invoke(comp, null);
                            TikTokGiftsPlugin.Instance.Logger.LogInfo($"[CoinDrop] Invoked {name}() on {comp.GetType().Name}");
                            goto Done;
                        } catch { }
                    }
                }
            }

        Done:
            // If it's still here after a frame, just destroy it to at least clear the screen
            Destroy(gameObject, 0.05f);
        }
    }
}

