using BattleTech.UI;
using Harmony;
using HBS.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace StopScrolling
{
    public class StopScrolling
    {

        private static ILog m_log = HBS.Logging.Logger.GetLogger(typeof(StopScrolling).Name, LogLevel.Log);
        private static float m_scrollPos = 1f;
        private static bool m_scrollAfterFilter;

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("io.github.splintermind.StopScrolling");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Log(string message)
        {
            m_log.Log(message);
        }

        #region MechLabPanel

        [HarmonyPatch(typeof(MechLabPanel), "PopulateInventory")]
        public static class BattleTech_UI_MechLabPanel_PopulateInventory_Postfix
        {
            public static void Postfix(MechLabPanel __instance)
            {
                //Log("scrolling to top.");
                m_scrollAfterFilter = true;
                ScrollToTop(Traverse.Create(__instance).Field("inventoryWidget").GetValue<MechLabInventoryWidget>());
            }
        }

        #endregion MechLabPanel

        #region MechLabInventoryWidget

        [HarmonyPatch(typeof(MechLabInventoryWidget), "OnAddItem")]
        public static class BattleTech_UI_MechLabInventoryWidget_OnAddItem_Prefix
        {
            public static void Prefix(MechLabInventoryWidget __instance)
            {
                m_scrollAfterFilter = true;
            }
        }

        [HarmonyPatch(typeof(MechLabInventoryWidget), "OnFilterButtonClicked")]
        public static class BattleTech_UI_MechLabInventoryWidget_OnFilterButtonClicked_Prefix
        {
            public static void Prefix(MechLabInventoryWidget __instance)
            {
                m_scrollAfterFilter = true;
            }
        }

        [HarmonyPatch(typeof(MechLabInventoryWidget), "OnItemHoverEnter")]
        public static class BattleTech_UI_MechLabInventoryWidget_OnItemHoverEnter_Postfix
        {
            public static void Postfix(MechLabInventoryWidget __instance)
            {
                m_scrollPos = Scrollbar(__instance).verticalNormalizedPosition;
                //Log("saving scroll (hover)");

            }
        }

        [HarmonyPatch(typeof(MechLabInventoryWidget), "ApplySorting")]
        public static class BattleTech_UI_MechLabInventoryWidget_ApplySorting_Prefix
        {
            public static void Prefix(MechLabInventoryWidget __instance)
            {
                if (Traverse.Create(__instance).Field("currentSort").GetValue<Comparison<InventoryItemElement>>() != null)
                {
                    m_scrollPos = Scrollbar(__instance).verticalNormalizedPosition;
                    //Log("saving scroll (hover)");
                }
            }
        }

        [HarmonyPatch(typeof(MechLabInventoryWidget), "ApplySorting")]
        public static class BattleTech_UI_MechLabInventoryWidget_ApplySorting_Postfix
        {
            public static void Postfix(MechLabInventoryWidget __instance)
            {
                if (__instance.gameObject.activeInHierarchy)
                {
                    __instance.StartCoroutine(EndOfFrameScrollBarMovement(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(MechLabInventoryWidget), "ApplyFiltering")]
        public static class BattleTech_UI_MechLabInventoryWidget_ApplyFiltering_Postfix
        {
            public static void Postfix(MechLabInventoryWidget __instance)
            {
                // when a filter is applied, permit the default behaviour (scroll to top) unless we're in the mech bay
                if (m_scrollAfterFilter)
                {
                    m_scrollAfterFilter = false;
                    if (__instance.gameObject.activeInHierarchy)
                    {
                        __instance.StartCoroutine(EndOfFrameScrollBarMovement(__instance));
                    }
                }
                //Log($"resetting selection index");
                m_selectedIdx = -1; // reset selection on filter as it will scroll to the top
            }
        }

        private static IEnumerator EndOfFrameScrollBarMovement(MechLabInventoryWidget __instance)
        {
            yield return new WaitForEndOfFrame();
            Scrollbar(__instance).verticalNormalizedPosition = Mathf.Clamp(m_scrollPos, 0f, 1f);
            yield break;
        }

        private static ScrollRect Scrollbar(MechLabInventoryWidget __instance)
        {
            return Traverse.Create(__instance).Field("scrollbarArea").GetValue<ScrollRect>();
        }

        private static void ScrollToTop(MechLabInventoryWidget __instance)
        {
            m_scrollPos = 1f;
            Scrollbar(__instance).verticalNormalizedPosition = 1f;
        }

        #endregion MechLabInventoryWidget

        #region SG_Shop_Screen

        private static int m_selectedIdx = -1;
        private static bool m_needSaveSelected;

        [HarmonyPatch(typeof(SG_Shop_Screen), "ChangeToBuy")]
        public static class BattleTech_UI_SG_Shop_Screen_ChangeToBuy_Postfix
        {
            public static void Postfix(SG_Shop_Screen __instance)
            {
                ScrollToTop(Traverse.Create(__instance).Field("inventoryWidget").GetValue<MechLabInventoryWidget>());
            }
        }

        [HarmonyPatch(typeof(SG_Shop_Screen), "ChangeToSell")]
        public static class BattleTech_UI_SG_Shop_Screen_ChangeToSell_Postfix
        {
            public static void Postfix(SG_Shop_Screen __instance)
            {
                ScrollToTop(Traverse.Create(__instance).Field("inventoryWidget").GetValue<MechLabInventoryWidget>());
            }
        }

        [HarmonyPatch(typeof(SG_Shop_Screen), "OnItemSelected")]
        public static class BattleTech_UI_SG_Shop_Screen_OnItemSelected_Prefix
        {
            public static void Prefix(SG_Shop_Screen __instance, InventoryItemElement item)
            {
                if (m_needSaveSelected)
                {
                    m_selectedIdx = GetActiveItems(__instance).IndexOf(item);
                    //Log($"saving index {m_selectedIdx}");
                }
            }
        }

        [HarmonyPatch(typeof(SG_Shop_Screen), "RefreshSelection")]
        public static class BattleTech_UI_SG_Shop_Screen_EndOfFrameRefreshSelection_Prefix
        {
            public static void Prefix(SG_Shop_Screen __instance)
            {
                m_needSaveSelected = false;
            }
        }

        [HarmonyPatch(typeof(SG_Shop_Screen), "RefreshSelection")]
        public static class BattleTech_UI_SG_Shop_Screen_EndOfFrameRefreshSelection_Postfix
        {
            public static void Postfix(SG_Shop_Screen __instance)
            {
                ReSelect(__instance);
                m_needSaveSelected = true;
            }
        }

        public static List<InventoryItemElement> GetActiveItems(SG_Shop_Screen __instance)
        {
            return Traverse.Create(__instance).Field("inventoryWidget").Field("localInventory").GetValue<List<InventoryItemElement>>().FindAll(i => i.isActiveAndEnabled);
        }

        private static void ReSelect(SG_Shop_Screen __instance)
        {
            //Log($"attempting to select index {m_selectedIdx}");
            if (m_selectedIdx > -1)
            {
                List<InventoryItemElement> list = GetActiveItems(__instance);
                if (list.Count > 0)
                {
                    if (m_selectedIdx > list.Count - 1)
                    {
                        m_selectedIdx = list.Count - 1;
                    }
                    if (m_selectedIdx > -1)
                    {
                        //Log($"selecting item at index {m_selectedIdx}");
                        InventoryItemElement item = list[m_selectedIdx];
                        if (item != null)
                        {
                            if (item.gameObject.activeSelf)
                            {
                                item.buttonElement.ForceRadioSetSelection();
                            }
                            __instance.OnItemSelected(item);
                        }
                    }
                }

            }

        }

        #endregion SG_Shop_Screen

    }
}