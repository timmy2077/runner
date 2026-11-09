using System.Collections.Generic;
using UnityEngine;

// Ordinary obstacles only. Mind demons and their attacks never enter this pool.
public class JourneyObstacleSpawner : MonoBehaviour
{
    public int CreatedCount { get; private set; }
    public int RentedCount { get; private set; }

    readonly Dictionary<JourneyObstacle, Stack<JourneyObstacleView>> pools = new Dictionary<JourneyObstacle, Stack<JourneyObstacleView>>();
    Transform storage;
    public JourneyObstacleView Rent(JourneyObstacle kind, Transform parent, Vector2 position, Vector2 size, Sprite sprite, float ground, int seed)
    {
        if (kind == JourneyObstacle.Gap || !JourneyRoutePlan.IsOrdinaryObstacle(kind))
            return null;
        if (storage == null)
        {
            storage = new GameObject("Inactive obstacle pool").transform;
            storage.SetParent(transform, false);
        }

        if (!pools.TryGetValue(kind, out var pool))
        {
            pool = new Stack<JourneyObstacleView>();
            pools.Add(kind, pool);
        }

        JourneyObstacleView view = null;
        while (pool.Count > 0 && view == null)
            view = pool.Pop();
        if (view == null)
        {
            var root = new GameObject("Pooled " + kind);
            root.SetActive(false);
            view = root.AddComponent<JourneyObstacleView>();
            view.Build(kind);
            CreatedCount++;
        }

        view.Configure(parent, position, size, sprite, ground, seed);
        RentedCount++;
        return view;
    }

    public void ReleaseSegment(Transform segment)
    {
        foreach (var view in segment.GetComponentsInChildren<JourneyObstacleView>(true))
        {
            if (!view.IsRented)
                continue;
            view.IsRented = false;
            view.gameObject.SetActive(false);
            view.transform.SetParent(storage, false);
            pools[view.Kind].Push(view);
            RentedCount--;
        }
    }
}
