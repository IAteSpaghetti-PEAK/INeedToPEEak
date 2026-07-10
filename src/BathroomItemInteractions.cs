using HarmonyLib;
using Peak.Afflictions;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Makes Poo/Pee behave like vanilla afflictions with respect to three items:
    ///  - Book of Bones (skeletons): don't gain Poo/Pee, just like they can't get hungry.
    ///  - Cure-All / "clear all status": also removes Poo and Pee.
    ///  - Pandora's Lunchbox (Affliction_Chaos): clears Poo/Pee and can re-roll them.
    ///
    /// Vanilla "clear all status" loops only over the base STATUSTYPE enum length, so it
    /// never touched our custom slots (12-15) — these patches extend that to Poo/Pee.
    /// </summary>
    internal static class BathroomItemInteractions
    {
        private static void RemovePooPee(CharacterAfflictions afflictions)
        {
            if (afflictions == null) return;
            afflictions.SubtractStatus(BathroomStatuses.Poo, 5f);
            afflictions.SubtractStatus(BathroomStatuses.Pee, 5f);
        }

        /// <summary>Cure-All etc. that go through the CharacterAfflictions.ClearAllStatus method
        /// (also covers Pandora's internal clear, since Affliction_Chaos calls it).</summary>
        [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.ClearAllStatus))]
        private static class Patch_ClearAllStatusMethod
        {
            private static void Postfix(CharacterAfflictions __instance)
            {
                if (BathroomConfig.CureAllRemovesPooPee.Value) RemovePooPee(__instance);
            }
        }

        /// <summary>Cure-All etc. built from the Action_ClearAllStatus item action, which
        /// loops SubtractStatus manually (not via the method above).</summary>
        [HarmonyPatch(typeof(Action_ClearAllStatus), nameof(Action_ClearAllStatus.RunAction))]
        private static class Patch_ClearAllStatusAction
        {
            private static void Postfix(Action_ClearAllStatus __instance)
            {
                if (!BathroomConfig.CureAllRemovesPooPee.Value) return;
                var item = __instance.GetComponent<Item>();
                var character = item != null ? item.holderCharacter : null;
                if (character != null) RemovePooPee(character.refs.afflictions);
            }
        }

        /// <summary>Pandora's Lunchbox: the Chaos affliction has already cleared Poo/Pee via
        /// ClearAllStatus above; here we give it a chance to re-roll them back on.</summary>
        [HarmonyPatch(typeof(Affliction_Chaos), nameof(Affliction_Chaos.OnApplied))]
        private static class Patch_Chaos
        {
            private static void Postfix(Affliction_Chaos __instance)
            {
                if (!BathroomConfig.PandoraRollsPooPee.Value) return;
                Character character = __instance.character;
                if (character == null || !character.IsLocal) return;

                float chance = Mathf.Clamp01(BathroomConfig.PandoraPooPeeChance.Value);
                if (Random.value < chance)
                {
                    character.refs.afflictions.AddStatus(BathroomStatuses.Poo, Random.Range(0.1f, 0.6f));
                }
                if (Random.value < chance)
                {
                    character.refs.afflictions.AddStatus(BathroomStatuses.Pee, Random.Range(0.1f, 0.6f));
                }
            }
        }
    }
}
