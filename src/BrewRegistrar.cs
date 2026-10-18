using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestedBrews
{
    /// <summary>Creates the brew items, recipes and fermenter conversions through Jotunn.</summary>
    internal static class BrewRegistrar
    {
        private const string BaseTemplate = "MeadBaseTasty";
        private const string PotionTemplate = "MeadTasty";
        private const string Cauldron = "piece_cauldron";

        private static readonly Dictionary<BrewTier, CustomItem> BaseItems = new Dictionary<BrewTier, CustomItem>();
        private static readonly Dictionary<BrewTier, CustomItemConversion> Conversions =
            new Dictionary<BrewTier, CustomItemConversion>();

        public static void RegisterAll()
        {
            for (int i = 0; i < BrewTiers.All.Count; i++)
                Register(BrewTiers.All[i], i);
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterAll;
        }

        private static void Register(BrewTier tier, int index)
        {
            var effect = ScriptableObject.CreateInstance<SE_RestedBrew>();
            effect.name = tier.EffectName;
            effect.m_name = $"${tier.Token}";
            effect.m_ttl = 1f;
            effect.TierIndex = index;
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));

            var potion = new CustomItem(tier.PotionPrefab, PotionTemplate, new ItemConfig
            {
                Name = $"${tier.Token}",
                Description = $"${tier.Token}_desc",
            });
            var shared = potion.ItemDrop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_consumeStatusEffect = effect;
            Tint(potion.ItemPrefab, tier.Tint);
            ItemManager.Instance.AddItem(potion);

            var baseConfig = new ItemConfig
            {
                Name = $"${tier.Token}_base",
                Description = $"${tier.Token}_base_desc",
                CraftingStation = Cauldron,
                MinStationLevel = tier.StationLevel.Value,
                Enabled = tier.Enabled.Value,
            };
            foreach (var ing in tier.ParseIngredients())
                baseConfig.AddRequirement(ing.Key, ing.Value);
            var brewBase = new CustomItem(tier.BasePrefab, BaseTemplate, baseConfig);
            Tint(brewBase.ItemPrefab, tier.Tint);
            ItemManager.Instance.AddItem(brewBase);
            BaseItems[tier] = brewBase;

            var conversion = new CustomItemConversion(new FermenterConversionConfig
            {
                FromItem = tier.BasePrefab,
                ToItem = tier.PotionPrefab,
                ProducedItems = tier.PotionsPerBatch.Value,
            });
            ItemManager.Instance.AddItemConversion(conversion);
            Conversions[tier] = conversion;

            RenderIcon(potion, tier);
            RenderIcon(brewBase, tier);
            AddLocalization(tier);
        }

        /// <summary>
        /// Gives the clone its own tinted materials, so the vanilla mead keeps its look.
        /// </summary>
        private static void Tint(GameObject prefab, Color tint)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials
                    .Select(m =>
                    {
                        if (m == null)
                            return null;
                        var copy = new Material(m);
                        if (copy.HasProperty("_Color"))
                            copy.color *= tint;
                        return copy;
                    })
                    .ToArray();
            }
        }

        private static void RenderIcon(CustomItem item, BrewTier tier)
        {
            try
            {
                var icon = RenderManager.Instance.Render(item.ItemPrefab, RenderManager.IsometricRotation);
                if (icon != null)
                    item.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }
            catch (System.Exception e)
            {
                RestedBrewsPlugin.Log.LogWarning($"{tier.DisplayName}: icon render failed, keeping vanilla icon ({e.Message})");
            }
        }

        private static void AddLocalization(BrewTier tier)
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { tier.Token, tier.DisplayName },
                { $"{tier.Token}_desc",
                  $"A fermented brew from the {tier.Biomes}. Drink it to become Rested anywhere" +
                  (tier.Minutes.Value > 0f
                      ? $" for {tier.Minutes.Value:0.#} minutes."
                      : $", as if you had rested at comfort {tier.MaxComfort.Value}.") },
                { $"{tier.Token}_base", $"{tier.DisplayName} base" },
                { $"{tier.Token}_base_desc", $"Ferment this in a Fermenter to make the {tier.DisplayName}." },
            });
        }

        /// <summary>
        /// Checks every ingredient prefab once the game's items exist, then logs the brew table.
        /// A tier with a missing ingredient is disabled rather than left half-working.
        /// </summary>
        public static void ValidateAndLog()
        {
            var rested = ObjectDB.instance?.GetStatusEffect(SEMan.s_statusEffectRested) as SE_Rested;
            if (rested != null)
                RestedBrewsPlugin.Log.LogInfo(
                    $"Live Rested: base {rested.m_baseTTL}s, +{rested.m_TTLPerComfortLevel}s per comfort level.");
            else
                RestedBrewsPlugin.Log.LogWarning("Could not find the vanilla Rested effect; brews will do nothing.");

            ApplyConfig();

            foreach (var tier in BrewTiers.All)
            {
                string source = tier.Minutes.Value > 0f ? "fixed" : $"comfort {tier.MaxComfort.Value}";
                RestedBrewsPlugin.Log.LogInfo(
                    $"{tier.DisplayName}: {tier.DurationSeconds() / 60f:0.#} min ({source}), " +
                    $"cauldron level {tier.StationLevel.Value}, ingredients {tier.Ingredients.Value}, " +
                    $"enabled {IsRecipeEnabled(tier)}");
            }
        }

        /// <summary>
        /// Re-applies config values to the live recipes and conversions. Runs once the game's items
        /// are registered, and again whenever a server syncs its config.
        /// </summary>
        public static void ApplyConfig()
        {
            if (ObjectDB.instance == null)
                return;

            foreach (var tier in BrewTiers.All)
            {
                if (!BaseItems.TryGetValue(tier, out var brewBase) || brewBase.Recipe?.Recipe == null)
                    continue;

                var recipe = brewBase.Recipe.Recipe;
                var requirements = new List<Piece.Requirement>();
                var missing = new List<string>();
                foreach (var ing in tier.ParseIngredients())
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(ing.Key);
                    var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                    if (drop == null)
                    {
                        missing.Add(ing.Key);
                        continue;
                    }
                    requirements.Add(new Piece.Requirement { m_resItem = drop, m_amount = ing.Value, m_recover = true });
                }

                if (missing.Count > 0)
                    RestedBrewsPlugin.Log.LogWarning(
                        $"{tier.DisplayName}: unknown ingredient(s) {string.Join(", ", missing)}; recipe disabled.");

                recipe.m_resources = requirements.ToArray();
                recipe.m_minStationLevel = tier.StationLevel.Value;
                recipe.m_enabled = tier.Enabled.Value && missing.Count == 0 && requirements.Count > 0;

                if (Conversions.TryGetValue(tier, out var conversion) &&
                    conversion.Config is FermenterConversionConfig fermenterConfig)
                {
                    fermenterConfig.ProducedItems = tier.PotionsPerBatch.Value;
                    SetProducedItems(tier, tier.PotionsPerBatch.Value);
                }
            }
        }

        private static bool IsRecipeEnabled(BrewTier tier) =>
            BaseItems.TryGetValue(tier, out var item) && item.Recipe?.Recipe != null && item.Recipe.Recipe.m_enabled;

        // The fermenter prefab holds the conversion list; fermenters placed after this pick it up.
        private static void SetProducedItems(BrewTier tier, int amount)
        {
            var fermenter = PrefabManager.Instance.GetPrefab("fermenter")?.GetComponent<Fermenter>();
            if (fermenter == null)
                return;
            foreach (var c in fermenter.m_conversion)
                if (c.m_from != null && c.m_from.name == tier.BasePrefab)
                    c.m_producedItems = amount;
        }
    }
}
