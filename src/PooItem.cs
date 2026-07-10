using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// The poo on the ground. Slips players that run over it (banana peel behavior),
    /// gives Stink while carried (see CharacterBathroom), and can be eaten — eating
    /// takes half of its poo-time and poisons the eater by half of its poo amount.
    ///
    /// The poo amount is persisted in the item's ItemInstanceData (custom key), so it
    /// survives being picked up and dropped — the game re-instantiates a bare prefab on
    /// drop and only restores the instance data, which is exactly how vanilla item scale
    /// persists. It is an ItemComponent so OnInstanceDataSet() fires when that data lands.
    /// </summary>
    public class PooItem : ItemComponent, IPunInstantiateMagicCallback
    {
        private const float DefaultAmount = 1f / 3f;

        // Unused byte in the DataEntryKey enum (vanilla goes up to 14); the instance-data
        // serializer round-trips arbitrary key bytes paired with a known value type.
        private const DataEntryKey PooAmountKey = (DataEntryKey)200;

        public float pooAmount = DefaultAmount;

        private float slipCounter;

        public override void Awake()
        {
            base.Awake();
            item.forceScale = false; // we control the size
            ResolveFromData();
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

        /// <summary>First spawn: amount arrives as Photon instantiation data. The owner
        /// (master, for these room objects) persists it into instance data and syncs it.</summary>
        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            object[] data = info.photonView.InstantiationData;
            if (data != null && data.Length > 0 && data[0] is float amount)
            {
                pooAmount = Mathf.Clamp(amount, 0.05f, 1f);
                if (photonView != null && photonView.IsMine)
                {
                    var entry = item.GetData<FloatItemData>(PooAmountKey, () => new FloatItemData { Value = pooAmount });
                    entry.Value = pooAmount;
                    if (item.data != null)
                    {
                        photonView.RPC("SetItemInstanceDataRPC", RpcTarget.Others, item.data);
                    }
                }
            }
            ApplyAmount();
        }

        /// <summary>Fires when instance data lands — including after a drop re-instantiates us.</summary>
        public override void OnInstanceDataSet()
        {
            ResolveFromData();
            ApplyAmount();
        }

        private void ResolveFromData()
        {
            if (item != null && item.HasData(PooAmountKey)
                && item.data.TryGetDataEntry<FloatItemData>(PooAmountKey, out var entry))
            {
                pooAmount = Mathf.Clamp(entry.Value, 0.05f, 1f);
            }
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
