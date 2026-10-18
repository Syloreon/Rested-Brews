using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace RestedBrews
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class RestedBrewsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.syloreon.restedbrews";
        public const string PluginName = "Rested Brews";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            BrewTiers.BindConfig(Config);

            PrefabManager.OnVanillaPrefabsAvailable += BrewRegistrar.RegisterAll;
            ItemManager.OnItemsRegistered += BrewRegistrar.ValidateAndLog;
            SynchronizationManager.OnConfigurationSynchronized += (_, __) => BrewRegistrar.ApplyConfig();

            new Harmony(PluginGuid).PatchAll();
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }
}
