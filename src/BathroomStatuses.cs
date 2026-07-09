using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace INeedToPEEak
{
    /// <summary>
    /// Registers the three custom stamina-bar statuses (Poo, Pee, Dirty) by extending
    /// CharacterAfflictions' status arrays past the vanilla enum, the same technique
    /// PEAKLib.Stats uses. Because the vanilla SyncStatusesRPC serializes the whole
    /// array, the custom statuses ride the game's own multiplayer sync for free
    /// (every player must run this mod, which the custom items require anyway).
    /// </summary>
    internal static class BathroomStatuses
    {
        public const int VanillaCount = 12; // Injury..Web in the current build
        public static readonly CharacterAfflictions.STATUSTYPE Poo = (CharacterAfflictions.STATUSTYPE)VanillaCount;
        public static readonly CharacterAfflictions.STATUSTYPE Pee = (CharacterAfflictions.STATUSTYPE)(VanillaCount + 1);
        public static readonly CharacterAfflictions.STATUSTYPE Dirty = (CharacterAfflictions.STATUSTYPE)(VanillaCount + 2);
        public static readonly CharacterAfflictions.STATUSTYPE Stink = (CharacterAfflictions.STATUSTYPE)(VanillaCount + 3);
        public const int TotalCount = VanillaCount + 4;

        public static readonly Color PooColor = new Color(0.45f, 0.27f, 0.07f);   // brown
        public static readonly Color PeeColor = new Color(0.93f, 0.85f, 0.21f);   // yellow
        public static readonly Color DirtyColor = new Color(0.55f, 0.55f, 0.53f); // grey
        public static readonly Color StinkColor = new Color(0.55f, 0.6f, 0.18f);  // sickly olive

        private static float[] Grow(float[] arr)
        {
            if (arr == null || arr.Length >= TotalCount) return arr;
            var bigger = new float[TotalCount];
            Array.Copy(arr, bigger, arr.Length);
            return bigger;
        }

        [HarmonyPatch(typeof(CharacterAfflictions), "InitStatusArrays")]
        private static class Patch_InitStatusArrays
        {
            private static void Postfix(CharacterAfflictions __instance)
            {
                __instance.currentStatuses = Grow(__instance.currentStatuses);
                __instance.currentIncrementalStatuses = Grow(__instance.currentIncrementalStatuses);
                __instance.currentDecrementalStatuses = Grow(__instance.currentDecrementalStatuses);
                __instance.lastAddedStatus = Grow(__instance.lastAddedStatus);
                __instance.lastAddedIncrementalStatus = Grow(__instance.lastAddedIncrementalStatus);
            }
        }

        /// <summary>Poo/Pee/Dirty can never exceed one full bar.</summary>
        [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.GetStatusCap))]
        private static class Patch_GetStatusCap
        {
            private static void Postfix(CharacterAfflictions.STATUSTYPE type, ref float __result)
            {
                if ((int)type >= VanillaCount)
                {
                    __result = 1f;
                }
            }
        }

        /// <summary>Adds the three custom segments to the stamina-bar UI by cloning a vanilla one.</summary>
        [HarmonyPatch(typeof(StaminaBar), "Start")]
        private static class Patch_StaminaBarStart
        {
            private static void Postfix(StaminaBar __instance)
            {
                try
                {
                    if (__instance.afflictions == null || __instance.afflictions.Length == 0) return;
                    // Already extended (e.g. scene reload with a persistent bar)?
                    foreach (var existing in __instance.afflictions)
                    {
                        if ((int)existing.afflictionType >= VanillaCount) return;
                    }

                    BarAffliction template = __instance.afflictions[0];
                    var list = new List<BarAffliction>(__instance.afflictions);
                    list.Add(CloneSegment(template, Poo, PooColor, BathroomAssets.PooIcon));
                    list.Add(CloneSegment(template, Pee, PeeColor, BathroomAssets.PeeIcon));
                    list.Add(CloneSegment(template, Dirty, DirtyColor, BathroomAssets.DirtyIcon));
                    list.Add(CloneSegment(template, Stink, StinkColor, BathroomAssets.StinkIcon));
                    __instance.afflictions = list.ToArray();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to extend stamina bar UI: {e}");
                }
            }

            private static BarAffliction CloneSegment(BarAffliction template, CharacterAfflictions.STATUSTYPE type, Color color, Sprite icon)
            {
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
                clone.name = $"BarAffliction_INTP_{(int)type}";
                var seg = clone.GetComponent<BarAffliction>();
                seg.afflictionType = type;
                foreach (var img in clone.GetComponentsInChildren<Image>(true))
                {
                    if (seg.icon != null && img == seg.icon)
                    {
                        // Colors are baked into the icon sprite; keep the Image tint neutral.
                        img.sprite = icon;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = color;
                    }
                }
                clone.SetActive(false);
                return seg;
            }
        }
    }
}
