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
        private void Awake()
        {
            // initialize harmony
            Harmony harmony = new Harmony("better_sidebar");
            harmony.PatchAll();

            PinIdeaMod.Initialize(Logger, Config);
        }

        public override void Ready()
        {

        }
    }
}