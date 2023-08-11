using HarmonyLib;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSideBarNS
{
    public class BetterSideBar : Mod
    {
        public static ModLogger L;
        public static ConfigFile C;

        public const string FileSeparator = ",";

        // MARK: choose to persist names of favorite ideas
        // because ideaElements may change between different runs
        private static ConfigEntry<string> __fideas;
        public static List<string> FIdeas;
        // array recording wether each idea is favorite
        // same shape as ideaElements
        private static bool[] isFidea;
        // the index (in ideaElements) of the first element in a group
        private static Dictionary<BlueprintGroup, int> groupIdxMap;
        // number of favorite ideas in a group
        private static Dictionary<BlueprintGroup, int> groupFNumMap;
        // the index (in UI) of the first element in a group
        private static Dictionary<BlueprintGroup, int> groupUIIdxMap;
        // the ordered list of all group names
        private static List<BlueprintGroup> groups;

        public static Sprite FavorIcon;

        private void Awake()
        {
            L = Logger;
            L.Log("Awake");

            // initialize harmony
            Harmony harmony = new Harmony("better_sidebar");
            harmony.PatchAll();

            C = Config;
            // initialize favorite list
            __fideas = Config.GetEntry<string>("favorite_ideas", "");
            //__fideas.UI.Hidden = true;
            FIdeas = __fideas.Value.Split(FileSeparator).ToList();
            FIdeas.Remove("");

            groupIdxMap = new Dictionary<BlueprintGroup, int>();
            groupFNumMap = new Dictionary<BlueprintGroup, int>();
            groupUIIdxMap = new Dictionary<BlueprintGroup, int>();

            // load assets
            Mod m = new Mod();
            ModManager.TryGetMod("better_sidebar", out m);
            L.Log(m.Path);
            FavorIcon = ResourceHelper.LoadSpriteFromPath(m.Path + "/Icons/icons-pin.png");
        }

        public override void Ready()
        {
            L.Log("Ready");
        }

        /// <summary>
        /// Set up the scope to initialize idea elemets 
        /// and initial re-ordering of idea elements
        /// </summary>
        [HarmonyPatch(typeof(GameScreen), "InitIdeaElements")]
        public class FavoriteButtonAssitHarmonyPatches
        {
            public static void Postfix(GameScreen __instance, List<IdeaElement> ___ideaElements,
                List<BlueprintGroup> ___groups, List<ExpandableLabel> ___ideaLabels)
            {
                // initialize group index
                groups = ___groups;
                for (int i=0; i<groups.Count; i++)
                {
                    BlueprintGroup group = groups[i];
                    groupIdxMap.Add(group, 0);
                    groupFNumMap.Add(group, 0);
                    groupUIIdxMap.Add(group, ___ideaLabels[i].transform.GetSiblingIndex() + 1);
                }
                // initialize isFidea
                isFidea = new bool[___ideaElements.Count];
                int[] initFideasIdx = new int[FIdeas.Count];

                // Initialize each idea element with the favorite functionality
                for (int i=0; i<___ideaElements.Count; i++)
                {
                    IdeaElement ideaElement = ___ideaElements[i];
                    string cardId = ideaElement.MyKnowledge.CardId;
                    int idx_fi = FIdeas.IndexOf(cardId);
                    // MARK: need to create a new local variable idx_ie
                    // otherwise, i is a global variable w.r.t. to delegate below
                    // i cannot faithfully reflect the true index of the element
                    int idx_ie = i;

                    isFidea[idx_ie] = FIdeas.Contains(cardId);
                    groupIdxMap[ideaElement.MyKnowledge.Group] += 1;

                    // initialize favor icon
                    GameObject favorLabel = GameObject.Instantiate(ideaElement.NewLabel);
                    favorLabel.transform.SetParent(ideaElement.transform);
                    favorLabel.GetComponent<Image>().sprite = FavorIcon;
                    favorLabel.SetActive(isFidea[idx_ie]);

                    // favor action takes effect when right click on the hovered IdeaElement
                    ideaElement.gameObject.GetComponent<CustomButton>().Clicked += delegate
                    {
                        // remove from favorites
                        L.Log(idx_ie.ToString());
                        if (isFidea[idx_ie])
                        {
                            L.Log("Remove " + cardId);
                            isFidea[idx_ie] = false;
                            FIdeas.Remove(cardId);
                            UnpinIdea(idx_ie, ideaElement);
                            favorLabel.SetActive(false);
                        }
                        // add to favorites
                        else
                        {
                            L.Log("Add " + cardId);
                            isFidea[idx_ie] = true;
                            FIdeas.Add(cardId);
                            PinIdea(idx_ie, ideaElement);
                            favorLabel.SetActive(true);
                        }
                        // update persisted data
                        __fideas.Value = string.Join(FileSeparator, FIdeas);
                        C.Save();
                    };

                    // record idea elements need re-ordering
                    if (idx_fi != -1)
                    {
                        initFideasIdx[idx_fi] = idx_ie;
                    }
                }

                // calculate the index (in ideaElement) of the first element in each group
                int total = ___ideaElements.Count;
                for (int i=groups.Count-1; i>=0; i--)
                {
                    total -= groupIdxMap[groups[i]];
                    groupIdxMap[groups[i]] = total;
                }

                // initial reodering
                foreach (int idx_ie in initFideasIdx)
                {
                    PinIdea(idx_ie, ___ideaElements[idx_ie]);
                }
            }
        }

        // pin an idea and increase favorite number of the group
        static void PinIdea(int idx_ie, IdeaElement ie)
        {
            BlueprintGroup group = ie.MyKnowledge.Group;
            ie.transform.SetSiblingIndex(groupUIIdxMap[group] + groupFNumMap[group]);
            groupFNumMap[group] += 1;
        }

        // unpin an idea and increase favorite number of the group
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
            groupFNumMap[group] -= 1;
            ie.transform.SetSiblingIndex(groupUIIdxMap[group] + groupFNumMap[group] + offset);
        }
    }
}