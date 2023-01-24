using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Security.Permissions;

namespace SuperConfigurablePickupRadius
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class SuperConfigurablePickupRadiusMain : BaseUnityPlugin
    {
        public const string MODNAME = "SuperConfigurablePickupRadius";
        public const string AUTHOR = "TastyChickenLegs";
        public const string GUID = AUTHOR+"."+MODNAME;
        public const string VERSION = "1.0.0";
        public static ManualLogSource logger;
        internal Harmony harmony;
        internal Assembly assembly;
        public static ConfigEntry<int> pickUpRange = null;
        public static ConfigEntry<bool> enablePickUpRange = null;
        


        public static readonly ManualLogSource TastyLogger =
            BepInEx.Logging.Logger.CreateLogSource(MODNAME);

        private void Awake()
        {

           
            enablePickUpRange = Config.Bind<bool>("Insanely Configurable", "Enable Pickup Range", false, "Enable Pickup Range");
            pickUpRange = Config.Bind<int>("Insanely Configurable", "Pickup Range", 1, new ConfigDescription("Auto Pickup Distance from Player", new AcceptableValueRange<int>(1, 5)));

            assembly = Assembly.GetExecutingAssembly();

            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();

        }

        private void OnDestroy()
        {
            //Dbgl("Destroying plugin");
            harmony.UnpatchSelf();
        }

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
    }


}
