using HarmonyLib;
using System;
using UnityEngine;

namespace BetterSideBarNS
{
    public class BetterSideBar : Mod
    {
        private void Awake()
        {
            // initialize harmony
            Harmony harmony = new Harmony("better_sidebar");
            harmony.PatchAll();

            // initialize blueprint database
            BlueprintDB.Initialize(Logger, Config);
            // initialize pin idea mod
            PinIdeaMod.Initialize(Logger, Config);
            // initialize advanced search mods
            AdvancedQuickSearchMod.Initialize(Logger, Config);
            //AdvancedSearchBarMod.Initialize(Logger, Config);    // not implemented yet
            SidebarDisplayControl.Initialize(Logger, Config);
        }

        public override void Ready()
        {

        }
    }
}