using BepInEx.Configuration;

namespace INeedToPEEak
{
    /// <summary>All tunables for the mod, bound to BepInEx config so players can tweak them.</summary>
    internal static class BathroomConfig
    {
        // --- Input ---
        public static ConfigEntry<string> PooKey;
        public static ConfigEntry<string> PeeKey;

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

        // --- Vanilla item interactions ---
        public static ConfigEntry<bool> SkeletonsDontGoToBathroom;
        public static ConfigEntry<bool> CureAllRemovesPooPee;
        public static ConfigEntry<bool> PandoraRollsPooPee;
        public static ConfigEntry<float> PandoraPooPeeChance;

        // --- Item IDs (must match between all players) ---
        public static ConfigEntry<int> ToiletPaperItemID;
        public static ConfigEntry<int> PooItemID;

        public static void Bind(ConfigFile cfg)
        {
            PooKey = cfg.Bind("Input", "PooKey", "<Keyboard>/k",
                "Input System binding path held to poo.");
            PeeKey = cfg.Bind("Input", "PeeKey", "<Keyboard>/l",
                "Input System binding path held to pee.");

            PooFromFoodRatio = cfg.Bind("Gain", "PooFromFoodRatio", 0.5f,
                "Fraction of the hunger a food cures that is added as Poo.");
            PeeFromDrinkRatio = cfg.Bind("Gain", "PeeFromDrinkRatio", 0.5f,
                "Fraction of the statuses a drink cures that is added as Pee.");
            PeeFallbackPerDrink = cfg.Bind("Gain", "PeeFallbackPerDrink", 0.15f,
                "Pee added by a drink that cures no statuses at all.");
            PeeGainCap = cfg.Bind("Gain", "PeeGainCap", 0.5f,
                "Maximum Pee a single drink can add (some drinks cure huge amounts).");

            ActionThreshold = cfg.Bind("Actions", "ActionThreshold", 1f / 3f,
                "Minimum fill (fraction of the stamina bar) of Poo/Pee before you can relieve yourself.");
            SecondsPerFullBar = cfg.Bind("Actions", "SecondsPerFullBar", 10f,
                "Seconds it takes to poo a FULL bar. 33% poo => ~3.3s, 85% => 8.5s. Pee drains at the same rate.");
            MovementEpsilon = cfg.Bind("Actions", "MovementEpsilon", 0.35f,
                "Velocity above which you no longer count as standing still.");
            PeeDrainPerSecond = cfg.Bind("Actions", "PeeDrainPerSecond", 0.1f,
                "Pee removed per second while peeing (0.1 = full bar in 10s).");
            PooMoveSpeedMultiplier = cfg.Bind("Actions", "PooMoveSpeedMultiplier", 0.25f,
                "Movement force multiplier while pooping (stacks with the 50% crouch penalty).");

            PooBaseDiameter = cfg.Bind("PooItem", "PooBaseDiameter", 0.35f,
                "World diameter of a default (33%) poo. Roughly half a Bing Bong.");
            PooCarryStink = cfg.Bind("PooItem", "PooCarryStink", 0.10f,
                "Stink status per poo carried (hands or main three slots); removed when dropped. Stacks per poo.");
            EatTimeRatio = cfg.Bind("PooItem", "EatTimeRatio", 0.5f,
                "Eating a poo takes its poo-time multiplied by this (default: half).");
            EatPoisonRatio = cfg.Bind("PooItem", "EatPoisonRatio", 0.5f,
                "Poison given when eating a poo = original poo amount times this.");

            DirtyPerPoo = cfg.Bind("Dirty", "DirtyPerPoo", 0.05f,
                "Dirty status applied after pooping.");
            DirtyPerWipe = cfg.Bind("Dirty", "DirtyPerWipe", 0.05f,
                "Dirty status removed per toilet paper wipe.");
            ToiletPaperUses = cfg.Bind("Dirty", "ToiletPaperUses", 5,
                "Wipes per toilet paper roll.");
            TPChanceBigLuggage = cfg.Bind("Dirty", "TPChanceBigLuggage", 0.03f,
                "Chance a Big Luggage has one of its items replaced with toilet paper.");
            TPChanceExplorerLuggage = cfg.Bind("Dirty", "TPChanceExplorerLuggage", 0.25f,
                "Chance an Explorer's Luggage has one of its items replaced with toilet paper.");
            GiveStartingToiletPaper = cfg.Bind("Dirty", "GiveStartingToiletPaper", true,
                "One random player starts the run with a toilet paper roll.");

            PuddleMaxDiameter = cfg.Bind("Puddle", "PuddleMaxDiameter", 1.2f,
                "Maximum pee puddle diameter (about a beached jellyfish).");
            PuddleFullAmount = cfg.Bind("Puddle", "PuddleFullAmount", 1.0f,
                "Amount of pee (bar fraction) that grows a puddle to maximum size.");
            PuddleLifetime = cfg.Bind("Puddle", "PuddleLifetime", 0f,
                "Seconds before a puddle dries up. 0 = never.");

            SkeletonsDontGoToBathroom = cfg.Bind("Interactions", "SkeletonsDontGoToBathroom", true,
                "Skeletons (revived via the Book of Bones) don't gain Poo/Pee, just like they don't get hungry.");
            CureAllRemovesPooPee = cfg.Bind("Interactions", "CureAllRemovesPooPee", true,
                "Cure-All (and other 'clear all status' items) also remove Poo and Pee.");
            PandoraRollsPooPee = cfg.Bind("Interactions", "PandoraRollsPooPee", true,
                "Pandora's Lunchbox clears Poo/Pee and can randomly re-roll them like other statuses.");
            PandoraPooPeeChance = cfg.Bind("Interactions", "PandoraPooPeeChance", 0.5f,
                "Chance (0-1) that Pandora's Lunchbox rolls each of Poo and Pee.");

            ToiletPaperItemID = cfg.Bind("ItemIDs", "ToiletPaperItemID", 61001,
                "Item database ID for toilet paper. Must match on all players.");
            PooItemID = cfg.Bind("ItemIDs", "PooItemID", 61002,
                "Item database ID for poo. Must match on all players.");
        }
    }
}
