using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Caps how many poo items / pee puddles persist at once so a long session can't
    /// slowly accumulate hundreds of physics colliders and draw calls. When the cap is
    /// exceeded the oldest is networked-destroyed. Caps are generous (opt out with 0),
    /// so normal play never hits them — this is purely a lag backstop.
    ///
    /// Poos are registered on the master (which owns them as room objects); puddles are
    /// registered by whoever created them (they own their own), so each side only
    /// destroys objects it has authority over.
    /// </summary>
    internal static class BathroomCleanup
    {
        private static readonly List<GameObject> Poos = new List<GameObject>();
        private static readonly List<GameObject> Puddles = new List<GameObject>();

        public static void RegisterPoo(GameObject go) => Enforce(Poos, go, BathroomConfig.MaxPoos.Value);
        public static void RegisterPuddle(GameObject go) => Enforce(Puddles, go, BathroomConfig.MaxPuddles.Value);

        private static void Enforce(List<GameObject> list, GameObject go, int max)
        {
            if (go != null) list.Add(go);
            list.RemoveAll(g => g == null); // drop already-destroyed entries
            if (max <= 0) return;

            // Destroy oldest-first, but only objects we still own and that aren't in
            // someone's hands/inventory — destroying a held item wrenches the ragdoll.
            for (int i = 0; i < list.Count && list.Count > max;)
            {
                GameObject candidate = list[i];
                var view = candidate.GetComponent<PhotonView>();
                if (view == null || !view.IsMine)
                {
                    list.RemoveAt(i); // lost authority (e.g. master switch) — forget it
                    continue;
                }
                var item = candidate.GetComponent<Item>();
                if (item != null && item.itemState != ItemState.Ground)
                {
                    i++; // held or pocketed — skip, try the next oldest
                    continue;
                }
                list.RemoveAt(i);
                PhotonNetwork.Destroy(candidate);
            }
        }
    }
}
