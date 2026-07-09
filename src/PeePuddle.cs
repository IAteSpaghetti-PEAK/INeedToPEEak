using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// A slippery pee puddle. Immovable zone that ragdolls players who run through it
    /// (like a beached jellyfish, but without the poison). Starts poo-sized and grows
    /// while its owner keeps peeing, up to jellyfish size. Size is streamed via the
    /// PhotonView so all clients stay in sync.
    /// </summary>
    public class PeePuddle : MonoBehaviourPun, IPunObservable
    {
        private float peeAmount;      // how much pee has been poured into this puddle
        private float lastSlipTime;
        private float bornTime;

        private void Awake()
        {
            bornTime = Time.time;
            lastSlipTime = Time.time;
            ApplyScale(true);
        }

        private void Update()
        {
            ApplyScale(false);
            if (photonView.IsMine && BathroomConfig.PuddleLifetime.Value > 0f
                && Time.time - bornTime > BathroomConfig.PuddleLifetime.Value)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        /// <summary>Called by the peeing player's client while the stream is going.</summary>
        public void SetPeeAmount(float amount)
        {
            if (photonView.IsMine)
            {
                peeAmount = Mathf.Max(peeAmount, amount);
            }
        }

        private float TargetDiameter()
        {
            float t = Mathf.Clamp01(peeAmount / Mathf.Max(0.05f, BathroomConfig.PuddleFullAmount.Value));
            return Mathf.Lerp(BathroomConfig.PooBaseDiameter.Value, BathroomConfig.PuddleMaxDiameter.Value, t);
        }

        private void ApplyScale(bool instant)
        {
            float target = TargetDiameter();
            float current = transform.localScale.x;
            float next = instant ? target : Mathf.Lerp(current, target, Time.deltaTime * 4f);
            transform.localScale = new Vector3(next, 1f, next);
        }

        private void OnTriggerStay(Collider other)
        {
            if (Time.time - lastSlipTime < 3f) return;
            if (Time.time - bornTime < 1f) return;
            if (!CharacterRagdoll.TryGetCharacterFromCollider(other, out Character character)) return;
            if (character == null || !character.IsLocal) return;
            if (!character.data.isGrounded) return;
            if (character.data.avarageVelocity.magnitude < 1.5f) return;
            var bathroom = character.GetComponent<CharacterBathroom>();
            if (bathroom != null && bathroom.IsPeeing) return; // don't slip mid-stream

            lastSlipTime = Time.time;
            photonView.RPC(nameof(RPCA_PuddleSlip), RpcTarget.All, character.refs.view.ViewID);
        }

        [PunRPC]
        public void RPCA_PuddleSlip(int viewID)
        {
            lastSlipTime = Time.time;
            BathroomSlip.SlipCharacter(viewID);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(peeAmount);
            }
            else
            {
                peeAmount = (float)stream.ReceiveNext();
            }
        }
    }
}
