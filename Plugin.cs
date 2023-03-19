using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace SuperConfigurablePickupRadius
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SuperConfigurablePickupRadiusPlugin : BaseUnityPlugin
    {
        internal const string ModName = "SuperConfigurablePickupRadius";
        internal const string ModVersion = "1.0.4";
        internal const string Author = "TastyChickenLegs";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        public static ConfigEntry<int> pickUpRange = null;
        public static ConfigEntry<bool> enablePickUpRange = null;
        internal static string ConnectionError = "";

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource SuperConfigurablePickupRadiusLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            enablePickUpRange = config("Insanely Configurable", "Enable Pickup Range", true, "Enable Pickup Range");
            pickUpRange = config("Insanely Configurable", "Pickup Range", 3, 
                new ConfigDescription("Auto Pickup Distance from Player. Restart Game After Setting This", 
                new AcceptableValueRange<int>(1, 10)));



            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                SuperConfigurablePickupRadiusLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
                updateConfigs();
            }
            catch
            {
                SuperConfigurablePickupRadiusLogger.LogError($"There was an issue loading your {ConfigFileName}");
                SuperConfigurablePickupRadiusLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }
        
        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }


        #endregion
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        public static class Player_Awake_Patch
        {
            private static void Postfix(ref Player __instance)
            {
                if (enablePickUpRange.Value)
                {
                    //convert int to float and set the pickup range
                    float autoPickupFloat = Convert.ToSingle(pickUpRange.Value);
                    __instance.m_autoPickupRange = autoPickupFloat;
                }
            }
        }

        public static void updateConfigs()
        {
            float autoPickupFloat = Convert.ToSingle(pickUpRange.Value);
            Player.m_localPlayer.m_autoPickupRange = autoPickupFloat;

        }
    }

}