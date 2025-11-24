using System.Collections.Generic;
using UnityEngine;

public class PickPlaceTaskManager : MonoBehaviour
{
    [Header("Prefabs & Scene Refs")]
    public GameObject blockPrefab;
    public Transform[] spawnPoints;   // 3 spawn points
    public PlacementZone[] bins;      // your 2 bins

    [Header("Runtime (read-only in Inspector)")]
    public List<GameObject> liveBlocks = new List<GameObject>();
    public int score;
    public float startTime;
    public int totalToSpawn = 3;      // 3 blocks

    void Update()
    {
        // Check bins for placed blocks
        if (bins == null) return;

        foreach (var bin in bins)
        {
            if (!bin) continue;

            foreach (var block in liveBlocks)
            {
                if (!block) continue;

                // already counted -> skip
                if (block.CompareTag("Untagged"))
                    continue;

                if (bin.IsInside(block.transform))
                {
                    var og = block.GetComponent<ObjectGrabbable>();
                    int value = (og != null) ? og.scoreValue : 1;
                    score += value;          // +1 by default
                    block.tag = "Untagged";  // don’t score it again
                }
            }
        }
    }

    // Called when starting a round
    public void ResetSession()
    {
        if (!blockPrefab)
        {
            Debug.LogError("PickPlaceTaskManager: blockPrefab not assigned!");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("PickPlaceTaskManager: no spawn points assigned!");
            return;
        }

        // Delete old blocks
        foreach (var b in liveBlocks)
            if (b) Destroy(b);
        liveBlocks.Clear();

        score = 0;
        startTime = Time.time;

        // Spawn new blocks
        for (int i = 0; i < totalToSpawn; i++)
        {
            Transform sp = spawnPoints[i % spawnPoints.Length];
            GameObject go = Instantiate(blockPrefab, sp.position, sp.rotation);
            go.name = $"Block_{i:00}";
            go.tag = "Block";   // important for scoring
            liveBlocks.Add(go);
        }

        Debug.Log($"[TaskManager] Spawned {totalToSpawn} blocks.");
    }
}
