using HarmonyLib;
using System;
using UnityEngine;

namespace BetterSideBarNS
{
    public static class HideUnhoveredCoroutine
    {
        public static IdeaElement hidingUnhoveredIdea;
        public static event Action hidingUnhoveredIdeaCallback;

        public static void InterruptCoroutine()
        {
            hidingUnhoveredIdea = null;
        }

        public static void StartCoroutine(IdeaElement element, Action callback)
        {
            // prevent reentry
            if (hidingUnhoveredIdea != null) return;
            hidingUnhoveredIdea = element;
            hidingUnhoveredIdeaCallback = callback;
        }

        public static void InvokeCallback()
        {
            hidingUnhoveredIdeaCallback?.Invoke();
        }

        /// <summary>
        /// Running the condition checker for HideUnhoveredCoroutine
        /// </summary>
        [HarmonyPatch(typeof(GameScreen), "Update")]
        public class HideUnhoveredCoroutineHarmonyPatches
        {
            public static void Postfix()
            {
                // when a valid hovered idea is waiting to hide,
                // and it is no longer hovered/selected/new, then hide it
                if (hidingUnhoveredIdea != null && !hidingUnhoveredIdea.MyButton.IsHovered &&
                    !hidingUnhoveredIdea.MyButton.IsSelected && !hidingUnhoveredIdea.IsNew)
                {
                    hidingUnhoveredIdea = null;
                    InvokeCallback();
                }
            }
        }
    }
}