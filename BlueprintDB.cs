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
    public static class BlueprintDB
    {
        private static ModLogger L;
        private static ConfigFile C;

        // mapping from result card to blueprints
        public static Dictionary<string, List<string>> ResultBPMap;
        // mapping from ingredient to blueprints
        public static Dictionary<string, List<string>> IngredBPMap;

        // TODO: these two are initialized in PinIdeaMod
        // because PinIdeaMod depend on them
        // still finding a way to initialize them here and ensure initialization order
        public static List<BlueprintGroup> BlueprintGroups;
        public static List<IdeaElement> IdeaElements;

        public static void Initialize(ModLogger logger, ConfigFile config)
        {
            L = logger;
            C = config;

            ResultBPMap = new Dictionary<string, List<string>>();
            IngredBPMap = new Dictionary<string, List<string>>();
        }

        static bool AddUniqueEntry(ref Dictionary<string, List<string>> dict, string key, string value)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, new List<string>());
            if (dict[key].Contains(value))
                return false;
            dict[key].Add(value);
            return true;
        }

        [HarmonyPatch(typeof(Blueprint), "Init")]
        public class BlueprintDataAcquisitionHarmonyPatches
        {
            public static void Postfix(Blueprint __instance)
            {
                // skip if the blueprint has not id
                if (string.IsNullOrWhiteSpace(__instance.CardId)) return;
                foreach (Subprint print in __instance.Subprints)
                {
                    // update ResultBPMap
                    if (!string.IsNullOrWhiteSpace(print.ResultCard))
                    {
                        AddUniqueEntry(ref ResultBPMap, print.ResultCard, __instance.CardId);
                    }
                    // do not record result action
                    //if (!string.IsNullOrWhiteSpace(print.ResultAction))
                    //    L.Log("\tResult Action:" + print.ResultAction);
                    foreach (string card in print.ExtraResultCards)
                    {
                        if (!string.IsNullOrWhiteSpace(card))
                        {
                            AddUniqueEntry(ref ResultBPMap, card, __instance.CardId);
                        }
                    }

                    // update IngredBPMap
                    foreach (string card in print.RequiredCards)
                    {
                        if (!string.IsNullOrWhiteSpace(card))
                        {
                            AddUniqueEntry(ref IngredBPMap, card, __instance.CardId);
                        }
                    }
                }
            }
        }
    }
}