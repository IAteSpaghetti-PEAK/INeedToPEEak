using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace INeedToPEEak
{
    /// <summary>
    /// I Need To PEEak — bathroom needs for PEAK.
    /// Eat -> Poo builds up on your stamina bar; hold K while standing still to poo
    /// (you crouch, it takes longer the more you need, and it leaves a real poo behind).
    /// Drink -> Pee builds up; hold L to relieve it into a slippery puddle.
    /// Pooping makes you Dirty; find toilet paper in big/explorer's luggage to wipe.
    /// Fully multiplayer-synced (everyone in the lobby needs the mod).
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.exoflex.ineedtopeeak";
        public const string PluginName = "INeedToPEEak";
        public const string PluginVersion = "0.1.5";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        internal static InputAction PooAction { get; private set; }
        internal static InputAction PeeAction { get; private set; }

        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            useGUILayout = false; // our OnGUI only blits rects; skip the layout pass

            BathroomConfig.Bind(Config);
            BathroomAssets.CreateAll();
            BathroomItems.Initialize();

            PooAction = new InputAction("INTP_Poo", InputActionType.Button, BathroomConfig.PooKey.Value);
            PeeAction = new InputAction("INTP_Pee", InputActionType.Button, BathroomConfig.PeeKey.Value);
            PooAction.Enable();
            PeeAction.Enable();

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);

            SceneManager.sceneLoaded += StartingItemGiver.OnSceneLoaded;
            StartCoroutine(BathroomItems.RegisterItemsWhenReady());

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Stay regular out there.");
        }

        /// <summary>Single central progress bar for the local player (one OnGUI for the
        /// whole mod instead of one per character).</summary>
        private void OnGUI()
        {
            var bathroom = CharacterBathroom.LocalInstance;
            if (bathroom == null || !bathroom.TryGetProgress(out string label, out float fill, out UnityEngine.Color color)) return;

            float w = 260f, h = 18f;
            float x = (UnityEngine.Screen.width - w) / 2f;
            float y = UnityEngine.Screen.height * 0.72f;

            UnityEngine.Color prev = UnityEngine.GUI.color;
            UnityEngine.GUI.color = new UnityEngine.Color(0f, 0f, 0f, 0.55f);
            UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x - 2, y - 2, w + 4, h + 4), UnityEngine.Texture2D.whiteTexture);
            UnityEngine.GUI.color = color;
            UnityEngine.GUI.DrawTexture(new UnityEngine.Rect(x, y, w * fill, h), UnityEngine.Texture2D.whiteTexture);
            UnityEngine.GUI.color = UnityEngine.Color.white;
            UnityEngine.GUI.Label(new UnityEngine.Rect(x, y - 22f, w, 20f), label);
            UnityEngine.GUI.color = prev;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= StartingItemGiver.OnSceneLoaded;
            harmony?.UnpatchSelf();
            PooAction?.Disable();
            PeeAction?.Disable();
        }
    }
}
