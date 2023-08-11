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
        
        private static ConfigEntry<string> __fideas;
        public static List<string> FIdeas;

        private static bool initiatingIdeaElements;

        public static Sprite FavorIcon;

        private void Awake()
        {
            Harmony harmony = new Harmony("better_sidebar");
            harmony.PatchAll();

            __fideas = Config.GetEntry<string>("favorite_ideas", "");
            //__fideas.UI.Hidden = true;
            FIdeas = ParseFromString(__fideas.Value);

            L = Logger;
            L.Log("Awake");

            initiatingIdeaElements = false;


            Mod m = new Mod();
            ModManager.TryGetMod("better_sidebar", out m);
            L.Log(m.Path);
            FavorIcon = ResourceHelper.LoadSpriteFromPath(m.Path + "/Icons/icons-pin.png");
        }

        public override void Ready()
        {
            L.Log("Ready");
        }

        [HarmonyPatch(typeof(GameScreen), "InitIdeaElements")]
        public class FavoriteButtonAssitHarmonyPatches
        {
            public static void Prefix()
            {
                initiatingIdeaElements = true;
            }

            public static void Postfix()
            {
                initiatingIdeaElements = false;
            }
        }

        [HarmonyPatch(typeof(IdeaElement), "SetKnowledge")]
        public class FavoriteButtonHarmonyPatches
        {
            public static void Postfix(IdeaElement __instance)
            {
                if (initiatingIdeaElements)
                {
                    string cardId = __instance.MyKnowledge.CardId;

                    // initialize favor icon
                    GameObject favorLabel = GameObject.Instantiate(__instance.NewLabel);
                    favorLabel.transform.SetParent(__instance.transform);
                    favorLabel.GetComponent<Image>().sprite = FavorIcon;
                    favorLabel.SetActive(FIdeas.Contains(cardId));
                    

                    // favor action takes effect when right click on the hovered IdeaElement
                    __instance.gameObject.GetComponent<CustomButton>().Clicked += delegate
                    {
                        if (FIdeas.Contains(cardId))
                        {
                            L.Log("Remove " + cardId);
                            FIdeas.Remove(cardId);
                            favorLabel.SetActive(false);
                        }
                        // add to favorites
                        else
                        {
                            L.Log("Add " + cardId);
                            FIdeas.Add(cardId);
                            favorLabel.SetActive(true);
                        }
                        // update persisted data
                        __fideas.Value = string.Join(",", FIdeas);
                    };
                }
            }
        }

        List<string> ParseFromString(string str)
        {
            List<string> strs = str.Split(",").ToList();
            strs.Remove("");
            return strs;
        }

        string ParseIntoString(List<string> strs)
        {
            return string.Join(",", strs);
        }
    }
}