using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TikTokGiftsToEnemies
{
    [BepInPlugin("com.raisinriotinteractive.thronefall.interactive", "Thronefall Interactive Mod", "1.0.0")]
    public class TikTokGiftsPlugin : BaseUnityPlugin
    {
        public static TikTokGiftsPlugin Instance { get; private set; }
        public new ManualLogSource Logger => base.Logger;
        public TikTokConnectionManager TikTokManager => _connectionManager;

        private Harmony _harmony;
        private TikTokConnectionManager _connectionManager;

        private void Awake()
        {
            Instance = this;

            // 1. Bind Config
            PluginConfig.Init(Config);

            // 2. Map & Orchestrator
            var mapper = new GiftEnemyMapper();
            var orchestrator = new SpawnOrchestrator();

            // 3. Setup Connection Manager
            _connectionManager = new TikTokConnectionManager(mapper, orchestrator);

            // 4. Setup Managers in Unity
            var managerGo = new GameObject("TikTokGiftsManagers");
            DontDestroyOnLoad(managerGo);
            managerGo.AddComponent<NotificationManager>();
            managerGo.AddComponent<TikTokUIPanel>();
            managerGo.AddComponent<PrefabScanner>();

            // 5. Apply Patches
            _harmony = new Harmony("com.raisinriotinteractive.thronefall.interactive");
            _harmony.PatchAll();

            Logger.LogInfo($"Thronefall Interactive Mod loaded!");
        }

        private void Start()
        {
            if (PluginConfig.AutoConnect.Value && !string.IsNullOrEmpty(PluginConfig.TikTokUsername.Value))
            {
                _connectionManager.Connect(PluginConfig.TikTokUsername.Value);
            }
        }

        private void Update()
        {
            _connectionManager?.Tick();
            SpawnOrchestrator.Instance?.Tick();
        }

        private void OnDestroy()
        {
            _connectionManager?.Disconnect();
            _harmony?.UnpatchSelf();
        }
    }
}
