using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace BetterSideBarNS
{
    public enum AdvancedSearchMode { Result, Ingred };

    public class AdvancedQuickSearchMod
    {
        private static ModLogger L;
        private static ConfigFile C;

        private static string targetId;
        private static List<IdeaElement> searchResults;
        private static int currentFocusIdx;
        private static bool mouseUnfocused;
        private static Dictionary<string, bool> isQuickSearchResult;

        private static AdvancedSearchMode mode;
        private static ConfigEntry<bool> defaultResultMode;
        private static ConfigEntry<bool> clearOnLeave;
        private static ConfigEntry<bool> enableQuickSearch;

        private static Sprite QuickSearchIcon;
        private static Dictionary<string, GameObject> quickLabelMap;

        static IdeaElement hidingUnhoveredIdea;

        public static bool IsQuickSearchResult(string cardId)
        {
            if (isQuickSearchResult.ContainsKey(cardId))
            {
                return isQuickSearchResult[cardId];
            }
            return false;
        }

        public static void Initialize(ModLogger logger, ConfigFile config)
        {
            L = logger;
            C = config;

            searchResults = new List<IdeaElement>();
            mouseUnfocused = true;

            defaultResultMode = C.GetEntry<bool>("default_search_result", true);
            defaultResultMode.UI.Name = "Default Search for Recipe";
            //defaultResultMode.UI.NameTerm = "default_result_mode_name";
            defaultResultMode.UI.Tooltip =
                "On: Default to search for recipes making the card.\n" +
                "Off: Default to search for recipes using the card as ingredients.\n" +
                "Hold Alt when doing quick search to switch mode.";

            clearOnLeave = C.GetEntry<bool>("clear_on_leave", true);
            clearOnLeave.UI.Name = "Reset Focused Result";
            //clearOnLeave.UI.NameTerm = "clear_on_leave_name";
            clearOnLeave.UI.Tooltip = "When turned on, reset the focused search result to the first hit entry,\n" +
                "when moving mouse away from the searched card.";

            enableQuickSearch = C.GetEntry<bool>("disable_quick_search", true);
            enableQuickSearch.UI.Name = "Enable Quick Search";
            //clearOnLeave.UI.NameTerm = "disable_quick_search_name";
            enableQuickSearch.UI.Tooltip = "Enables the quick search.";

            // load assets
            Mod m = new Mod();
            ModManager.TryGetMod("better_sidebar", out m);
            L.Log(m.Path);
            QuickSearchIcon = ResourceHelper.LoadSpriteFromPath(m.Path + "/Icons/icon-quick.png");

            isQuickSearchResult = new Dictionary<string, bool>();
            quickLabelMap = new Dictionary<string, GameObject>();
        }

        public static void InitIdeaElements()
        {
            foreach (IdeaElement element in BlueprintDB.IdeaElements)
            {
                // initialize quick search mapping
                isQuickSearchResult.Add(element.MyKnowledge.CardId, false);

                // initialize quick icon
                GameObject quickLabel = GameObject.Instantiate(element.NewLabel);
                quickLabel.transform.SetParent(element.transform);
                //quickLabel.transform.SetSiblingIndex(quickLabel.transform.GetSiblingIndex() - 1);
                quickLabel.GetComponent<Image>().sprite = QuickSearchIcon;
                quickLabel.SetActive(false);
                quickLabelMap.Add(element.MyKnowledge.CardId, quickLabel);
            }
        }

        [HarmonyPatch(typeof(WorldManager), "Update")]
        public class TestHarmonyPatches
        {
            public static void Postfix(CardData __instance)
            {
                if (enableQuickSearch.Value)
                {
                    if (!mouseUnfocused && WorldManager.instance.HoveredCard == null)
                    {
                        if (clearOnLeave.Value)
                        {
                            targetId = "";
                            searchResults.Clear();
                            currentFocusIdx = 0;
                        }
                        mouseUnfocused = true;
                    }
                    if (Mouse.current.middleButton.wasPressedThisFrame
                         && WorldManager.instance.HoveredCard != null)
                    {
                        AdvancedSearchMode newMode =
                            // computes the xor
                            ((Keyboard.current.leftAltKey.isPressed != defaultResultMode.Value) ?
                                AdvancedSearchMode.Result : AdvancedSearchMode.Ingred);

                        string newTargetId = WorldManager.instance.HoveredCard.CardData.Id;
                        L.Log(newTargetId);
                        // focus on next search result
                        if (newTargetId == targetId && newMode == mode)
                        {
                            currentFocusIdx = (currentFocusIdx + 1) % searchResults.Count;
                            /*SnapTo(searchResults[currentFocusIdx]);*/
                            mouseUnfocused = false;
                        }
                        // restart search when clicking a new card
                        else
                        {
                            mode = newMode;
                            Dictionary<string, List<string>> dict = ((mode == AdvancedSearchMode.Result) ?
                                BlueprintDB.ResultBPMap : BlueprintDB.IngredBPMap);
                            if (dict.ContainsKey(newTargetId))
                            {
                                targetId = newTargetId;
                                searchResults.Clear();
                                currentFocusIdx = 0;
                                mouseUnfocused = false;
                                foreach (IdeaElement element in BlueprintDB.IdeaElements)
                                {
                                    if (KnowledgeWasFound(element.MyKnowledge) &&
                                        dict[targetId].Contains(element.MyKnowledge.CardId))
                                    {
                                        // need to set active so it works properly in the pinned only mode
                                        //element.gameObject.SetActive(true);
                                        isQuickSearchResult[element.MyKnowledge.CardId] = true;
                                        quickLabelMap[element.MyKnowledge.CardId].SetActive(true);
                                        searchResults.Add(element);
                                    }
                                }
                                /*if (searchResults.Count > 0)
                                {
                                    SnapTo(searchResults[0]);
                                }*/
                                // need to update idea panel so it works properly in the pinned only mode
                                GameScreen.instance.UpdateIdeasLog();
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(IdeaElement), "Update")]
        public class UpdateElementQuickSearchStatucHarmonyPatches
        {
            public static void Postfix(IdeaElement __instance)
            {
                if (IsQuickSearchResult(__instance.MyKnowledge.CardId) &&
                    (__instance.MyButton.IsHovered || __instance.MyButton.IsSelected))
                {
                    isQuickSearchResult[__instance.MyKnowledge.CardId] = false;
                    quickLabelMap[__instance.MyKnowledge.CardId].SetActive(false);
                    hidingUnhoveredIdea = __instance;
                }
            }
        }

        [HarmonyPatch(typeof(GameScreen), "Update")]
        public class HideUnhoveredCoroutineHarmonyPatches
        {
            public static void Postfix()
            {
                // when a valid hovered idea is waiting to hide,
                // and it is no longer hovered/selected, then hide it
                if (hidingUnhoveredIdea != null && !hidingUnhoveredIdea.MyButton.IsHovered &&
                    !hidingUnhoveredIdea.MyButton.IsSelected)
                {
                    hidingUnhoveredIdea = null;
                    GameScreen.instance.UpdateIdeasLog();
                }
            }
        }

        [HarmonyPatch(typeof(GameScreen), "LateUpdate")]
        public class RenderCurrentFocusInfoHarmonyPatches
        {
            public static void Prefix()
            {
                if (enableQuickSearch.Value)
                {
                    // MARK: need to override the 
                    // display currently focused result
                    if (!mouseUnfocused && searchResults.Count > 0)
                    {
                        GameScreen.instance.InfoTitle.text = "";
                        GameScreen.InfoBoxTitle = searchResults[currentFocusIdx].MyKnowledge.KnowledgeName;
                        GameScreen.InfoBoxText = searchResults[currentFocusIdx].MyKnowledge.KnowledgeText;
                    }
                }
            }
        }

        private static bool KnowledgeWasFound(IKnowledge knowledge)
        {
            return WorldManager.instance.CurrentSave.FoundCardIds.Contains(knowledge.CardId);
        }
        
        private static void SnapTo(IdeaElement element)
        {
            RectTransform contentPanel = element.gameObject.transform.parent.parent.gameObject.GetComponent<RectTransform>();
            ScrollRect scrollRect = element.gameObject.transform.parent.parent.parent.gameObject.GetComponent<ScrollRect>();
            RectTransform target = element.gameObject.GetComponent<RectTransform>();
            //Canvas.ForceUpdateCanvases();
            contentPanel.anchoredPosition =
                    (Vector2)scrollRect.transform.InverseTransformPoint(contentPanel.position)
                    - (Vector2)scrollRect.transform.InverseTransformPoint(target.position);
        }
    }
}