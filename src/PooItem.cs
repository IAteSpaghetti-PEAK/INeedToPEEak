using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// The poo on the ground. Slips players that run over it (banana peel behavior),
    /// gives thorns while held (see Patch_HeldPooThorns), and can be eaten — eating
    /// takes half of its poo-time and poisons the eater by half of its poo amount.
    /// The amount travels with the Photon instantiation data so every client sizes
    /// it identically.
    /// </summary>
    public class PooItem : MonoBehaviourPun, IPunInstantiateMagicCallback
    {
        private const float DefaultAmount = 1f / 3f;

        public float pooAmount = DefaultAmount;

        private Item item;
        private float slipCounter;

        private void Awake()
        {
            item = GetComponent<Item>();
            item.forceScale = false; // we control the size
            ApplyAmount();
        }

        private void Start()
        {
            item.OnPrimaryFinishedCast += OnEatenLocally;
        }

        private void OnDestroy()
        {
            if (item != null) item.OnPrimaryFinishedCast -= OnEatenLocally;
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            object[] data = info.photonView.InstantiationData;
            if (data != null && data.Length > 0 && data[0] is float amount)
            {
                pooAmount = Mathf.Clamp(amount, 0.05f, 1f);
            }
            ApplyAmount();
        }

        private void ApplyAmount()
        {
            if (item == null) item = GetComponent<Item>();
            float scale = BathroomConfig.PooBaseDiameter.Value * (pooAmount / DefaultAmount);
            scale = Mathf.Clamp(scale, 0.12f, 1.6f); // never invisible, never building-sized
            transform.localScale = Vector3.one * scale;
            // Eating takes half as long as the pooing did (3s poo -> 1.5s eat).
            item.usingTimePrimary = Mathf.Max(0.4f,
                pooAmount * BathroomConfig.SecondsPerFullBar.Value * BathroomConfig.EatTimeRatio.Value);
            Plugin.Log.LogInfo($"Poo sized: amount={pooAmount:F3}, scale={scale:F3}, pos={transform.position}");
        }

        private void OnEatenLocally()
        {
            Character eater = item.holderCharacter;
            if (eater == null || !eater.IsLocal) return;
            eater.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison,
                pooAmount * BathroomConfig.EatPoisonRatio.Value);
            item.StartCoroutine(item.ConsumeDelayed());
        }

        private void Update()
        {
            // Banana-peel style slipping: every client watches its own character.
            if (item.itemState != ItemState.Ground) { slipCounter = 0f; return; }
            slipCounter += Time.deltaTime;
            if (slipCounter < 3f) return;

            Character local = Character.localCharacter;
            if (local == null || !local.data.isGrounded) return;
            float slipRadius = Mathf.Max(0.9f, transform.localScale.x * 2f);
            if (Vector3.Distance(local.Center, transform.position) > slipRadius) return;
            if (local.data.avarageVelocity.magnitude < 1.5f) return;

            slipCounter = 0f;
            photonView.RPC(nameof(RPCA_PooSlip), RpcTarget.All, local.refs.view.ViewID);
        }

        [PunRPC]
        public void RPCA_PooSlip(int viewID)
        {
            BathroomSlip.SlipCharacter(viewID);
            var rig = GetComponent<Rigidbody>();
            var slipped = PhotonView.Find(viewID);
            if (rig != null && slipped != null)
            {
                var character = slipped.GetComponent<Character>();
                if (character != null)
                {
                    rig.AddForce((character.data.lookDirection_Flat * 0.5f + Vector3.up) * 40f, ForceMode.Impulse);
                }
            }
        }
    }

    /// <summary>Shared ragdoll-slip, copied from the game's BananaPeel/SlipperyJellyfish.</summary>
    internal static class BathroomSlip
    {
        public static void SlipCharacter(int viewID)
        {
            PhotonView view = PhotonView.Find(viewID);
            if (view == null) return;
            Character character = view.GetComponent<Character>();
            if (character == null) return;

            Rigidbody footR = character.GetBodypartRig(BodypartType.Foot_R);
            Rigidbody footL = character.GetBodypartRig(BodypartType.Foot_L);
            Rigidbody hip = character.GetBodypartRig(BodypartType.Hip);
            Rigidbody head = character.GetBodypartRig(BodypartType.Head);
            character.RPCA_Fall(2f);
            footR.AddForce((character.data.lookDirection_Flat + Vector3.up) * 200f, ForceMode.Impulse);
            footL.AddForce((character.data.lookDirection_Flat + Vector3.up) * 200f, ForceMode.Impulse);
            hip.AddForce(Vector3.up * 1500f, ForceMode.Impulse);
            head.AddForce(character.data.lookDirection_Flat * -300f, ForceMode.Impulse);
        }
    }
}
