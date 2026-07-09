using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace INeedToPEEak
{
    /// <summary>
    /// Builds the custom item/puddle prefabs entirely in code (no asset bundles),
    /// registers the items in the game's ItemDatabase and installs a wrapping
    /// Photon prefab pool so PhotonNetwork.Instantiate can resolve them on every
    /// client. Prefabs are kept inactive so their components' Awake only runs on
    /// spawned clones, after Photon has wired up the view IDs (same technique as
    /// PEAKLib/REPOLib's CustomPrefabPool).
    /// </summary>
    internal static class BathroomItems
    {
        public const string PooPrefabName = "0_Items/INTP_Poo";
        public const string TPPrefabName = "0_Items/INTP_ToiletPaper";
        public const string PuddlePrefabName = "0_Items/INTP_PeePuddle";

        public static ushort PooItemID => (ushort)BathroomConfig.PooItemID.Value;
        public static ushort ToiletPaperItemID => (ushort)BathroomConfig.ToiletPaperItemID.Value;

        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static GameObject pooPrefab;
        private static GameObject tpPrefab;
        private static GameObject puddlePrefab;
        private static Mesh sphereMesh;
        private static Mesh cylinderMesh;

        public static void Initialize()
        {
            CacheMeshes();
            pooPrefab = BuildPooPrefab();
            tpPrefab = BuildToiletPaperPrefab();
            puddlePrefab = BuildPuddlePrefab();
            Register(PooPrefabName, pooPrefab);
            Register(TPPrefabName, tpPrefab);
            Register(PuddlePrefabName, puddlePrefab);
            InstallPrefabPool();
        }

        private static void Register(string key, GameObject prefab)
        {
            Prefabs[key] = prefab;
            // Some game paths spawn by bare prefab name without the folder prefix.
            string bare = key.Substring(key.LastIndexOf('/') + 1);
            Prefabs[bare] = prefab;
        }

        private static void CacheMeshes()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
        }

        // ------------------------------------------------------------- prefabs

        private static GameObject NewInactiveRoot(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            return go;
        }

        private static GameObject AddVisual(GameObject parent, Mesh mesh, Material material,
            Vector3 localPos, Vector3 localScale, Vector3 localEuler = default)
        {
            var child = new GameObject("Visual");
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = localPos;
            child.transform.localScale = localScale;
            child.transform.localEulerAngles = localEuler;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = material;
            return child;
        }

        /// <summary>
        /// Every holdable item MUST have direct children named Hand_R and Hand_L —
        /// CharacterItems grips items via transform.Find("Hand_R"/"Hand_L") and
        /// FixedJoints the hands to them. Missing grips wrench the whole ragdoll
        /// (the "teleports to spawn" bug).
        /// </summary>
        private static void AddHandGrips(GameObject parent, Vector3 rightLocalPos, Vector3 leftLocalPos)
        {
            var right = new GameObject("Hand_R");
            right.transform.SetParent(parent.transform, false);
            right.transform.localPosition = rightLocalPos;
            var left = new GameObject("Hand_L");
            left.transform.SetParent(parent.transform, false);
            left.transform.localPosition = leftLocalPos;
        }

        private static PhotonView AddPhotonView(GameObject go, params Component[] observed)
        {
            var view = go.AddComponent<PhotonView>();
            view.OwnershipTransfer = OwnershipOption.Takeover;
            view.Synchronization = ViewSynchronization.UnreliableOnChange;
            view.observableSearch = PhotonView.ObservableSearch.Manual;
            view.ObservedComponents = new List<Component>(observed);
            return view;
        }

        /// <summary>Three stacked brown spheres. Designed ~1m tall; PooItem scales it by amount.</summary>
        private static GameObject BuildPooPrefab()
        {
            var go = NewInactiveRoot("INTP_Poo");

            var mat = BathroomAssets.PooMaterial;
            AddVisual(go, sphereMesh, mat, new Vector3(0f, 0.30f, 0f), new Vector3(1.0f, 0.7f, 1.0f));
            AddVisual(go, sphereMesh, mat, new Vector3(0f, 0.62f, 0f), new Vector3(0.74f, 0.55f, 0.74f));
            AddVisual(go, sphereMesh, mat, new Vector3(0f, 0.88f, 0f), new Vector3(0.48f, 0.42f, 0.48f));

            AddHandGrips(go, new Vector3(0.45f, 0.35f, 0f), new Vector3(-0.45f, 0.35f, 0f));

            // Box, not sphere: a spherical poo rolls away downhill and "disappears".
            var collider = go.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(0.95f, 1.0f, 0.95f);

            var rig = go.AddComponent<Rigidbody>();
            rig.angularDamping = 3f;
            var syncer = go.AddComponent<ItemPhysicsSyncer>();
            AddPhotonView(go, syncer);

            var item = go.AddComponent<Item>();
            item.itemID = PooItemID;
            item.mass = 4f;
            item.usingTimePrimary = 1.65f; // overwritten per-instance by PooItem
            item.totalUses = -1;
            item.UIData = new Item.ItemUIData
            {
                itemName = "Poo",
                icon = BathroomAssets.PooItemIcon,
                mainInteractPrompt = "Eat",
                canDrop = true,
                canPocket = true,
                canBackpack = true,
                canThrow = true,
            };

            var eatAnim = go.AddComponent<Action_PlayAnimation>();
            eatAnim.OnPressed = true;
            eatAnim.animationName = "PlayerEat";

            go.AddComponent<PooItem>();
            return go;
        }

        /// <summary>A white roll lying on its side, ~0.28m across.</summary>
        private static GameObject BuildToiletPaperPrefab()
        {
            var go = NewInactiveRoot("INTP_ToiletPaper");

            const float size = 0.28f;
            AddVisual(go, cylinderMesh, BathroomAssets.PaperMaterial,
                Vector3.zero, new Vector3(size, size * 0.45f, size), new Vector3(0f, 0f, 90f));
            AddVisual(go, cylinderMesh, BathroomAssets.PooMaterial, // cardboard-ish tube ends
                Vector3.zero, new Vector3(size * 0.35f, size * 0.46f, size * 0.35f), new Vector3(0f, 0f, 90f));

            AddHandGrips(go, new Vector3(size * 0.5f, 0f, 0f), new Vector3(-size * 0.5f, 0f, 0f));

            var collider = go.AddComponent<SphereCollider>();
            collider.radius = size * 0.55f;

            go.AddComponent<Rigidbody>();
            var syncer = go.AddComponent<ItemPhysicsSyncer>();
            AddPhotonView(go, syncer);

            var item = go.AddComponent<ToiletPaperItem>();
            item.itemID = ToiletPaperItemID;
            item.mass = 4f;
            item.usingTimePrimary = 1.2f;
            item.totalUses = BathroomConfig.ToiletPaperUses.Value;
            item.UIData = new Item.ItemUIData
            {
                itemName = "Toilet Paper",
                icon = BathroomAssets.ToiletPaperItemIcon,
                mainInteractPrompt = "Wipe",
                canDrop = true,
                canPocket = true,
                canBackpack = true,
                canThrow = true,
            };

            var reduceUses = go.AddComponent<Action_ReduceUses>();
            reduceUses.consumeOnFullyUsed = true; // triggered via RPC from ToiletPaperWipe

            go.AddComponent<ToiletPaperWipe>();
            return go;
        }

        /// <summary>Flat yellow disc with a trigger volume; PeePuddle drives its size.</summary>
        private static GameObject BuildPuddlePrefab()
        {
            var go = NewInactiveRoot("INTP_PeePuddle");

            AddVisual(go, cylinderMesh, BathroomAssets.PuddleMaterial,
                new Vector3(0f, 0.012f, 0f), new Vector3(1f, 0.012f, 1f));

            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.1f, 0f);
            trigger.size = new Vector3(0.95f, 0.22f, 0.95f);

            var puddle = go.AddComponent<PeePuddle>();
            AddPhotonView(go, puddle);
            return go;
        }

        // -------------------------------------------------- database + network

        /// <summary>Waits for the game's ItemDatabase singleton, then registers our items.</summary>
        public static IEnumerator RegisterItemsWhenReady()
        {
            ItemDatabase database = null;
            while (database == null)
            {
                try
                {
                    database = SingletonAsset<ItemDatabase>.Instance;
                }
                catch
                {
                    // asset not loaded yet
                }
                if (database == null) yield return new WaitForSeconds(0.5f);
            }

            RegisterItem(database, pooPrefab.GetComponent<Item>());
            RegisterItem(database, tpPrefab.GetComponent<Item>());
            AddLocalization();
            CopyGripsFromVanilla(database);
            Plugin.Log.LogInfo($"Registered items: Poo (id {PooItemID}), Toilet Paper (id {ToiletPaperItemID})");
        }

        /// <summary>
        /// Item names and prompts run through LocalizedText.GetText; unknown keys render
        /// as "LOC: NAME". Inject our strings into the main table (same text for every
        /// language — it's poo, it's universal).
        /// </summary>
        private static void AddLocalization()
        {
            try
            {
                // Item names resolve via LocalizedText.GetNameIndex => "NAME_" + itemName.
                AddTerm("NAME_POO", "Poo");
                AddTerm("NAME_TOILET PAPER", "Toilet Paper");
                AddTerm("DESC_POO", "You made this.");
                AddTerm("DESC_TOILET PAPER", "Five wipes of pure luxury.");
                AddTerm("EAT", "Eat");
                AddTerm("WIPE", "Wipe");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to add localization terms: {e}");
            }

            void AddTerm(string key, string text)
            {
                var table = LocalizedText.mainTable;
                if (table.ContainsKey(key)) return;
                int languages = System.Enum.GetNames(typeof(LocalizedText.Language)).Length;
                var entries = new List<string>(languages);
                for (int i = 0; i < languages; i++) entries.Add(text);
                table.Add(key, entries);
            }
        }

        /// <summary>
        /// Steal the hold setup from a vanilla item so ours sit in the hands the way
        /// the animation rig expects:
        ///  - Hand_R / Hand_L grip ROTATIONS (identity-rotated grips twist the arms),
        ///  - Item.defaultPos / defaultForward (the held position relative to the
        ///    head — zero leaves the item ON the character's head/back, which is
        ///    exactly what remote players saw).
        /// </summary>
        private static void CopyGripsFromVanilla(ItemDatabase database)
        {
            Item donor = null;
            Transform donorR = null, donorL = null;
            foreach (var pair in database.itemLookup)
            {
                Item candidate = pair.Value;
                if (candidate == null || candidate.GetComponent<PooItem>() != null
                    || candidate.GetComponent<ToiletPaperWipe>() != null) continue;
                Transform r = candidate.transform.Find("Hand_R");
                Transform l = candidate.transform.Find("Hand_L");
                if (r == null || l == null) continue;
                donor = candidate;
                donorR = r;
                donorL = l;
                if (candidate.gameObject.name == "BingBong") break; // ideal donor: small two-hand plush
            }
            if (donor == null)
            {
                Plugin.Log.LogWarning("No vanilla item with Hand_R/Hand_L found; keeping default hold setup.");
                return;
            }

            Plugin.Log.LogInfo($"Copying hold setup from vanilla item '{donor.gameObject.name}' " +
                               $"(defaultPos {donor.defaultPos}, defaultForward {donor.defaultForward}, " +
                               $"R {donorR.localPosition}, L {donorL.localPosition})");
            ApplyHoldSetup(tpPrefab);
            ApplyHoldSetup(pooPrefab);

            void ApplyHoldSetup(GameObject prefab)
            {
                // Held position in front of the character, not inside the head.
                var item = prefab.GetComponent<Item>();
                item.defaultPos = donor.defaultPos;
                item.defaultForward = donor.defaultForward;

                // Copy only the grip ROTATIONS. Copying donor grip positions offsets
                // the item into the player's body (playtested); our own side-grip
                // positions keep the hands on the item's flanks.
                Transform r = prefab.transform.Find("Hand_R");
                Transform l = prefab.transform.Find("Hand_L");
                r.localRotation = donorR.localRotation;
                l.localRotation = donorL.localRotation;
            }
        }

        private static void RegisterItem(ItemDatabase database, Item item)
        {
            if (database.itemLookup.ContainsKey(item.itemID))
            {
                if (database.itemLookup[item.itemID] != item)
                {
                    Plugin.Log.LogError($"Item ID {item.itemID} already taken by {database.itemLookup[item.itemID].name}! " +
                                        "Change it in the config on ALL players.");
                }
                return;
            }
            database.itemLookup.Add(item.itemID, item);
            database.Objects.Add(item);
        }

        private static void InstallPrefabPool()
        {
            if (PhotonNetwork.PrefabPool is BathroomPrefabPool) return;
            PhotonNetwork.PrefabPool = new BathroomPrefabPool(PhotonNetwork.PrefabPool);
        }

        private class BathroomPrefabPool : IPunPrefabPool
        {
            private readonly IPunPrefabPool fallback;

            public BathroomPrefabPool(IPunPrefabPool fallback)
            {
                this.fallback = fallback;
            }

            public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
            {
                if (Prefabs.TryGetValue(prefabId, out GameObject prefab))
                {
                    // Prefab is inactive, so the clone starts inactive too;
                    // PUN activates it once the view IDs are assigned.
                    return Object.Instantiate(prefab, position, rotation);
                }
                return fallback.Instantiate(prefabId, position, rotation);
            }

            public void Destroy(GameObject gameObject)
            {
                if (gameObject != null && (gameObject.GetComponent<PooItem>() != null || gameObject.GetComponent<PeePuddle>() != null
                    || gameObject.GetComponent<ToiletPaperWipe>() != null))
                {
                    Object.Destroy(gameObject);
                    return;
                }
                fallback.Destroy(gameObject);
            }
        }
    }
}
