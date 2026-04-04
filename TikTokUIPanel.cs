using UnityEngine;

namespace TikTokGiftsToEnemies
{
    public class TikTokUIPanel : MonoBehaviour
    {
        private bool _showPanel = true;
        private Rect _windowRect = new Rect(20, 20, 380, 320);
        private int _tab = 0;
        private Vector2 _configScroll;
        private Vector2 _debugScroll;

        // Connect tab
        private string _usernameInput = "";

        // Simulate
        private string _simPrefabInput = "";
        private string _simCountInput  = "1";

        // Config tab buffers
        private string _giftRules;
        private string _coinRules;
        private string _likeRules;
        private string _followRules;
        private bool   _notifications;
        private int    _spawnModeIndex; // 0=NightAware 1=Immediate 2=Queue

        // Debug tab
        private string   _debugCountInput = "1";
        private string[] _debugEnemyNames = System.Array.Empty<string>();

        private static readonly string[] SpawnModeLabels = { "NightAware (default)", "Immediate", "Queue" };
        private static readonly string[] SpawnModeValues = { "NightAware", "Immediate", "Queue" };

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _usernameInput = PluginConfig.TikTokUsername.Value;
            LoadConfigBuffers();
        }

        void LoadConfigBuffers()
        {
            _giftRules     = PluginConfig.GiftRules.Value;
            _coinRules     = PluginConfig.CoinRules.Value;
            _likeRules     = PluginConfig.LikeRules.Value;
            _followRules   = PluginConfig.FollowRules.Value;
            _notifications = PluginConfig.ShowOnScreenNotifications.Value;
            string mode    = PluginConfig.SpawnMode.Value ?? "NightAware";
            _spawnModeIndex = System.Array.FindIndex(SpawnModeValues,
                v => v.Equals(mode, System.StringComparison.OrdinalIgnoreCase));
            if (_spawnModeIndex < 0) _spawnModeIndex = 0;
        }

        void SaveConfig()
        {
            PluginConfig.GiftRules.Value                  = _giftRules;
            PluginConfig.CoinRules.Value                  = _coinRules;
            PluginConfig.LikeRules.Value                  = _likeRules;
            PluginConfig.FollowRules.Value                = _followRules;
            PluginConfig.ShowOnScreenNotifications.Value  = _notifications;
            PluginConfig.SpawnMode.Value                  = SpawnModeValues[_spawnModeIndex];
            
            TikTokGiftsPlugin.Instance.Config.Save();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                _showPanel = !_showPanel;

            // While any IMGUI text field has keyboard focus, swallow game input so
            // typing doesn't trigger movement, attacks, or other key bindings.
            if (_showPanel && GUIUtility.keyboardControl != 0)
                Input.ResetInputAxes();
        }

        void OnGUI()
        {
            if (!_showPanel) return;

            // Change color if connected
            var oldColor = GUI.backgroundColor;
            if (TikTokGiftsPlugin.Instance.TikTokManager?.IsConnected == true)
            {
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f, 1f); // Nice green
            }

            _windowRect = GUILayout.Window(99, _windowRect, DrawWindow, "TikTok Gifts to Enemies");

