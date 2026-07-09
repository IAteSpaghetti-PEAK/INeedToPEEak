using Photon.Pun;

namespace INeedToPEEak
{
    /// <summary>
    /// Toilet paper item class: only usable while you are actually dirty.
    /// </summary>
    public class ToiletPaperItem : Item
    {
        public override bool CanUsePrimary()
        {
            if (!base.CanUsePrimary()) return false;
            Character holder = holderCharacter;
            return holder == null
                   || holder.refs.afflictions.GetCurrentStatus(BathroomStatuses.Dirty) > 0.001f;
        }
    }

    /// <summary>
    /// One wipe: removes a bit of Dirty and spends one of the roll's uses.
    /// Use counting/consuming is delegated to the vanilla Action_ReduceUses RPC.
    /// </summary>
    public class ToiletPaperWipe : MonoBehaviourPun
    {
        private Item item;

        private void Awake()
        {
            item = GetComponent<Item>();
        }

        private void Start()
        {
            item.OnPrimaryFinishedCast += OnWipe;
        }

        private void OnDestroy()
        {
            if (item != null) item.OnPrimaryFinishedCast -= OnWipe;
        }

        private void OnWipe()
        {
            Character holder = item.holderCharacter;
            if (holder == null || !holder.IsLocal) return;
            holder.refs.afflictions.SubtractStatus(BathroomStatuses.Dirty, BathroomConfig.DirtyPerWipe.Value);
            photonView.RPC("ReduceUsesRPC", RpcTarget.All);
        }
    }
}
