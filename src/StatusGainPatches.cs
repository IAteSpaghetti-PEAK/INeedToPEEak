using HarmonyLib;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Eating food adds Poo equal to half the hunger it cures; drinking adds Pee.
    ///
    /// This hooks CharacterAfflictions.SubtractStatus rather than the individual
    /// Action_* item scripts, because that is the single choke point every hunger
    /// restoration passes through — vanilla foods, cooked items, mushroom effects and
    /// foods added by other mods alike. Hooking the action classes only caught the
    /// specific ones PEAK happened to use.
    ///
    /// Runs on the consumer's own client (including when a friend feeds you, which
    /// executes on the receiver), so the resulting status syncs normally.
    /// </summary>
    internal static class StatusGainPatches
    {
        /// <summary>Set while a "cure everything" item is running so its hunger cure
        /// doesn't count as eating.</summary>
        internal static bool SuppressHungerGain;

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

        /// <summary>Skeletons (Book of Bones) and anything that can't get hungry don't process
        /// food/drink into Poo/Pee — mirrors how hunger itself is gated.</summary>
        internal static bool CanProcessDigestion(Character character)
        {
            if (character == null || !character.IsLocal) return false;
            if (BathroomConfig.SkeletonsDontGoToBathroom.Value && character.data.isSkeleton) return false;
            return character.refs.afflictions.canGetHungry;
        }

        /// <summary>Anything that reduces your Hunger feeds the machine.</summary>
        [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.SubtractStatus))]
        private static class Patch_HungerSubtracted
        {
            private static void Postfix(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType,
                float amount, bool fromRPC, bool decreasedNaturally)
            {
                if (statusType != CharacterAfflictions.STATUSTYPE.Hunger) return;
                if (fromRPC || decreasedNaturally || amount <= 0f) return;
                if (SuppressHungerGain) return;

                Character character = __instance.character;
                if (!CanProcessDigestion(character)) return;

                // Whatever is in hand is what's being consumed (null when fed by a friend,
                // in which case we treat it as food).
                Item item = character.data.currentItem;
                if (IsOurItem(item)) return;

                if (IsDrink(item))
                {
                    float gain = Mathf.Min(amount * BathroomConfig.PeeFromDrinkRatio.Value, BathroomConfig.PeeGainCap.Value);
                    character.refs.afflictions.AddStatus(BathroomStatuses.Pee,
                        BathroomStatuses.ChunkUp(gain * BathroomConfig.EffectScale));
                }
                else
                {
                    character.refs.afflictions.AddStatus(BathroomStatuses.Poo,
                        BathroomStatuses.ChunkUp(amount * BathroomConfig.PooFromFoodRatio.Value * BathroomConfig.EffectScale));
                }

                Plugin.Log.LogInfo($"Digested '{(item != null ? item.name : "?")}' (drink={IsDrink(item)}, cured={amount:F3}) " +
                                   $"-> Poo={character.refs.afflictions.GetCurrentStatus(BathroomStatuses.Poo):F3} " +
                                   $"Pee={character.refs.afflictions.GetCurrentStatus(BathroomStatuses.Pee):F3}");
            }
        }

        /// <summary>
        /// Drinks that cure no hunger at all (energy/sports drinks, milk...) still fill your
        /// bladder: half of whatever they do cure, capped, or a small fixed fallback.
        /// </summary>
        [HarmonyPatch(typeof(Action_Consume), nameof(Action_Consume.RunAction))]
        private static class Patch_Consume
        {
            private static void Postfix(Action_Consume __instance)
            {
                var item = __instance.GetComponent<Item>();
                if (item == null || !IsDrink(item) || IsOurItem(item)) return;
                var character = item.holderCharacter;
                if (!CanProcessDigestion(character)) return;

                float curedNonHunger = 0f;
                foreach (var action in item.GetComponents<Action_ModifyStatus>())
                {
                    if (action.changeAmount >= 0f) continue;
                    if (action.statusType == CharacterAfflictions.STATUSTYPE.Hunger) return; // handled by the hunger hook
                    curedNonHunger += Mathf.Abs(action.changeAmount);
                }
                foreach (var action in item.GetComponents<Action_RestoreHunger>())
                {
                    if (action.restorationAmount > 0f) return; // handled by the hunger hook
                }

                float gain = curedNonHunger > 0f
                    ? Mathf.Min(curedNonHunger * BathroomConfig.PeeFromDrinkRatio.Value, BathroomConfig.PeeGainCap.Value)
                    : BathroomConfig.PeeFallbackPerDrink.Value;
                character.refs.afflictions.AddStatus(BathroomStatuses.Pee,
                    BathroomStatuses.ChunkUp(gain * BathroomConfig.EffectScale));
                Plugin.Log.LogInfo($"Drank '{item.name}' (no hunger cure) -> Pee=" +
                                   $"{character.refs.afflictions.GetCurrentStatus(BathroomStatuses.Pee):F3}");
            }
        }
    }
}
