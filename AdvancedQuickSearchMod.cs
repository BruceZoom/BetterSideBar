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
            clearOnLeave = C.GetEntry<bool>("clear_on_leave", false);
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
                                    element.IsNew = true;
                                    searchResults.Add(element);
                                }
                            }
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