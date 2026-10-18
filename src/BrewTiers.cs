using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace RestedBrews
{
    /// <summary>One potion tier: its fixed identity plus the config entries players can tune.</summary>
    public class BrewTier
    {
        public string Key;          // short id used in prefab names and tokens, e.g. "forest"
        public string DisplayName;  // e.g. "Forest Draught"
        public string Biomes;       // e.g. "Black Forest"
        public Color Tint;

        // Defaults, used to bind config
        public string DefaultIngredients;
        public int DefaultStationLevel;
        public int DefaultMaxComfort;
        public float DefaultMinutes;

        public ConfigEntry<bool> Enabled;
        public ConfigEntry<string> Ingredients;
        public ConfigEntry<int> StationLevel;
        public ConfigEntry<int> MaxComfort;
        public ConfigEntry<float> Minutes;
        public ConfigEntry<int> PotionsPerBatch;

        /// <summary>
        /// Rested duration in seconds: the fixed Minutes setting if above 0, otherwise what
        /// vanilla Rested gives at ComfortLevel. Returns 0 if the Rested effect can't be found.
        /// </summary>
        public float DurationSeconds() =>
            Minutes.Value > 0f ? Minutes.Value * 60f : SE_RestedBrew.RestedDuration(MaxComfort.Value);

        public string BasePrefab => $"RestedBrew_{Key}_Base";
        public string PotionPrefab => $"RestedBrew_{Key}";
        public string EffectName => $"SE_RestedBrew_{Key}";
        public string Token => $"restedbrew_{Key}";

        /// <summary>Parses "Prefab:amount,Prefab:amount" into (prefab, amount) pairs; bad entries are skipped.</summary>
        public List<KeyValuePair<string, int>> ParseIngredients()
        {
            var result = new List<KeyValuePair<string, int>>();
            foreach (var part in Ingredients.Value.Split(','))
            {
                var bits = part.Split(':');
                if (bits.Length != 2 || !int.TryParse(bits[1].Trim(), out int amount) || amount < 1)
                {
                    if (part.Trim().Length > 0)
                        RestedBrewsPlugin.Log.LogWarning($"{DisplayName}: ignoring bad ingredient entry '{part.Trim()}'");
                    continue;
                }
                result.Add(new KeyValuePair<string, int>(bits[0].Trim(), amount));
            }
            return result;
        }
    }

    public static class BrewTiers
    {
        // Max comfort per tier is the realistic best you can build with that tier's materials
        // (Black Forest 13, Swamp 15, Mountain 17, Plains 19, Mistlands 20, Ashlands 22, Deep North 22).
        // Merged tiers use the higher biome of the pair. Cauldron level = 1 + attached extensions;
        // each extension is gated by its biome's materials, so the level gates the tier.
        public static readonly List<BrewTier> All = new List<BrewTier>
        {
            new BrewTier
            {
                Key = "forest", DisplayName = "Forest Draught", Biomes = "Black Forest",
                Tint = new Color(0.35f, 0.75f, 0.35f),
                DefaultIngredients = "BoneFragments:5,Thistle:3",
                DefaultStationLevel = 1, DefaultMaxComfort = 13,
            },
            new BrewTier
            {
                Key = "frostmarsh", DisplayName = "Frostmarsh Draught", Biomes = "Swamp + Mountains",
                Tint = new Color(0.45f, 0.85f, 0.95f),
                DefaultIngredients = "Bloodbag:2,Turnip:3,Onion:3,FreezeGland:1",
                DefaultStationLevel = 3, DefaultMaxComfort = 17, DefaultMinutes = 25f,
            },
            new BrewTier
            {
                Key = "mistfield", DisplayName = "Mistfield Draught", Biomes = "Plains + Mistlands",
                Tint = new Color(0.75f, 0.5f, 0.95f),
                DefaultIngredients = "Cloudberry:5,Barley:3,RoyalJelly:2,MushroomJotunPuffs:2",
                DefaultStationLevel = 5, DefaultMaxComfort = 20, DefaultMinutes = 30f,
            },
            new BrewTier
            {
                Key = "emberfrost", DisplayName = "Emberfrost Draught", Biomes = "Ashlands + Deep North",
                Tint = new Color(1f, 0.45f, 0.2f),
                DefaultIngredients = "Vineberry:3,Fiddleheadfern:2,MushroomSmokePuff:2,Lingonberry:5",
                DefaultStationLevel = 7, DefaultMaxComfort = 22, DefaultMinutes = 35f,
            },
        };

        public static ConfigEntry<bool> PauseNearFire;

        public static void BindConfig(ConfigFile config)
        {
            PauseNearFire = config.Bind("0 - General", "PauseNearFire", true,
                new ConfigDescription("Pause a brew's Rested timer while you are within a fire's warmth.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            for (int i = 0; i < All.Count; i++)
            {
                var t = All[i];
                string section = $"{i + 1} - {t.DisplayName}";
                var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

                t.Enabled = config.Bind(section, "Enabled", true,
                    new ConfigDescription($"Allow brewing the {t.DisplayName} ({t.Biomes}).", null, adminOnly));
                t.Ingredients = config.Bind(section, "Ingredients", t.DefaultIngredients,
                    new ConfigDescription("Cauldron ingredients for the brew base, as Prefab:amount pairs separated by commas.",
                        null, adminOnly));
                t.StationLevel = config.Bind(section, "CauldronLevel", t.DefaultStationLevel,
                    new ConfigDescription("Cauldron level needed (1 + number of attached Cauldron upgrades).",
                        new AcceptableValueRange<int>(1, 10), adminOnly));
                t.MaxComfort = config.Bind(section, "ComfortLevel", t.DefaultMaxComfort,
                    new ConfigDescription("If Minutes is 0, the potion grants Rested as if you rested at this " +
                                          "comfort level (default: the best comfort buildable in this tier).",
                        new AcceptableValueRange<int>(1, 40), adminOnly));
                t.Minutes = config.Bind(section, "Minutes", t.DefaultMinutes,
                    new ConfigDescription("Fixed Rested duration in minutes. 0 = use ComfortLevel instead.",
                        new AcceptableValueRange<float>(0f, 180f), adminOnly));
                t.PotionsPerBatch = config.Bind(section, "PotionsPerBatch", 4,
                    new ConfigDescription("Potions produced by one fermented brew base.",
                        new AcceptableValueRange<int>(1, 20), adminOnly));
            }
        }
    }
}
