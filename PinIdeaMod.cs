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
    public static class PinIdeaMod
    {
        private static ModLogger L;
        private static ConfigFile C;

        private const string FileSeparator = ",";

        // MARK: choose to persist names of favorite ideas
        // because BlueprintDB.IdeaElements may change between different runs
        private static ConfigEntry<string> __fideas;
        private static List<string> Fideas;

        //public static ConfigEntry<bool> HideUnfavor;

        // array recording wether each idea is favorite
        // same shape as BlueprintDB.IdeaElements
        private static bool[] isFidea;
        // the index (in BlueprintDB.IdeaElements) of the first element in a group
        private static Dictionary<BlueprintGroup, int> groupIdxMap;
        // number of favorite ideas in a group
        private static Dictionary<BlueprintGroup, int> groupFNumMap;
        // the index (in UI) of the first element in a group
        private static Dictionary<BlueprintGroup, int> groupUIIdxMap;
        // maps cardId of an idea element to its index in isFidea
        private static Dictionary<string, int> elementIdToIdx;

        //private static List<BlueprintGroup> BlueprintDB.BlueprintGroups;
        //private static List<IdeaElement> BlueprintDB.IdeaElements;

        private static Sprite FavorIcon;


        // callback for reseting favorite ideas
        public static event Action ResetFavor;

        public static bool IsFidea(string cardId)
        {
            if (elementIdToIdx.ContainsKey(cardId))
            {
                return isFidea[elementIdToIdx[cardId]];
            }
            return false;
        }

        public static void Initialize(ModLogger logger, ConfigFile config)
        {
            L = logger;
            C = config;
            
            // initialize favorite list
            __fideas = C.GetEntry<string>("favorite_ideas", "");
            __fideas.UI.Hidden = true;
            Fideas = __fideas.Value.Split(FileSeparator).ToList();
            Fideas.Remove("");

            /*
            // initialize hide unfavor toggle
            HideUnfavor = C.GetEntry<bool>("hide_unfavor", false);
            HideUnfavor.UI.Name = "Pinned Only";
            //HideUnfavor.UI.NameTerm = "pinned_only_name";
            HideUnfavor.UI.Tooltip = "When turned on, only pinned ideas will display.";
            HideUnfavor.UI.Hidden = true;*/

            groupIdxMap = new Dictionary<BlueprintGroup, int>();
            groupFNumMap = new Dictionary<BlueprintGroup, int>();
            groupUIIdxMap = new Dictionary<BlueprintGroup, int>();
            elementIdToIdx = new Dictionary<string, int>();

            // load assets
            Mod m = new Mod();
            ModManager.TryGetMod("better_sidebar", out m);
            L.Log(m.Path);
            FavorIcon = ResourceHelper.LoadSpriteFromPath(m.Path + "/Icons/icon-pin.png");
        }

        /// <summary>
        /// Initialize idea elements with the favoring functionality
        /// </summary>
        public static void InitIdeaElements(GameScreen __instance, List<IdeaElement> ___ideaElements,
            List<BlueprintGroup> ___groups, List<ExpandableLabel> ___ideaLabels)
        {
            // initialize group index
            BlueprintDB.BlueprintGroups = ___groups;
            for (int i = 0; i < BlueprintDB.BlueprintGroups.Count; i++)
            {
                BlueprintGroup group = BlueprintDB.BlueprintGroups[i];
                groupIdxMap.Add(group, 0);
                groupFNumMap.Add(group, 0);
                groupUIIdxMap.Add(group, ___ideaLabels[i].transform.GetSiblingIndex() + 1);
            }
            // initialize isFidea
            BlueprintDB.IdeaElements = ___ideaElements;
            isFidea = new bool[___ideaElements.Count];
            int[] initFideasIdx = new int[Fideas.Count];

            // Initialize each idea element with the favorite functionality
            for (int i = 0; i < ___ideaElements.Count; i++)
            {
                IdeaElement ideaElement = ___ideaElements[i];
                string cardId = ideaElement.MyKnowledge.CardId;
                int idx_fi = Fideas.IndexOf(cardId);
                // MARK: need to create a new local variable idx_ie
                // otherwise, i is a global variable w.r.t. to delegate below
                // i cannot faithfully reflect the true index of the element
                int idx_ie = i;

                if (!elementIdToIdx.ContainsKey(cardId))
                {
                    elementIdToIdx.Add(cardId, idx_ie);
                }
                isFidea[idx_ie] = Fideas.Contains(cardId);
                groupIdxMap[ideaElement.MyKnowledge.Group] += 1;

                // initialize favor icon
                GameObject favorLabel = GameObject.Instantiate(ideaElement.NewLabel);
                favorLabel.transform.SetParent(ideaElement.transform);
                favorLabel.transform.SetSiblingIndex(favorLabel.transform.GetSiblingIndex()-1);
                favorLabel.GetComponent<Image>().sprite = FavorIcon;
                favorLabel.SetActive(isFidea[idx_ie]);

                // favor action takes effect when right click on the hovered IdeaElement
                ideaElement.gameObject.GetComponent<CustomButton>().Clicked += delegate
                {
                    // remove from favorites
                    if (isFidea[idx_ie])
                    {
                        L.Log("Remove " + cardId);
                        isFidea[idx_ie] = false;
                        Fideas.Remove(cardId);
                        groupFNumMap[ideaElement.MyKnowledge.Group] -= 1;
                        // this unpin should happen only when the idea is unhovered
                        HideUnhoveredCoroutine.StartCoroutine(ideaElement, delegate
                        {
                            UnpinIdea(idx_ie, ideaElement);
                            /*
                            if (HideUnfavor.Value)
                            {
                                L.Log("Hide " + cardId);
                                ideaElement.gameObject.SetActive(false);
                                GameScreen.instance.UpdateIdeasLog();
                            }*/
                            GameScreen.instance.UpdateIdeasLog();
                        });
                        favorLabel.SetActive(false);
                    }
                    // add to favorites
                    else
                    {
                        // can only interrupt the hiding coroutine
                        // when another pin update happens
                        HideUnhoveredCoroutine.InterruptCoroutine();

                        L.Log("Add " + cardId);
                        isFidea[idx_ie] = true;
                        Fideas.Add(cardId);
                        groupFNumMap[ideaElement.MyKnowledge.Group] += 1;
                        PinIdea(idx_ie, ideaElement);
                        favorLabel.SetActive(true);
                    }
                    // save
                    SaveConfig();
                    // update UI
                    // UpdateIdea(idx_ie);
                    GameScreen.instance.UpdateIdeasLog();
                };

                ResetFavor += delegate
                {
                    if (isFidea[idx_ie])
                    {
                        L.Log("Remove " + cardId);
                        isFidea[idx_ie] = false;
                        Fideas.Remove(cardId);
                        groupFNumMap[ideaElement.MyKnowledge.Group] -= 1;
                        UnpinIdea(idx_ie, ideaElement);
                        favorLabel.SetActive(false);
                    }
                };

                // record idea elements need re-ordering
                if (idx_fi != -1)
                {
                    initFideasIdx[idx_fi] = idx_ie;
                }
            }

            // calculate the index (in ideaElement) of the first element in each group
            int total = ___ideaElements.Count;
            for (int i = BlueprintDB.BlueprintGroups.Count - 1; i >= 0; i--)
            {
                total -= groupIdxMap[BlueprintDB.BlueprintGroups[i]];
                groupIdxMap[BlueprintDB.BlueprintGroups[i]] = total;
            }

            // initial reodering
            foreach (int idx_ie in initFideasIdx)
            {
                groupFNumMap[___ideaElements[idx_ie].MyKnowledge.Group] += 1;
                PinIdea(idx_ie, ___ideaElements[idx_ie]);
            }
        }

        /// <summary>
        /// pin an idea and increase favorite number of the group
        /// </summary>
        static void PinIdea(int idx_ie, IdeaElement ie)
        {
            BlueprintGroup group = ie.MyKnowledge.Group;
            ie.transform.SetSiblingIndex(groupUIIdxMap[group] + groupFNumMap[group] - 1);
        }

        /// <summary>
        /// unpin an idea and increase favorite number of the group
        /// </summary>
        static void UnpinIdea(int idx_ie, IdeaElement ie)
        {
            BlueprintGroup group = ie.MyKnowledge.Group;
            int offset = 0;
            for (int i = groupIdxMap[group]; i < idx_ie; i++)
            {
                // need to offset by 1 when an element before it
                // is not included in pinned elements
                offset += (isFidea[i] ? 0 : 1);
            }
            ie.transform.SetSiblingIndex(groupUIIdxMap[group] + groupFNumMap[group] + offset);
        }

        public static void ResetPin()
        {
            ResetFavor?.Invoke();
        }

        /*
        [HarmonyPatch(typeof(GameScreen), "UpdateIdeasLog")]
        public class UpdateIdeasLogHarmonyPatches
        {
            public static void Postfix(GameScreen __instance, List<ExpandableLabel> ___ideaLabels)
            {
                for (int i = 0; i < BlueprintDB.IdeaElements.Count; i++)
                {
                    UpdateIdea(i);
                }
                if (!string.IsNullOrEmpty(__instance.IdeaSearchField.text))
                {
                    foreach (ExpandableLabel label in ___ideaLabels)
                    {
                        if (label.Children.Count((GameObject x) => x.activeSelf) <= 0)
                        {
                            label.gameObject.SetActive(value: false);
                            label.IsExpanded = false;
                        }
                    }
                }
            }
        }
        */

        /// <summary>
        /// In the Pinned Only mode, additional checking and updates are required
        /// to keep new ideas and just unfavored ideas visible
        /// </summary>
        /*
        static void UpdateIdea(int idx_ie)
        {
            IdeaElement element = BlueprintDB.IdeaElements[idx_ie];
            // element's activity differs from the default only when HideUnfavor is true
            if (HideUnfavor.Value)
            {
                // element is active only when
                // - it is active originally,
                //      otherwise unseen ones or not searched ones will show up
                // - it is favorite
                // - it is new
                // - it is hovered or clicked
                // TODO: this could be implemented better if there is a way to add
                // an oneshot callback to the element or its button's update
                element.gameObject.SetActive(
                    element.gameObject.activeSelf && 
                    (isFidea[idx_ie] || element.IsNew ||
                    element.MyButton.IsHovered || element.MyButton.IsSelected)
                );
                // when the element is active even when it is not favorite,
                // it is then displayed due to other conditions,
                // need a coroutine to wait until other conditons are no longer valid and hide it
                if (element.gameObject.activeSelf && !isFidea[idx_ie] && 
                    (element.MyButton.IsHovered || element.MyButton.IsSelected || element.IsNew))
                {
                    HideUnhoveredCoroutine.StartCoroutine(element, delegate {
                        element.gameObject.SetActive(false);
                        GameScreen.instance.UpdateIdeasLog();
                    });
                }
            }
        }*/

        /// <summary>
        /// Save configuration
        /// </summary>
        public static void SaveConfig()
        {
            // store favorites
            __fideas.Value = string.Join(FileSeparator, Fideas);
            // save
            C.Save();
        }
    }
}