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
        // Read the vanilla status count from the game at runtime rather than hardcoding
        // it. PEAK 2.0 added Arrow/Petrify/FlyTrap, which would have collided with the
        // old hardcoded indices — this way a game update that adds statuses shifts ours
        // up automatically instead of corrupting them.
        public static readonly int VanillaCount = Enum.GetNames(typeof(CharacterAfflictions.STATUSTYPE)).Length;
        public static readonly CharacterAfflictions.STATUSTYPE Poo = (CharacterAfflictions.STATUSTYPE)VanillaCount;
        public static readonly CharacterAfflictions.STATUSTYPE Pee = (CharacterAfflictions.STATUSTYPE)(VanillaCount + 1);
        public static readonly CharacterAfflictions.STATUSTYPE Dirty = (CharacterAfflictions.STATUSTYPE)(VanillaCount + 2);
        public static readonly CharacterAfflictions.STATUSTYPE Stink = (CharacterAfflictions.STATUSTYPE)(VanillaCount + 3);
        public static readonly int TotalCount = VanillaCount + 4;

        public static readonly Color PooColor = new Color(0.45f, 0.27f, 0.07f);   // brown
        public static readonly Color PeeColor = new Color(0.93f, 0.85f, 0.21f);   // yellow
        public static readonly Color DirtyColor = new Color(0.55f, 0.55f, 0.53f); // grey
        public static readonly Color StinkColor = new Color(0.55f, 0.6f, 0.18f);  // sickly olive

        /// <summary>
        /// PEAK stores statuses in 2.5% chunks: AddStatus banks anything smaller in a
        /// hidden accumulator and only moves the bar once it crosses a whole chunk. That
        /// makes small snacks look like they did nothing ("the first cookie didn't work"),
        /// even though the value isn't lost. Rounding a gain up to a whole chunk means
        /// every meal or drink visibly registers straight away.
        /// </summary>
        public const float Chunk = 0.025f;

        public static float ChunkUp(float amount)
        {
            if (amount <= 0f) return 0f;
            return Mathf.Ceil(amount / Chunk) * Chunk;
        }

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
            private static bool logged;

            private static void Postfix(CharacterAfflictions __instance)
            {
                __instance.currentStatuses = Grow(__instance.currentStatuses);
                __instance.currentIncrementalStatuses = Grow(__instance.currentIncrementalStatuses);
                __instance.currentDecrementalStatuses = Grow(__instance.currentDecrementalStatuses);
                __instance.lastAddedStatus = Grow(__instance.lastAddedStatus);
                __instance.lastAddedIncrementalStatus = Grow(__instance.lastAddedIncrementalStatus);
                if (!logged)
                {
                    logged = true;
                    Plugin.Log.LogInfo($"Status arrays grown to {__instance.currentStatuses.Length} " +
                                       $"(vanilla {VanillaCount}); Poo={(int)Poo} Pee={(int)Pee} Dirty={(int)Dirty} Stink={(int)Stink}");
                }
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

                    var list = new List<BarAffliction>(__instance.afflictions);
                    bool alreadyExtended = false;
                    foreach (var existing in __instance.afflictions)
                    {
                        if ((int)existing.afflictionType >= VanillaCount) { alreadyExtended = true; break; }
                    }

                    // StaminaBar.Start rebuilds `afflictions` from ACTIVE children only, so
                    // our (hidden) segments drop out of the array on a rebuild. Re-attach any
                    // that already exist instead of cloning duplicates.
                    if (!alreadyExtended)
                    {
                        var parent = __instance.afflictions[0].transform.parent;
                        var survivors = new Dictionary<int, BarAffliction>();
                        foreach (var seg in parent.GetComponentsInChildren<BarAffliction>(true))
                        {
                            if (seg.gameObject.name.StartsWith(ClonePrefix))
                            {
                                survivors[(int)seg.afflictionType] = seg;
                            }
                        }

                        // Never use the Petrify bar as a template: PEAK 2.0 added
                        // BarAffliction.isPetrify, and a bar with it set reads petrifyAmount
                        // instead of its status — cloning that makes our segments invisible.
                        BarAffliction template = null;
                        foreach (var candidate in __instance.afflictions)
                        {
                            if (!candidate.isPetrify) { template = candidate; break; }
                        }
                        if (template == null) template = __instance.afflictions[0];

                        list.Add(GetOrClone(survivors, template, Poo, PooColor, BathroomAssets.PooIcon));
                        list.Add(GetOrClone(survivors, template, Pee, PeeColor, BathroomAssets.PeeIcon));
                        list.Add(GetOrClone(survivors, template, Dirty, DirtyColor, BathroomAssets.DirtyIcon));
                        list.Add(GetOrClone(survivors, template, Stink, StinkColor, BathroomAssets.StinkIcon));
                        __instance.afflictions = list.ToArray();

                        Plugin.Log.LogInfo($"Stamina bar extended: vanilla={VanillaCount} segments, " +
                                           $"Poo={(int)Poo} Pee={(int)Pee} Dirty={(int)Dirty} Stink={(int)Stink}, " +
                                           $"template='{template.gameObject.name}', total={__instance.afflictions.Length}");
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to extend stamina bar UI: {e}");
                }
            }

            private const string ClonePrefix = "BarAffliction_INTP_";

            private static BarAffliction GetOrClone(Dictionary<int, BarAffliction> survivors, BarAffliction template,
                CharacterAfflictions.STATUSTYPE type, Color color, Sprite icon)
            {
                if (survivors.TryGetValue((int)type, out var existing) && existing != null) return existing;
                return CloneSegment(template, type, color, icon);
            }

            private static BarAffliction CloneSegment(BarAffliction template, CharacterAfflictions.STATUSTYPE type, Color color, Sprite icon)
            {
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
                clone.name = ClonePrefix + (int)type;
                var seg = clone.GetComponent<BarAffliction>();
                seg.afflictionType = type;
                seg.isPetrify = false; // must read our status, not the petrify meter
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
