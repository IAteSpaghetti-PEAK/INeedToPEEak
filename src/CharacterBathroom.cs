using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Added to every Character. Runs the hold-to-poo / hold-to-pee state machine for
    /// the local player and receives the pee-stream VFX RPCs for remote players.
    /// Lives on the Character GameObject so it can use the character's PhotonView.
    /// </summary>
    public class CharacterBathroom : MonoBehaviourPun
    {
        private Character character;

        // Poo state (local player only)
        public bool IsPooping { get; private set; }
        private float pooProgress;
        private float pooDuration;

        // Pee state (local player only)
        public bool IsPeeing { get; private set; }
        private float peeReleased;
        private float peeStartLevel;
        private PeePuddle activePuddle;

        // VFX (all clients)
        private ParticleSystem peeStream;

        private int lastPooCarryCount = -1;

        // Cached reference to the local player's component, so the per-frame movement
        // patches never need GetComponent (see the patches at the bottom of the file).
        internal static CharacterBathroom LocalInstance { get; private set; }
        internal Character CharacterRef => character;

        private void Awake()
        {
            character = GetComponent<Character>();
        }

        private void OnDestroy()
        {
            if (LocalInstance == this) LocalInstance = null;
        }

        private CharacterAfflictions Afflictions => character.refs.afflictions;

        private void Update()
        {
            UpdateCarriedPooStink();

            if (character == null || !character.IsLocal || character.isBot) return;
            LocalInstance = this;

            float pooLevel = Afflictions.GetCurrentStatus(BathroomStatuses.Poo);
            float peeLevel = Afflictions.GetCurrentStatus(BathroomStatuses.Pee);
            bool pooHeld = Plugin.PooAction != null && Plugin.PooAction.IsPressed();
            bool peeHeld = Plugin.PeeAction != null && Plugin.PeeAction.IsPressed();

            if (IsPooping)
            {
                if (!pooHeld || !CanPoo())
                {
                    StopPoo();
                }
                else
                {
                    pooProgress += Time.deltaTime;
                    if (pooProgress >= pooDuration) FinishPoo();
                }
            }
            else if (IsPeeing)
            {
                if (!peeHeld || !CanPee() || Afflictions.GetCurrentStatus(BathroomStatuses.Pee) <= 0.001f)
                {
                    StopPee();
                }
                else
                {
                    ContinuePee();
                }
            }
            else if (pooHeld && pooLevel >= BathroomConfig.ActionThreshold.Value && CanPoo())
            {
                StartPoo(pooLevel);
            }
            else if (peeHeld && peeLevel >= BathroomConfig.ActionThreshold.Value && CanPee())
            {
                StartPee(peeLevel);
            }
        }

        /// <summary>Grounded and conscious. Walking is allowed — you'll just be very slow and crouched.</summary>
        private bool CanPoo()
        {
            var data = character.data;
            return data.isGrounded
                   && data.fullyConscious
                   && !data.isClimbingAnything
                   && !data.isCarried
                   && !data.isJumping
                   && !data.usingWheel;
        }

        /// <summary>Peeing additionally requires standing still.</summary>
        private bool CanPee()
        {
            return CanPoo()
                   && character.data.avarageVelocity.magnitude < BathroomConfig.MovementEpsilon.Value
                   && character.input.movementInput.magnitude < 0.01f;
        }

        // ------------------------------------------------------------------ POO

        private void StartPoo(float level)
        {
            IsPooping = true;
            pooProgress = 0f;
            pooDuration = Mathf.Max(0.5f, level * BathroomConfig.SecondsPerFullBar.Value);
        }

        private void StopPoo()
        {
            IsPooping = false;
            pooProgress = 0f;
        }

        private void FinishPoo()
        {
            IsPooping = false;
            float amount = Afflictions.GetCurrentStatus(BathroomStatuses.Poo);
            Afflictions.SetStatus(BathroomStatuses.Poo, 0f);
            Afflictions.AddStatus(BathroomStatuses.Dirty, BathroomConfig.DirtyPerPoo.Value);

            Vector3 pos = character.data.isGrounded ? character.data.groundPos : character.Center;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            // Master client spawns the poo as a room object so it survives the pooper leaving.
            photonView.RPC(nameof(RPC_MasterSpawnPoo), RpcTarget.MasterClient, pos + Vector3.up * 0.1f, rot, amount);
        }

        [PunRPC]
        public void RPC_MasterSpawnPoo(Vector3 pos, Quaternion rot, float amount)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            var go = PhotonNetwork.InstantiateRoomObject(BathroomItems.PooPrefabName, pos, rot, 0, new object[] { amount });
            if (go == null) Plugin.Log.LogError("Poo InstantiateRoomObject returned null!");
        }

        // ------------------------------------------------------------------ PEE

        // Stream emitter placement/physics — shared by the VFX (CreatePeeStream) and the
        // landing calc (ComputeStreamLanding) so the puddle spawns exactly where the
        // visible stream hits the ground.
        private const float StreamStartSpeed = 3.2f;
        private const float StreamGravityModifier = 1.35f;
        private static readonly Vector3 StreamLocalOffset = new Vector3(0f, 0f, 0.22f);
        private static readonly Quaternion StreamLocalTilt = Quaternion.Euler(12f, 0f, 0f);

        private void StartPee(float level)
        {
            IsPeeing = true;
            peeReleased = 0f;
            peeStartLevel = level;
            photonView.RPC(nameof(RPC_SetPeeVFX), RpcTarget.All, true);

            Vector3 puddlePos = ComputeStreamLanding();
            GameObject go = PhotonNetwork.Instantiate(BathroomItems.PuddlePrefabName, puddlePos, Quaternion.identity);
            activePuddle = go != null ? go.GetComponent<PeePuddle>() : null;
        }

        /// <summary>
        /// Traces the pee stream's parabola (same origin/direction/speed/gravity as the
        /// particle VFX) until it hits the ground, and returns that point — so the puddle
        /// lands under the visible stream. Falls back to a straight-down cast, then to a
        /// fixed forward offset.
        /// </summary>
        private Vector3 ComputeStreamLanding()
        {
            Rigidbody hip = character.GetBodypartRig(BodypartType.Hip);
            Transform hipT = hip != null ? hip.transform : character.transform;
            Vector3 origin = hipT.TransformPoint(StreamLocalOffset);
            Vector3 dir = ((hipT.rotation * StreamLocalTilt) * Vector3.forward).normalized;
            Vector3 vel = dir * StreamStartSpeed;
            Vector3 g = Physics.gravity * StreamGravityModifier;
            LayerMask groundMask = HelperFunctions.GetMask(HelperFunctions.LayerType.TerrainMap);

            Vector3 pos = origin;
            const float dt = 0.02f;
            for (int i = 0; i < 250; i++)
            {
                Vector3 next = pos + vel * dt + 0.5f * g * dt * dt;
                vel += g * dt;
                Vector3 seg = next - pos;
                float dist = seg.magnitude;
                if (dist > 1e-4f && Physics.Raycast(pos, seg / dist, out RaycastHit hit, dist, groundMask, QueryTriggerInteraction.Ignore))
                {
                    return hit.point + Vector3.up * 0.02f;
                }
                pos = next;
                if (pos.y < origin.y - 30f) break;
            }
            // Fallback 1: straight down from where the trace ended.
            if (Physics.Raycast(pos + Vector3.up, Vector3.down, out RaycastHit down, 40f, groundMask, QueryTriggerInteraction.Ignore))
            {
                return down.point + Vector3.up * 0.02f;
            }
            // Fallback 2: a fixed spot in front on the player's ground plane.
            Vector3 flat = character.data.lookDirection_Flat.normalized;
            Vector3 ground = character.data.isGrounded ? character.data.groundPos : character.Center + Vector3.down;
            return ground + flat * 0.9f + Vector3.up * 0.02f;
        }

        private void ContinuePee()
        {
            float drain = BathroomConfig.PeeDrainPerSecond.Value * Time.deltaTime;
            Afflictions.SubtractStatus(BathroomStatuses.Pee, drain);
            peeReleased += drain;
            if (activePuddle != null)
            {
                activePuddle.SetPeeAmount(peeReleased);
            }
            if (peeReleased >= peeStartLevel - 0.001f && Afflictions.GetCurrentStatus(BathroomStatuses.Pee) <= 0.026f)
            {
                // bar shrinks in 2.5% chunks; flush the remainder so it reaches zero
                Afflictions.SetStatus(BathroomStatuses.Pee, 0f);
                StopPee();
            }
        }

        private void StopPee()
        {
            IsPeeing = false;
            activePuddle = null; // the puddle stays behind
            photonView.RPC(nameof(RPC_SetPeeVFX), RpcTarget.All, false);
        }

        [PunRPC]
        public void RPC_SetPeeVFX(bool active)
        {
            if (active && peeStream == null) CreatePeeStream();
            if (peeStream == null) return;
            if (active) peeStream.Play();
            else peeStream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void CreatePeeStream()
        {
            Rigidbody hip = character.GetBodypartRig(BodypartType.Hip);
            if (hip == null) return;
            var go = new GameObject("INTP_PeeStream");
            go.transform.SetParent(hip.transform, false);
            go.transform.localPosition = StreamLocalOffset;
            go.transform.localRotation = StreamLocalTilt; // slightly downward arc

            peeStream = go.AddComponent<ParticleSystem>();
            var main = peeStream.main;
            main.startSpeed = StreamStartSpeed;
            main.startSize = 0.055f;
            main.startLifetime = 1.1f;
            main.gravityModifier = StreamGravityModifier;
            main.startColor = new Color(0.95f, 0.87f, 0.25f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = peeStream.emission;
            emission.rateOverTime = 65f;

            var shape = peeStream.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 3.5f;
            shape.radius = 0.015f;

            var renderer = peeStream.GetComponent<ParticleSystemRenderer>();
            renderer.material = BathroomAssets.PuddleMaterial;

            peeStream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ----------------------------------------------------- CARRIED POO STINK

        /// <summary>
        /// Carrying poo makes you Stink: its own status (works like thorns — occupies
        /// the bar while carried, gone when dropped). Counts poos in the hands and in
        /// the three main slots, stacking per poo.
        /// </summary>
        private void UpdateCarriedPooStink()
        {
            if (character == null || !photonView.IsMine || character.isBot || character.player == null) return;

            int count = 0;
            var player = character.player;
            for (byte slot = 0; slot < 3; slot++)
            {
                if (SlotHasPoo(player.GetItemSlot(slot))) count++;
            }
            // Item held after picking it up with full slots lives in the temp slot.
            if (SlotHasPoo(player.tempFullSlot)) count++;
            // Held item not backed by any slot (e.g. freshly grabbed off the ground).
            // itemID compare instead of GetComponent — this runs every frame.
            if (count == 0 && character.data.currentItem != null
                && character.data.currentItem.itemID == BathroomItems.PooItemID) count = 1;

            if (count != lastPooCarryCount)
            {
                lastPooCarryCount = count;
                Afflictions.SetStatus(BathroomStatuses.Stink, count * BathroomConfig.PooCarryStink.Value);
            }
        }

        private static bool SlotHasPoo(ItemSlot slot)
        {
            return slot != null && !slot.IsEmpty() && slot.prefab != null
                   && slot.prefab.itemID == BathroomItems.PooItemID;
        }

        // ------------------------------------------------------------------- UI

        /// <summary>
        /// Progress-bar data for the central drawer (Plugin.OnGUI). A per-character
        /// OnGUI would make Unity run the GUI event pipeline for EVERY character every
        /// frame — one central OnGUI is dramatically cheaper.
        /// </summary>
        internal bool TryGetProgress(out string label, out float fill, out Color color)
        {
            if (IsPooping)
            {
                label = "Pooping...";
                fill = Mathf.Clamp01(pooProgress / pooDuration);
                color = BathroomStatuses.PooColor;
                return true;
            }
            if (IsPeeing)
            {
                label = "Peeing...";
                fill = Mathf.Clamp01(Afflictions.GetCurrentStatus(BathroomStatuses.Pee) / Mathf.Max(peeStartLevel, 0.01f));
                color = BathroomStatuses.PeeColor;
                return true;
            }
            label = null;
            fill = 0f;
            color = default;
            return false;
        }
    }

    /// <summary>Attach a CharacterBathroom to every character as it spawns.</summary>
    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class Patch_CharacterAwake
    {
        private static void Postfix(Character __instance)
        {
            if (__instance.GetComponent<CharacterBathroom>() == null)
            {
                __instance.gameObject.AddComponent<CharacterBathroom>();
                var view = __instance.GetComponent<PhotonView>();
                if (view != null) view.RefreshRpcMonoBehaviourCache();
            }
        }
    }

    /// <summary>
    /// Force crouching while pooping by pretending the crouch key is held,
    /// and block sprinting (you can waddle, not run).
    /// </summary>
    [HarmonyPatch(typeof(CharacterMovement), "SetMovementState")]
    internal static class Patch_ForceCrouchWhilePooping
    {
        private static void Prefix(CharacterMovement __instance)
        {
            var b = CharacterBathroom.LocalInstance;
            if (b == null || !b.IsPooping) return;            // cheap common-case early-out
            if (__instance.character != b.CharacterRef) return;
            var input = b.CharacterRef.input;
            input.crouchIsPressed = true;
            input.sprintIsPressed = false;
            input.sprintToggleWasPressed = false;
        }
    }

    /// <summary>Mid-poo movement is incredibly slow (stacks with the vanilla crouch penalty).</summary>
    [HarmonyPatch(typeof(CharacterMovement), "GetMovementForce")]
    internal static class Patch_SlowWhilePooping
    {
        private static void Postfix(CharacterMovement __instance, ref float __result)
        {
            if (__result <= 0f) return;
            var b = CharacterBathroom.LocalInstance;
            if (b == null || !b.IsPooping) return;            // cheap common-case early-out
            if (__instance.character != b.CharacterRef) return;
            __result *= BathroomConfig.PooMoveSpeedMultiplier.Value;
        }
    }

}
