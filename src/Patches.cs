using HarmonyLib;

namespace RestedBrews
{
    [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem))]
    internal static class CanConsumeItemPatch
    {
        // Refuse a brew while one of the same or a higher tier is still active, or while Rested
        // already lasts at least as long as the brew would, so it isn't wasted.
        private static void Postfix(Player __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result || !(item?.m_shared.m_consumeStatusEffect is SE_RestedBrew brew))
                return;

            var rested = __instance.GetSEMan().GetStatusEffect(SEMan.s_statusEffectRested);
            if (rested == null)
                return;

            float duration = BrewTiers.All[brew.TierIndex].DurationSeconds();
            if (SE_RestedBrew.ActiveBrewDuration(rested) >= duration || rested.GetRemaningTime() >= duration)
            {
                __instance.Message(MessageHud.MessageType.Center, "You are already well rested");
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.UpdateStatusEffect))]
    internal static class RestedUpdatePatch
    {
        // Undo this frame's countdown while a brew's Rested is active and the player is by a fire.
        private static void Postfix(SE_Rested __instance, float dt)
        {
            if (!BrewTiers.PauseNearFire.Value || SE_RestedBrew.ActiveBrewDuration(__instance) <= 0f)
                return;

            var player = Player.m_localPlayer;
            if (player == null || !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectCampFire))
                return;

            ref float time = ref SE_RestedBrew.TimeRef(__instance);
            time = UnityEngine.Mathf.Max(0f, time - dt);
        }
    }
}
