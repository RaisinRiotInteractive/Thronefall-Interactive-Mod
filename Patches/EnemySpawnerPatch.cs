using HarmonyLib;

namespace TikTokGiftsToEnemies.Patches
{
    // Fires whenever any level's EnemySpawner finishes starting up.
    // This auto-populates the prefab cache so enemies from every visited level
    // can be spawned on any subsequent level — no gift required to trigger it.
    [HarmonyPatch(typeof(EnemySpawner), "Start")]
    public static class EnemySpawner_Start_Patch
    {
        static void Postfix(EnemySpawner __instance)
        {
            SpawnOrchestrator.Instance?.CachePrefabsFromSpawner(__instance);
            SpawnOrchestrator.Instance?.ResetSpawnDump();
        }
    }
}
