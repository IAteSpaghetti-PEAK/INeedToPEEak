using BepInEx.Configuration;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>How hard the bathroom needs hit you. Scales every effect the mod applies.</summary>
    public enum BathroomDifficulty
    {
        Gentle,
        Normal,
        Rough,
        Brutal,
    }

    /// <summary>
    /// All tunables, bound to BepInEx config.
    ///
    /// Only the two keybinds and the difficulty are shown in PEAKLib.ModConfig's in-game
    /// Mod Settings menu; everything else is tagged "Hidden" so the menu stays clean while
    /// the values remain editable in the .cfg file. The tag is a plain BepInEx description
    /// tag, so this needs no reference to (or dependency on) ModConfig — without it
    /// installed the mod behaves exactly the same.
    /// </summary>
    internal static class BathroomConfig
    {
        /// <summary>Description that ModConfig's menu skips over.</summary>
        private static ConfigDescription Hidden(string description) =>
            new ConfigDescription(description, null, "Hidden");

        // --- Shown in the in-game Mod Settings / Mod Controls menus ---
        public static ConfigEntry<KeyCode> PooKey;
        public static ConfigEntry<KeyCode> PeeKey;
        public static ConfigEntry<BathroomDifficulty> Difficulty;
        public static ConfigEntry<bool> ToiletPaperReplacesLuggageItem;

        // --- Config-file only ---
        public static ConfigEntry<bool> EnableDirty;
        public static ConfigEntry<bool> EnableStink;

        // --- Status gain ---
        public static ConfigEntry<float> PooFromFoodRatio;
        public static ConfigEntry<float> PeeFromDrinkRatio;
        public static ConfigEntry<float> PeeFallbackPerDrink;
        public static ConfigEntry<float> PeeGainCap;

        // --- Actions ---
        public static ConfigEntry<float> ActionThreshold;
        public static ConfigEntry<float> SecondsPerFullBar;
        public static ConfigEntry<float> MovementEpsilon;
        public static ConfigEntry<float> PeeDrainPerSecond;
        public static ConfigEntry<float> PooMoveSpeedMultiplier;

        // --- Poo item ---
        public static ConfigEntry<float> PooBaseDiameter;
        public static ConfigEntry<float> PooCarryStink;
        public static ConfigEntry<float> EatTimeRatio;
        public static ConfigEntry<float> EatPoisonRatio;

        // --- Dirty / toilet paper ---
        public static ConfigEntry<float> DirtyPerPoo;
        public static ConfigEntry<float> DirtyPerWipe;
        public static ConfigEntry<int> ToiletPaperUses;
        public static ConfigEntry<float> TPChanceBigLuggage;
        public static ConfigEntry<float> TPChanceExplorerLuggage;
        public static ConfigEntry<bool> GiveStartingToiletPaper;

        // --- Pee puddle ---
        public static ConfigEntry<float> PuddleMaxDiameter;
        public static ConfigEntry<float> PuddleFullAmount;
        public static ConfigEntry<float> PuddleLifetime;

        // --- Performance backstops ---
        public static ConfigEntry<int> MaxPoos;
        public static ConfigEntry<int> MaxPuddles;

        // --- Vanilla item interactions ---
        public static ConfigEntry<bool> SkeletonsDontGoToBathroom;
        public static ConfigEntry<bool> CureAllRemovesPooPee;
        public static ConfigEntry<bool> PandoraRollsPooPee;
        public static ConfigEntry<float> PandoraPooPeeChance;

        // --- Item IDs (must match between all players) ---
        public static ConfigEntry<int> ToiletPaperItemID;
        public static ConfigEntry<int> PooItemID;

        /// <summary>
        /// Multiplier applied to every status this mod gives you — poo and pee build-up,
        /// the dirtiness from going, the stink of carrying a poo, and the poison from
        /// eating one. Purely local: each player's own difficulty scales their own
        /// gains, and the resulting statuses sync normally.
        /// </summary>
        public static float EffectScale
        {
            get
            {
                if (Difficulty == null) return 1f;
                switch (Difficulty.Value)
                {
                    case BathroomDifficulty.Gentle: return 0.5f;
                    case BathroomDifficulty.Rough: return 1.5f;
                    case BathroomDifficulty.Brutal: return 2f;
                    default: return 1f;
                }
            }
        }

        public static void Bind(ConfigFile cfg)
        {
            // KeyCode entries get a proper click-then-press rebind widget in ModConfig
            // (both in Mod Settings and Mod Controls), rather than a raw text field.
            PooKey = cfg.Bind("Input", "PooKey", KeyCode.K,
                "Key held to poo.");
            PeeKey = cfg.Bind("Input", "PeeKey", KeyCode.L,
                "Key held to pee.");
            Difficulty = cfg.Bind("General", "Difficulty", BathroomDifficulty.Normal,
                "How strongly bathroom needs affect you. Scales poo/pee build-up, dirtiness, " +
                "stink and poo poisoning. Gentle = half, Rough = 1.5x, Brutal = double.");
            // Kept out of the in-game menu (config-file only) to keep it uncluttered.
            EnableDirty = cfg.Bind("General", "EnableDirty", true,
                Hidden("Get Dirty after pooping (cured with toilet paper). Turn off if you don't want " +
                       "to depend on finding toilet paper."));
            EnableStink = cfg.Bind("General", "EnableStink", true,
                Hidden("Carrying a poo makes you Stink. Turn off to carry poos with no penalty."));

            // Toilet paper spawning — shown in the menu so groups can tune scarcity.
            ToiletPaperReplacesLuggageItem = cfg.Bind("ToiletPaper", "ReplacesALuggageItem", false,
                "ON: toilet paper takes the place of one item the luggage rolled, keeping its item " +
                "count the same (that item is destroyed, which can eat items added by other mods). " +
                "OFF: toilet paper is added alongside the normal loot and nothing is removed.");
            TPChanceExplorerLuggage = cfg.Bind("ToiletPaper", "ChanceExplorerLuggage", 0.25f,
                "Chance (0-1) an Explorer's Luggage contains toilet paper.");
            TPChanceBigLuggage = cfg.Bind("ToiletPaper", "ChanceBigLuggage", 0.03f,
                "Chance (0-1) a Big Luggage contains toilet paper.");
            ToiletPaperUses = cfg.Bind("ToiletPaper", "UsesPerRoll", 5,
                "Wipes per toilet paper roll. Raise this for bigger groups.");
            GiveStartingToiletPaper = cfg.Bind("ToiletPaper", "GiveStartingRoll", true,
                "One random player starts the run with a toilet paper roll.");

            // Everything below is hidden from the in-game menu (still editable here).
            PooFromFoodRatio = cfg.Bind("Gain", "PooFromFoodRatio", 0.5f,
                Hidden("Fraction of the hunger a food cures that is added as Poo."));
            PeeFromDrinkRatio = cfg.Bind("Gain", "PeeFromDrinkRatio", 0.5f,
                Hidden("Fraction of the statuses a drink cures that is added as Pee."));
            PeeFallbackPerDrink = cfg.Bind("Gain", "PeeFallbackPerDrink", 0.15f,
                Hidden("Pee added by a drink that cures no statuses at all."));
            PeeGainCap = cfg.Bind("Gain", "PeeGainCap", 0.5f,
                Hidden("Maximum Pee a single drink can add (some drinks cure huge amounts)."));

            ActionThreshold = cfg.Bind("Actions", "ActionThreshold", 1f / 3f,
                Hidden("Minimum fill (fraction of the stamina bar) of Poo/Pee before you can relieve yourself."));
            SecondsPerFullBar = cfg.Bind("Actions", "SecondsPerFullBar", 10f,
                Hidden("Seconds it takes to poo a FULL bar. 33% poo => ~3.3s, 85% => 8.5s. Pee drains at the same rate."));
            MovementEpsilon = cfg.Bind("Actions", "MovementEpsilon", 0.35f,
                Hidden("Velocity above which you no longer count as standing still."));
            PeeDrainPerSecond = cfg.Bind("Actions", "PeeDrainPerSecond", 0.1f,
                Hidden("Pee removed per second while peeing (0.1 = full bar in 10s)."));
            PooMoveSpeedMultiplier = cfg.Bind("Actions", "PooMoveSpeedMultiplier", 0.25f,
                Hidden("Movement force multiplier while pooping (stacks with the 50% crouch penalty)."));

            PooBaseDiameter = cfg.Bind("PooItem", "PooBaseDiameter", 0.35f,
                Hidden("World diameter of a default (33%) poo. Roughly half a Bing Bong."));
            PooCarryStink = cfg.Bind("PooItem", "PooCarryStink", 0.10f,
                Hidden("Stink status per poo carried (hands or main three slots); removed when dropped. Stacks per poo."));
            EatTimeRatio = cfg.Bind("PooItem", "EatTimeRatio", 0.5f,
                Hidden("Eating a poo takes its poo-time multiplied by this (default: half)."));
            EatPoisonRatio = cfg.Bind("PooItem", "EatPoisonRatio", 0.5f,
                Hidden("Poison given when eating a poo = original poo amount times this."));

            DirtyPerPoo = cfg.Bind("Dirty", "DirtyPerPoo", 0.05f,
                Hidden("Dirty status applied after pooping."));
            DirtyPerWipe = cfg.Bind("Dirty", "DirtyPerWipe", 0.05f,
                Hidden("Dirty status removed per toilet paper wipe."));

            PuddleMaxDiameter = cfg.Bind("Puddle", "PuddleMaxDiameter", 1.2f,
                Hidden("Maximum pee puddle diameter (about a beached jellyfish)."));
            PuddleFullAmount = cfg.Bind("Puddle", "PuddleFullAmount", 1.0f,
                Hidden("Amount of pee (bar fraction) that grows a puddle to maximum size."));
            PuddleLifetime = cfg.Bind("Puddle", "PuddleLifetime", 0f,
                Hidden("Seconds before a puddle dries up. 0 = never."));

            MaxPoos = cfg.Bind("Performance", "MaxPoos", 40,
                Hidden("Most poos allowed to exist at once (oldest on the ground is removed past this). 0 = unlimited."));
            MaxPuddles = cfg.Bind("Performance", "MaxPuddles", 20,
                Hidden("Most of your own pee puddles allowed at once (oldest is removed past this). 0 = unlimited."));

            SkeletonsDontGoToBathroom = cfg.Bind("Interactions", "SkeletonsDontGoToBathroom", true,
                Hidden("Skeletons (revived via the Book of Bones) don't gain Poo/Pee, just like they don't get hungry."));
            CureAllRemovesPooPee = cfg.Bind("Interactions", "CureAllRemovesPooPee", true,
                Hidden("Cure-All (and other 'clear all status' items) also remove Poo and Pee."));
            PandoraRollsPooPee = cfg.Bind("Interactions", "PandoraRollsPooPee", true,
                Hidden("Pandora's Lunchbox clears Poo/Pee and can randomly re-roll them like other statuses."));
            PandoraPooPeeChance = cfg.Bind("Interactions", "PandoraPooPeeChance", 0.5f,
                Hidden("Chance (0-1) that Pandora's Lunchbox rolls each of Poo and Pee."));

            ToiletPaperItemID = cfg.Bind("ItemIDs", "ToiletPaperItemID", 61001,
                Hidden("Item database ID for toilet paper. Must match on all players."));
            PooItemID = cfg.Bind("ItemIDs", "PooItemID", 61002,
                Hidden("Item database ID for poo. Must match on all players."));
        }
    }
}
