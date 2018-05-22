using Harmony;
using HBS.Logging;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace StopScrolling
{
    public class StopScrolling
    {

        private static ILog m_log = HBS.Logging.Logger.GetLogger(typeof(StopScrolling).Name, LogLevel.Log);
        private static float m_pos = 1f;

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("io.github.splintermind.StopScrolling");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Log(string message)
        {
            m_log.Log(message);
        }

        [HarmonyPatch(typeof(BattleTech.UI.MechLabPanel), "PopulateInventory")]
        public static class BattleTech_UI_MechLabPanel_PopulateInventory_Prefix
        {
            public static void Prefix(BattleTech.UI.MechLabPanel __instance)
            {
                StopScrolling.m_pos = -1f; //reset scroll to the top on the next re-position
            }
        }

        [HarmonyPatch(typeof(BattleTech.UI.MechLabInventoryWidget), "ApplySorting")]
        public static class BattleTech_UI_MechLabInventoryWidget_ApplySorting_Prefix
        {
            public static void Prefix(BattleTech.UI.MechLabInventoryWidget __instance)
            {
                if (Traverse.Create(__instance).Field("currentSort").GetValue<Comparison<BattleTech.UI.InventoryItemElement>>() != null)
                {
                    SaveScrollPosition(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(BattleTech.UI.MechLabInventoryWidget), "ApplySorting")]
        public static class BattleTech_UI_MechLabInventoryWidget_ApplySorting_Postfix
        {
            public static void Postfix(BattleTech.UI.MechLabInventoryWidget __instance)
            {
                if (__instance.gameObject.activeInHierarchy)
                {
                    __instance.StartCoroutine(EndOfFrameScrollBarMovement(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(BattleTech.UI.MechLabInventoryWidget), "ApplyFiltering")]
        public static class BattleTech_UI_MechLabInventoryWidget_ApplyFiltering_Patch
        {
            public static void Postfix(BattleTech.UI.MechLabInventoryWidget __instance)
            {
                if (__instance.gameObject.activeInHierarchy)
                {
                    __instance.StartCoroutine(EndOfFrameScrollBarMovement(__instance));
                }
            }
        }

        private static IEnumerator EndOfFrameScrollBarMovement(BattleTech.UI.MechLabInventoryWidget __instance)
        {
            yield return new WaitForEndOfFrame();
            Traverse.Create(__instance).Field("scrollbarArea").Property("verticalNormalizedPosition").SetValue(Mathf.Clamp(StopScrolling.m_pos, 0f, 1f));
            yield break;
        }

        private static void SaveScrollPosition(BattleTech.UI.MechLabInventoryWidget __instance)
        {
            if (StopScrolling.m_pos == -1)
            {
                StopScrolling.m_pos = 1f;
            }
            else
            {
                StopScrolling.m_pos = Traverse.Create(__instance).Field("scrollbarArea").Property("verticalNormalizedPosition").GetValue<float>();
            }
            //Log($"saved scroll position {StopScrolling.pos}");
        }

    }
}