using HarmonyLib;
using UnityEngine;

namespace RestedBrews
{
    /// <summary>
    /// Short-lived effect applied when a brew is drunk. It grants the vanilla Rested effect and
    /// raises its timer to what the tier's comfort level would give, then expires on its own.
    /// </summary>
    public class SE_RestedBrew : StatusEffect
    {
        internal static readonly AccessTools.FieldRef<StatusEffect, float> TimeRef =
            AccessTools.FieldRefAccess<StatusEffect, float>("m_time");

        public int TierIndex;

        // The local player's Rested instance granted by the last brew, and that brew's duration.
        // A new Rested instance (expired, died, relogged) means no brew is active any more.
        private static StatusEffect s_brewRested;
        private static float s_brewDuration;

        /// <summary>Duration of the brew behind the player's current Rested, or 0 if none.</summary>
        public static float ActiveBrewDuration(StatusEffect currentRested) =>
            currentRested != null && currentRested == s_brewRested ? s_brewDuration : 0f;

        public override void Setup(Character character)
        {
            base.Setup(character);

            var player = character as Player;
            if (player == null || player != Player.m_localPlayer)
                return;

            float duration = BrewTiers.All[TierIndex].DurationSeconds();
            if (duration <= 0f)
                return;

            var seman = player.GetSEMan();
            seman.AddStatusEffect(SEMan.s_statusEffectRested, resetTime: true);
            var rested = seman.GetStatusEffect(SEMan.s_statusEffectRested);
            if (rested == null)
                return;

            // Only ever lengthen Rested, never cut a longer one short.
            if (rested.GetRemaningTime() < duration)
            {
                rested.m_ttl = duration;
                TimeRef(rested) = 0f;
            }
            if (duration > ActiveBrewDuration(rested))
            {
                s_brewRested = rested;
                s_brewDuration = duration;
            }

            player.Message(MessageHud.MessageType.Center,
                $"Rested for {Mathf.RoundToInt(rested.GetRemaningTime() / 60f)} min");
        }

        /// <summary>
        /// Rested duration in seconds at the given comfort level, using the live Rested effect's
        /// values so it matches vanilla exactly. Returns 0 if the effect can't be found.
        /// </summary>
        public static float RestedDuration(int comfort)
        {
            var rested = ObjectDB.instance?.GetStatusEffect(SEMan.s_statusEffectRested) as SE_Rested;
            if (rested == null)
                return 0f;
            return rested.m_baseTTL + (comfort - 1) * rested.m_TTLPerComfortLevel;
        }
    }
}
