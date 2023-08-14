using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
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

        private static AdvancedSearchMode mode;
        private static ConfigEntry<bool> defaultResultMode;
        private static ConfigEntry<bool> clearOnLeave;

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
        }

        [HarmonyPatch(typeof(WorldManager), "Update")]
        public class TestHarmonyPatches
        {
            public static void Postfix(CardData __instance)
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
                        foreach (IdeaElement element in searchResults)
                        {
                            element.IsNew = true;
                        }
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
                                    if (!element.IsNew)
                                    {
                                        //element.IsNew = true;
                                        SaveManager.instance.CurrentSave.NewKnowledgeIds.Add(element.MyKnowledge.CardId);
                                    }
                                    // need to set active so it works properly in the pinned only mode
                                    element.gameObject.SetActive(true);
                                    searchResults.Add(element);
                                }
                            }
                            // need to update idea panel so it works properly in the pinned only mode
                            GameScreen.instance.UpdateIdeasLog();
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameScreen), "LateUpdate")]
        public class RenderCurrentFocusInfoHarmonyPatches
        {
            public static void Prefix()
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

        private static bool KnowledgeWasFound(IKnowledge knowledge)
        {
            return WorldManager.instance.CurrentSave.FoundCardIds.Contains(knowledge.CardId);
        }
    }
}