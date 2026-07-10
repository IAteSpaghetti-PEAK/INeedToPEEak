using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Toilet paper can be found in Big Luggage (3%) and Explorer's Luggage (25%).
    /// Runs on the master client right after a luggage spawns its loot and REPLACES one
    /// of the rolled items with toilet paper (rather than adding an extra item), so the
    /// luggage keeps its normal item count. Also updates the returned list so the item
    /// tracker records the swap.
    ///
    /// Detection is data-driven, not name-based:
    ///  - Explorer's Luggage is the only luggage using the LuggageClimber spawn pool.
    ///  - Big Luggage uses a biome luggage pool with 3+ item spawn spots (regular has 2).
    /// </summary>
    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnItems))]
    internal static class LuggagePatch
    {
        private const SpawnPool BiomeLuggagePools =
            SpawnPool.LuggageBeach | SpawnPool.LuggageJungle | SpawnPool.LuggageTundra |
            SpawnPool.LuggageCaldera | SpawnPool.LuggageMesa | SpawnPool.LuggageRoots;

        private static void Postfix(Spawner __instance, List<Transform> spawnSpots, List<PhotonView> __result)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!(__instance is Luggage luggage)) return;

            float chance = GetToiletPaperChance(luggage, spawnSpots);
            float roll = Random.Range(0f, 1f);
            int itemCount = __result?.Count ?? 0;
            Plugin.Log.LogInfo($"Luggage opened: name={luggage.gameObject.name}, displayName={luggage.displayName}, " +
                               $"pool={luggage.GetSpawnPool()}, spots={spawnSpots?.Count ?? 0}, items={itemCount}, " +
                               $"tpChance={chance:F2}, roll={roll:F2}");
            if (chance <= 0f || roll > chance) return;
            if (__result == null || __result.Count == 0) return; // nothing to replace

            // Replace a random rolled item with toilet paper, at the same spot.
            int index = Random.Range(0, __result.Count);
            PhotonView victim = __result[index];
            if (victim == null) return;
            Vector3 pos = victim.transform.position;
            Quaternion rot = victim.transform.rotation;

            __result.RemoveAt(index);
            PhotonNetwork.Destroy(victim.gameObject);

            GameObject tp = PhotonNetwork.InstantiateRoomObject(BathroomItems.TPPrefabName, pos, rot);
            if (tp != null)
            {
                var tpView = tp.GetComponent<PhotonView>();
                // Float in place like the rest of the luggage loot until grabbed.
                tpView.RPC("SetKinematicRPC", RpcTarget.AllBuffered, true, pos, rot);
                __result.Add(tpView); // keep the tracker's list accurate
                Plugin.Log.LogInfo($"Toilet paper replaced an item in {luggage.gameObject.name}");
            }
            else
            {
                Plugin.Log.LogError("Toilet paper InstantiateRoomObject returned null!");
            }
        }

        private static float GetToiletPaperChance(Luggage luggage, List<Transform> spawnSpots)
        {
            SpawnPool pool = luggage.GetSpawnPool();

            // Explorer's Luggage: the climbing-gear pool.
            if (pool.HasFlag(SpawnPool.LuggageClimber))
            {
                return BathroomConfig.TPChanceExplorerLuggage.Value;
            }

            // Big Luggage: biome loot pool with 3+ spawn spots (regular luggage has 2).
            if ((pool & BiomeLuggagePools) != 0 && spawnSpots != null && spawnSpots.Count >= 3)
            {
                return BathroomConfig.TPChanceBigLuggage.Value;
            }

            // Name-based fallback in case a variant slips through the data heuristics.
            string id = ((luggage.displayName ?? "") + " " + luggage.gameObject.name).ToLowerInvariant();
            if (id.Contains("big")) return BathroomConfig.TPChanceBigLuggage.Value;
            if (id.Contains("explorer") || id.Contains("climber")) return BathroomConfig.TPChanceExplorerLuggage.Value;
            return 0f;
        }
    }
}