            GUI.backgroundColor = oldColor;
        }


        void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tab == 0, "Connect", GUI.skin.button)) _tab = 0;
            if (GUILayout.Toggle(_tab == 1, "Config",  GUI.skin.button)) _tab = 1;
            if (GUILayout.Toggle(_tab == 2, "Debug",   GUI.skin.button)) { if (_tab != 2) RefreshDebugEnemyList(); _tab = 2; }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if      (_tab == 0) DrawConnectTab();
            else if (_tab == 1) DrawConfigTab();
            else                DrawDebugTab();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        void DrawConnectTab()
        {
            GUILayout.Label("TikTok Username:");
            string newUser = GUILayout.TextField(_usernameInput);
            if (newUser != _usernameInput)
            {
                _usernameInput = newUser;
                PluginConfig.TikTokUsername.Value = newUser;
            }

            GUILayout.Space(8);

            if (TikTokConnectionManager.Instance != null)
            {
                GUILayout.Label($"Status: {TikTokConnectionManager.Instance.ConnectionStatus}");
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Connect"))    TikTokConnectionManager.Instance.Connect(_usernameInput);
                if (GUILayout.Button("Disconnect")) TikTokConnectionManager.Instance.Disconnect();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);
        }

        void DrawConfigTab()
        {
            _configScroll = GUILayout.BeginScrollView(_configScroll, GUILayout.Height(240));

            GUILayout.Label("── Gift Rules  (GiftName:SpawnIndex:Count) ──");
            GUILayout.Label("e.g.  Rose:0:1;Lion:1:2;Universe:2:5", SmallStyle());
            _giftRules = GUILayout.TextArea(_giftRules, GUILayout.MinHeight(50));

            GUILayout.Space(8);

            GUILayout.Label("── Coin Rules  (MinDiamonds:SpawnIndex:Count) ──");
            GUILayout.Label("e.g.  5:0:1;50:1:3;200:2:5   (highest match wins)", SmallStyle());
            _coinRules = GUILayout.TextArea(_coinRules, GUILayout.MinHeight(50));

            GUILayout.Space(8);

            GUILayout.Label("── Like Rules  (LikesPerSpawn:PrefabName:Count) ──");
            GUILayout.Label("e.g.  100:E Melee:1;500:E Elite:1", SmallStyle());
            _likeRules = GUILayout.TextArea(_likeRules, GUILayout.MinHeight(50));

            GUILayout.Space(8);

            GUILayout.Label("── Follow Rules  (FollowsPerSpawn:PrefabName:Count) ──");
            GUILayout.Label("e.g.  1:E Spider Small:1", SmallStyle());
            _followRules = GUILayout.TextArea(_followRules, GUILayout.MinHeight(50));

            GUILayout.Space(8);

            _notifications = GUILayout.Toggle(_notifications, "Show on-screen notifications");

            GUILayout.Space(8);
            GUILayout.Label("── Spawn Mode ──");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < SpawnModeLabels.Length; i++)
            {
                if (GUILayout.Toggle(_spawnModeIndex == i, SpawnModeLabels[i], GUI.skin.button))
                    _spawnModeIndex = i;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("Tip: run the game once to generate interactive_spawns.json\nin BepInEx/config — open it to see monster names per wave.", SmallStyle());

            GUILayout.EndScrollView();

            GUILayout.Space(4);
            if (GUILayout.Button("Save")) SaveConfig();
        }

        void RefreshDebugEnemyList()
        {
            // Merge: current level waves + everything in the persistent prefab cache
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            if (EnemySpawner.instance?.waves != null)
            {
                foreach (var wave in EnemySpawner.instance.waves)
                {
                    if (wave?.spawns == null) continue;
                    foreach (var s in wave.spawns)
                        if (s?.enemyPrefab != null) seen.Add(s.enemyPrefab.name);
                }
            }

            // CacheCount tells us how many are cached; we show all of them
            // by pulling names from spawns.json via the same path the configurator uses
            // — but the simplest in-game approach is just the cache keys exposed via CacheCount.
            // We let SpawnOrchestrator surface them through a new helper.
            foreach (var name in SpawnOrchestrator.CachedEnemyNames)
                seen.Add(name);

            var list = new System.Collections.Generic.List<string>(seen);
            list.Sort(System.StringComparer.OrdinalIgnoreCase);
            _debugEnemyNames = list.ToArray();
        }

        void DrawDebugTab()
        {
            // Scanner status
            string scanStatus = PrefabScanner.Instance?.Status ?? "Scanner not running";
            GUILayout.Label(scanStatus, SmallStyle());
            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Count:", GUILayout.Width(44));
            _debugCountInput = GUILayout.TextField(_debugCountInput, GUILayout.Width(36));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh List", GUILayout.Width(90))) RefreshDebugEnemyList();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("── Manual Spawn ─────────────────────────────────");
            GUILayout.Label("Prefab name:", SmallStyle());
            _simPrefabInput = GUILayout.TextField(_simPrefabInput);
            if (GUILayout.Button("Spawn By Name"))
            {
                int.TryParse(_debugCountInput, out int cnt);
                if (cnt < 1) cnt = 1;
                if (!string.IsNullOrEmpty(_simPrefabInput))
                    SpawnOrchestrator.Instance?.SimulateSpawn(_simPrefabInput.Trim(), cnt);
            }

            GUILayout.Space(8);
            if (_debugEnemyNames.Length == 0)
            {
                GUILayout.Label("No enemies cached yet — wait for the background scan or load a level.", SmallStyle());
                return;
            }

            GUILayout.Label($"{_debugEnemyNames.Length} enemies available — click to spawn:", SmallStyle());
            GUILayout.Space(2);

            _debugScroll = GUILayout.BeginScrollView(_debugScroll, GUILayout.Height(200));
            foreach (var name in _debugEnemyNames)
            {
                if (GUILayout.Button(name))
                {
                    int.TryParse(_debugCountInput, out int cnt);
                    if (cnt < 1) cnt = 1;
                    SpawnOrchestrator.Instance?.SimulateSpawn(name, cnt);
                }
            }
            GUILayout.EndScrollView();
        }

        GUIStyle SmallStyle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 10;
            s.normal.textColor = Color.gray;
            return s;
        }
    }
}
