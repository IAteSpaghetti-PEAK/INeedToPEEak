using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Toilet paper can only be found in Big Luggage (33%) and Explorer's Luggage (50%).
    /// Runs on the master client right after a luggage spawns its normal loot and
    /// rolls an extra toilet paper into it.
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

        private static void Postfix(Spawner __instance, List<Transform> spawnSpots)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!(__instance is Luggage luggage)) return;

            float chance = GetToiletPaperChance(luggage, spawnSpots);
            float roll = Random.Range(0f, 1f);
            Plugin.Log.LogInfo($"Luggage opened: name={luggage.gameObject.name}, displayName={luggage.displayName}, " +
                               $"pool={luggage.GetSpawnPool()}, spots={spawnSpots?.Count ?? 0}, " +
                               $"tpChance={chance:F2}, roll={roll:F2}");
            if (chance <= 0f || roll > chance) return;

            Vector3 pos = luggage.transform.position + Vector3.up * 0.5f;
            if (spawnSpots != null && spawnSpots.Count > 0)
            {
                Transform spot = spawnSpots[Random.Range(0, spawnSpots.Count)];
                if (spot != null) pos = spot.position + Vector3.up * 0.05f;
            }

            GameObject tp = PhotonNetwork.InstantiateRoomObject(BathroomItems.TPPrefabName, pos, Quaternion.identity);
            if (tp != null)
            {
                // Float in place like the rest of the luggage loot until grabbed.
                tp.GetComponent<PhotonView>().RPC("SetKinematicRPC", RpcTarget.AllBuffered, true, pos, Quaternion.identity);
                Plugin.Log.LogInfo($"Toilet paper spawned in {luggage.gameObject.name}");
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
