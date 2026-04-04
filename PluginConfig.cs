using BepInEx.Configuration;

namespace TikTokGiftsToEnemies
{
    public static class PluginConfig
    {
        public static ConfigEntry<string> TikTokUsername;
        public static ConfigEntry<bool>   AutoConnect;
        public static ConfigEntry<bool>   ShowOnScreenNotifications;

        // "GiftName:SpawnIndex:Count" pairs separated by ;
        // e.g.  Rose:0:1;Lion:1:2;Universe:2:5
        public static ConfigEntry<string> GiftRules;

        // "MinDiamonds:SpawnIndex:Count" pairs separated by ;
        // e.g.  5:0:1;50:1:3;200:2:5
        // Highest matching threshold wins.
        public static ConfigEntry<string> CoinRules;

        // "Likes:PrefabName:Count" pairs separated by ;
        // e.g. 100:E Melee:1
        public static ConfigEntry<string> LikeRules;

        // "Follows:PrefabName:Count" pairs separated by ;
        // e.g. 1:E Spider Small:1
        public static ConfigEntry<string> FollowRules;

        // How enemies are spawned when a gift is received.
        public static ConfigEntry<string> SpawnMode;

        public static void Init(ConfigFile config)
        {
            TikTokUsername = config.Bind("General", "TikTokUsername", "",
                "Streamer's TikTok unique ID");

            AutoConnect = config.Bind("General", "AutoConnect", false,
                "Automatically connect on game start");

            SpawnMode = config.Bind("General", "SpawnMode", "NightAware",
                "When to spawn gifted enemies.\n" +
                "  Immediate  - spawn instantly regardless of day/night\n" +
                "  Queue      - always hold until the next night wave\n" +
                "  NightAware - spawn immediately at night, queue during day");

            ShowOnScreenNotifications = config.Bind("General", "ShowOnScreenNotifications", true,
                "Show on-screen gift notifications");

            GiftRules = config.Bind("Rules", "GiftRules", "",
                "Map specific gift names to monster spawns.\n" +
                "Format: GiftName:PrefabName:Count separated by ;\n" +
                "PrefabName is the enemy prefab name from interactive_spawns.json.\n" +
                "Run the game once then check BepInEx/config/interactive_spawns.json for available names.\n" +
                "Example: Rose:BasicEnemy:1;Universe:HardEnemy:5");

            CoinRules = config.Bind("Rules", "CoinRules", "",
                "Map diamond totals to monster spawns (used when gift has no name match).\n" +
                "Format: MinDiamonds:PrefabName:Count separated by ;\n" +
                "Highest matching threshold wins.\n" +
                "Example: 5:BasicEnemy:1;50:MediumEnemy:3;200:HardEnemy:5");

            LikeRules = config.Bind("Rules", "LikeRules", "100:E Melee:1",
                "Map likes to monster spawns.\n" +
                "Format: LikesPerSpawn:PrefabName:Count separated by ;\n" +
                "Example: 100:E Melee:1;500:E Elite:1");

            FollowRules = config.Bind("Rules", "FollowRules", "1:E Spider Small:1",
                "Map follows to monster spawns.\n" +
                "Format: FollowsPerSpawn:PrefabName:Count separated by ;\n" +
                "Example: 1:E Spider Small:1");
        }
    }
}
