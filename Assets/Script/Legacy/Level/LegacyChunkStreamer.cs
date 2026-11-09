using System.Collections.Generic;
using UnityEngine;

// Original demo support; never used by any of the six campaign stages.
public class LegacyChunkStreamer : MonoBehaviour
{
    readonly Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();
    readonly List<GameObject> candidates = new List<GameObject>();
    ChunkSpawner source;
    Vector2 next;
    int difficulty, spawned;
    public void Initialize(ChunkSpawner settings)
    {
        source = settings;
        next = settings.initialSpawnPosition;
        if (source.difficultyLevels == null || source.difficultyLevels.Length == 0 || source.spawnDistance <= 0)
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < Mathf.Clamp(source.initialChunkCount, 1, 10); i++)
            Spawn();
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null || source == null)
            return;
        if (player.transform.position.x > next.x - source.spawnOffset)
            Spawn();
        foreach (var group in pool.Values)
            foreach (var chunk in group)
                if (chunk != null && chunk.activeSelf && chunk.transform.position.x + source.spawnDistance < player.transform.position.x - 45)
                    chunk.SetActive(false);
    }

    void Spawn()
    {
        candidates.Clear();
        var level = source.difficultyLevels[difficulty];
        if (level != null && level.chunks != null)
            foreach (var prefab in level.chunks)
                if (prefab != null && prefab.GetComponentInChildren<Boss>(true) == null && prefab.GetComponentInChildren<Bossapper>(true) == null && prefab.GetComponentInChildren<BossHealth>(true) == null)
                    candidates.Add(prefab);
        if (candidates.Count == 0)
        {
            enabled = false;
            Debug.LogWarning("Legacy chunk group has no ordinary prefabs; embedded boss chunks are excluded.");
            return;
        }

        var selected = candidates[Random.Range(0, candidates.Count)];
        if (!pool.TryGetValue(selected, out var instances))
        {
            instances = new List<GameObject>();
            pool.Add(selected, instances);
        }

        var chunk = instances.Find(item => item != null && !item.activeSelf);
        if (chunk == null)
        {
            chunk = Instantiate(selected, transform);
            instances.Add(chunk);
        }

        chunk.transform.position = next;
        chunk.SetActive(true);
        foreach (var coin in chunk.GetComponentsInChildren<Coin>(true))
            coin.gameObject.SetActive(true);
        next.x += source.spawnDistance;
        if (++spawned >= candidates.Count * Mathf.Max(1, level.appearanceCount) && difficulty < source.difficultyLevels.Length - 1)
        {
            difficulty++;
            spawned = 0;
        }
    }
}
