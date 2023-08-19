using HarmonyLib;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSideBarNS
{
    public static class SidebarDisplayControl
    {
        private static ModLogger L;
        private static ConfigFile C;

        private static Sprite FavorIcon;
        private static Sprite QuickSearchIcon;
        private static Sprite NewIcon;

        public static bool PinFilterOn;
        private static CustomButton pinFilterBtn;
        private static CustomButton resetPinBtn;

        public static bool QuickFilterOn;
        private static CustomButton quickFilterBtn;
        private static CustomButton resetQuickBtn;

        public static bool NewFilterOn;
        private static CustomButton newFilterBtn;
        private static CustomButton resetNewBtn;

        public static void Initialize(ModLogger logger, ConfigFile config)
        {
            L = logger;
            C = config;

            // load assets
            Mod m = new Mod();
            ModManager.TryGetMod("better_sidebar", out m);
            L.Log(m.Path);
            FavorIcon = ResourceHelper.LoadSpriteFromPath(m.Path + "/Icons/icon-pin.png");
            QuickSearchIcon = ResourceHelper.LoadSpriteFromPath(m.Path + "/Icons/icon-quick.png");

            PinFilterOn = false;
            QuickFilterOn = false;
            NewFilterOn = false;
        }


        /// <summary>
        /// Add buttons for hiding unfavorite ideas and reseting favorite ideas
        /// </summary>
        [HarmonyPatch(typeof(GameScreen), "Awake")]
        public class ExtraButtonHarmonyPatches
        {
            public static void Postfix()
            {
                GameObject filterOptionParent = new GameObject();
                GameObject expandableLabelObj = UnityEngine.Object.Instantiate(PrefabManager.instance.AchievementElementLabelPrefab);

                expandableLabelObj.transform.SetParent(GameScreen.instance.IdeaSearchField.transform.parent.parent);
                expandableLabelObj.transform.SetSiblingIndex(1);

                filterOptionParent.transform.SetParent(GameScreen.instance.IdeaSearchField.transform.parent.parent);
                filterOptionParent.transform.SetSiblingIndex(2);

                ExpandableLabel expandableLabel = expandableLabelObj.GetComponent<ExpandableLabel>();
                expandableLabel.SetText("Filter Options");
                expandableLabel.Children.Add(filterOptionParent);
                /*expandableLabel.SetCallback(delegate {
                    expandableLabel.IsExpanded = !expandableLabel.IsExpanded;
                    filterOptionParent.SetActive(expandableLabel.IsExpanded);
                });*/

                VerticalLayoutGroup filterOptionParentLayout = filterOptionParent.AddComponent<VerticalLayoutGroup>();
                filterOptionParentLayout.childForceExpandHeight = false;
                filterOptionParentLayout.childForceExpandWidth = false;
                filterOptionParentLayout.childControlHeight = true;
                filterOptionParentLayout.childControlWidth = true;
                filterOptionParentLayout.spacing = 10;


                // add first row
                HorizontalLayoutGroup filterOptionRowALayout = AddOptionRow(filterOptionParentLayout.transform);

                pinFilterBtn = AddOptionBtn(filterOptionRowALayout.transform);
                pinFilterBtn.TextMeshPro.fontSize = 25;
                pinFilterBtn.TooltipText = "Filter pinned ideas.";
                pinFilterBtn.Clicked += delegate
                {
                    // toggle HideUnfavor boolean
                    //HideUnfavor.Value = !HideUnfavor.Value;
                    PinFilterOn = !PinFilterOn;
                    // update UI
                    GameScreen.instance.UpdateIdeasLog();
                    // save
                    //SaveConfig();
                };

                // reset favorite idea button
                resetPinBtn = AddOptionBtn(filterOptionRowALayout.transform);
                resetPinBtn.TextMeshPro.fontSize = 25;
                resetPinBtn.TooltipText = "Reset pinned ideas.";
                resetPinBtn.Clicked += delegate
                {
                    // reset HideUnfavor boolean
                    //HideUnfavor.Value = false;
                    PinFilterOn = false;
                    // reset favorite ideas
                    PinIdeaMod.ResetPin();
                    // update UI
                    GameScreen.instance.UpdateIdeasLog();
                    // save
                    PinIdeaMod.SaveConfig();
                };

                // second row
                HorizontalLayoutGroup filterOptionRowBLayout = AddOptionRow(filterOptionParentLayout.transform);

                quickFilterBtn = AddOptionBtn(filterOptionRowBLayout.transform);
                quickFilterBtn.TextMeshPro.fontSize = 25;
                quickFilterBtn.TooltipText = "Filter quick search results.";
                quickFilterBtn.Clicked += delegate
                {
                    // toggle HideUnfavor boolean
                    //HideUnfavor.Value = !HideUnfavor.Value;
                    QuickFilterOn = !QuickFilterOn;
                    // update UI
                    GameScreen.instance.UpdateIdeasLog();
                    // save
                    //SaveConfig();
                };

                newFilterBtn = AddOptionBtn(filterOptionRowBLayout.transform);
                newFilterBtn.TextMeshPro.fontSize = 25;
                newFilterBtn.TooltipText = "Filter new ideas.";
                newFilterBtn.Clicked += delegate
                {
                    // toggle HideUnfavor boolean
                    //HideUnfavor.Value = !HideUnfavor.Value;
                    NewFilterOn = !NewFilterOn;
                    // update UI
                    GameScreen.instance.UpdateIdeasLog();
                    // save
                    //SaveConfig();
                };
            }
        }

        static HorizontalLayoutGroup AddOptionRow(Transform parent)
        {
            GameObject filterOptionRow = new GameObject();
            filterOptionRow.transform.SetParent(parent);
            HorizontalLayoutGroup filterOptionRowLayout = filterOptionRow.AddComponent<HorizontalLayoutGroup>();
            filterOptionRowLayout.childForceExpandHeight = false;
            filterOptionRowLayout.childForceExpandWidth = false;
            filterOptionRowLayout.childControlHeight = true;
            filterOptionRowLayout.childControlWidth = true;
            filterOptionRowLayout.spacing = 10;
            return filterOptionRowLayout;
        }

        static CustomButton AddOptionBtn(Transform parent)
        {
            GameObject BtnObj = GameObject.Instantiate(GameScreen.instance.IdeasButton.gameObject);
            BtnObj.transform.SetParent(parent);
            BtnObj.AddComponent<LayoutElement>();
            BtnObj.GetComponent<LayoutElement>().minWidth = 150;
            BtnObj.GetComponent<LayoutElement>().preferredWidth = 150;

            return BtnObj.GetComponent<CustomButton>();
        }


        /// <summary>
        /// Renders content for extra buttons
        /// can only be done later than Awake or else the text doesnt change
        /// </summary>
        [HarmonyPatch(typeof(GameScreen), "Update")]
        public class RenderExtraButtonHarmonyPatches
        {
            public static void Prefix()
            {
                pinFilterBtn.TextMeshPro.text = "Pinned";
                pinFilterBtn.Image.color = (PinFilterOn ?
                                                ColorManager.instance.HoverButtonColor :
                                                ColorManager.instance.DisabledColor);
                pinFilterBtn.TextMeshPro.color = (PinFilterOn ?
                                                    ColorManager.instance.ButtonTextColor :
                                                    ColorManager.instance.DisabledButtonTextColor);

                resetPinBtn.TextMeshPro.text = "Reset Pin";
                resetPinBtn.Image.color = ColorManager.instance.ButtonColor;
                resetPinBtn.TextMeshPro.color = ColorManager.instance.ButtonTextColor;

                quickFilterBtn.TextMeshPro.text = "Quick";
                quickFilterBtn.Image.color = (QuickFilterOn ?
                                                ColorManager.instance.HoverButtonColor :
                                                ColorManager.instance.DisabledColor);
                quickFilterBtn.TextMeshPro.color = (QuickFilterOn ?
                                                    ColorManager.instance.ButtonTextColor :
                                                    ColorManager.instance.DisabledButtonTextColor);

                newFilterBtn.TextMeshPro.text = "New";
                newFilterBtn.Image.color = (NewFilterOn ?
                                                ColorManager.instance.HoverButtonColor :
                                                ColorManager.instance.DisabledColor);
                newFilterBtn.TextMeshPro.color = (NewFilterOn ?
                                                    ColorManager.instance.ButtonTextColor :
                                                    ColorManager.instance.DisabledButtonTextColor);
            }
        }

        public static bool FilterOn()
        {
            return PinFilterOn || QuickFilterOn || NewFilterOn;
        }

        [HarmonyPatch(typeof(GameScreen), "UpdateIdeasLog")]
        public class UpdateIdeasLogHarmonyPatches
        {
            public static void Postfix(GameScreen __instance, List<ExpandableLabel> ___ideaLabels, List<IdeaElement> ___ideaElements)
            {
                string searchTerm = "";
                if (!string.IsNullOrEmpty(__instance.IdeaSearchField.text))
                {
                    searchTerm = __instance.IdeaSearchField.text;
                }

                Dictionary<object, bool> dictionary = __instance.wasExpandedDict(__instance.IdeaElementsParent.GetComponentsInChildren<ExpandableLabel>());

                foreach (IdeaElement element in ___ideaElements)
                {
                    //element.SetKnowledge(list.Find((IKnowledge x) => x.CardId == element.MyKnowledge.CardId));
                    if (__instance.KnowledgeWasFound(element.MyKnowledge))
                    {
                        string cardId = element.MyKnowledge.CardId;
                        if (element.IsNew)
                        {
                            dictionary[element.MyKnowledge.Group] = true;
                        }
                        if (
                            // consider to display
                            // when element is hovered
                            (element.MyButton.IsHovered || element.MyButton.IsSelected) ||
                            // or when the filter is off
                            !FilterOn() ||
                            // or when some filter is satisfied
                            (
                                // or when element is pinned
                                (PinFilterOn && PinIdeaMod.IsFidea(cardId)) ||
                                // or when element is quick searched
                                (QuickFilterOn && AdvancedQuickSearchMod.IsQuickSearchResult(cardId)) ||
                                // or when element is new
                                (NewFilterOn && element.IsNew)
                            )
                        )
                        {
                            // if in the search mode
                            if (!string.IsNullOrEmpty(searchTerm))
                            {
                                // display when the element is search result
                                if (__instance.searchKnowledge(element.MyKnowledge, searchTerm))
                                {
                                    element.gameObject.SetActive(value: true);
                                    continue;
                                }
                            }
                            // otherwise display only when it is expanded
                            else if (dictionary.ContainsKey(element.MyKnowledge.Group) && dictionary[element.MyKnowledge.Group])
                            {
                                element.gameObject.SetActive(value: true);
                                continue;
                            }
                        }
                    }
                    element.gameObject.SetActive(value: false);
                }

                foreach (ExpandableLabel ideaLabel in ___ideaLabels)
                {
                    //ideaLabel.SetText(GetBlueprintGroupText((BlueprintGroup)ideaLabel.Tag));
                    if (ideaLabel.Children.Count((GameObject x) => __instance.hasFoundKnowledge(x)) > 0)
                    {
                        if (string.IsNullOrEmpty(searchTerm))
                        {
                            ideaLabel.gameObject.SetActive(value: true);
                            ideaLabel.IsExpanded = dictionary.ContainsKey(ideaLabel.Tag) && dictionary[ideaLabel.Tag];
                            continue;
                        }
                        if (ideaLabel.Children.Count((GameObject x) => __instance.hasFoundKnowledge(x) && __instance.searchKnowledge(x.GetComponent<IdeaElement>().MyKnowledge, searchTerm)) > 0)
                        {
                            ideaLabel.gameObject.SetActive(value: true);
                            ideaLabel.IsExpanded = true;
                            continue;
                        }
                    }
                    ideaLabel.gameObject.SetActive(value: false);
                }
            }
        }
    }
}