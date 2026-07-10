using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace INeedToPEEak
{
    /// <summary>
    /// One random player starts the run with a toilet paper roll in their first slot.
    /// The master client (whose Player.AddItem is authoritative and syncs inventories)
    /// picks the lucky scout once everyone has spawned on the island.
    /// </summary>
    internal static class StartingItemGiver
    {
        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!BathroomConfig.GiveStartingToiletPaper.Value) return;
            if (mode != LoadSceneMode.Single) return;
            string name = scene.name ?? "";
            // Only gameplay scenes; skip the hub/menu. sceneLoaded fires once per Single
            // load, so this runs exactly once per run — do NOT dedupe on scene name, or
            // subsequent runs (same island scene name) would be skipped.
            if (name == "Airport" || name.Contains("Title") || name.Contains("Boot") || name.Contains("Menu")) return;
            Plugin.Instance.StartCoroutine(GiveWhenReady());
        }

        private static IEnumerator GiveWhenReady()
        {
            // Wait for the room and all characters to exist.
            float deadline = Time.realtimeSinceStartup + 60f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (PhotonNetwork.InRoom && Character.AllCharacters.Count >= PhotonNetwork.CurrentRoom.PlayerCount
                    && Character.localCharacter != null)
                {
                    break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(2f);

            if (!PhotonNetwork.IsMasterClient) yield break;
            if (Character.AllCharacters.Count == 0) yield break;

            var candidates = Character.AllCharacters.FindAll(c => c != null && !c.isBot && c.player != null);
            if (candidates.Count == 0) yield break;
            Character lucky = candidates[Random.Range(0, candidates.Count)];

            if (lucky.player.AddItem(BathroomItems.ToiletPaperItemID, null, out _))
            {
                Plugin.Log.LogInfo($"{lucky.characterName} starts with the toilet paper.");
            }
            else
            {
                Plugin.Log.LogWarning("Could not give the starting toilet paper roll.");
            }
        }
    }
}
