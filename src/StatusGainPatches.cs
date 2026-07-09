using HarmonyLib;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Eating food adds Poo equal to half the hunger the food cures.
    /// Drinking adds Pee: half the hunger a drink cures, or half of its other
    /// status cures if it cures no hunger (capped), or a small fixed fallback.
    /// Runs on the consumer's own client; the status then syncs via the vanilla
    /// status push, so it is fully multiplayer safe (including friend-feeding).
    /// </summary>
    internal static class StatusGainPatches
    {
        internal static bool IsDrink(Item item)
        {
            if (item == null) return false;
            var feedback = item.GetComponent<ItemUseFeedback>();
            return feedback != null && !string.IsNullOrEmpty(feedback.useAnimation)
                   && feedback.useAnimation.ToLowerInvariant().Contains("drink");
        }

        private static bool IsOurItem(Item item)
        {
            return item != null && (item.GetComponent<PooItem>() != null || item.GetComponent<ToiletPaperWipe>() != null);
        }

        private static void AddDigestion(Character character, Item item, float hungerCured)
        {
            if (character == null || !character.IsLocal || hungerCured <= 0f || IsOurItem(item)) return;
            if (IsDrink(item))
            {
                float gain = Mathf.Min(hungerCured * BathroomConfig.PeeFromDrinkRatio.Value, BathroomConfig.PeeGainCap.Value);
                character.refs.afflictions.AddStatus(BathroomStatuses.Pee, gain);
            }
            else
            {
                character.refs.afflictions.AddStatus(BathroomStatuses.Poo, hungerCured * BathroomConfig.PooFromFoodRatio.Value);
            }
        }

        [HarmonyPatch(typeof(Action_ModifyStatus), nameof(Action_ModifyStatus.RunAction))]
        private static class Patch_ModifyStatus
        {
            private static void Postfix(Action_ModifyStatus __instance)
            {
                if (__instance.statusType != CharacterAfflictions.STATUSTYPE.Hunger || __instance.changeAmount >= 0f) return;
                var item = __instance.GetComponent<Item>();
                AddDigestion(item != null ? item.holderCharacter : null, item, Mathf.Abs(__instance.changeAmount));
            }
        }

        [HarmonyPatch(typeof(Action_RestoreHunger), nameof(Action_RestoreHunger.RunAction))]
        private static class Patch_RestoreHunger
        {
            private static void Postfix(Action_RestoreHunger __instance)
            {
                if (__instance.restorationAmount <= 0f) return;
                var item = __instance.GetComponent<Item>();
                AddDigestion(item != null ? item.holderCharacter : null, item, __instance.restorationAmount);
            }
        }

        /// <summary>
        /// Drinks that cure no hunger (energy/sports drinks, milk...) still fill your bladder:
        /// half of whatever statuses they cure, capped, or a fixed fallback if they cure nothing.
        /// </summary>
        [HarmonyPatch(typeof(Action_Consume), nameof(Action_Consume.RunAction))]
        private static class Patch_Consume
        {
            private static void Postfix(Action_Consume __instance)
            {
                var item = __instance.GetComponent<Item>();
                if (item == null || !IsDrink(item) || IsOurItem(item)) return;
                var character = item.holderCharacter;
                if (character == null || !character.IsLocal) return;

                float curedNonHunger = 0f;
                foreach (var action in item.GetComponents<Action_ModifyStatus>())
                {
                    if (action.changeAmount >= 0f) continue;
                    if (action.statusType == CharacterAfflictions.STATUSTYPE.Hunger) return; // hunger patch already handled this drink
                    curedNonHunger += Mathf.Abs(action.changeAmount);
                }
                foreach (var action in item.GetComponents<Action_RestoreHunger>())
                {
                    if (action.restorationAmount > 0f) return; // hunger patch already handled this drink
                }

                float gain = curedNonHunger > 0f
                    ? Mathf.Min(curedNonHunger * BathroomConfig.PeeFromDrinkRatio.Value, BathroomConfig.PeeGainCap.Value)
                    : BathroomConfig.PeeFallbackPerDrink.Value;
                character.refs.afflictions.AddStatus(BathroomStatuses.Pee, gain);
            }
        }
    }
}
