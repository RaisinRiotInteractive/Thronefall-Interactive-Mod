using HarmonyLib;

namespace TikTokGiftsToEnemies.Patches
{
    [HarmonyPatch(typeof(DayNightCycle), "SwitchToNight")]
    public static class DayNightCycle_SwitchToNight_Patch
    {
        static void Postfix()
        {
            if (SpawnOrchestrator.Instance != null)
            {
                SpawnOrchestrator.Instance.FlushQueue();
            }
        }
    }
}
