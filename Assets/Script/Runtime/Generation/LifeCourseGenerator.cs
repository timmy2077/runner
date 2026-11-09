using System.Collections.Generic;
using UnityEngine;

public class LifeCourseGenerator : MonoBehaviour
{
    public float GroundTop { get; private set; }
    public float Progress => player == null ? 0 : Mathf.Clamp01((player.transform.position.x - origin) / (count * length));
    public int ActiveSegmentCount => segments.Count;
    public int SegmentCount => count;
    public float Length => length;
    public float FinishX => origin + count * length;

    public float XAtProgress(float progress) => origin + count * length * progress;
    public JourneyObstacleSpawner Obstacles { get; private set; }
    public bool UsesPrefabChunks => prefabSource != null;

    readonly Dictionary<int, GameObject> segments = new Dictionary<int, GameObject>();
    readonly List<int> retired = new List<int>();
    LifeJourneySession journey;
    PlayerController player;
    LifeStageProfile profile;
    Sprite ground, obstacle, memory;
    float origin, length, gravity;
    int count, nextIndex;
    LifeEventTrigger[] eventPoints;
    ChunkSpawner prefabSource;
    GameObject[][] prefabGroups;
    int[] groupSizes, groupRepeats;
    readonly Dictionary<GameObject, Stack<GameObject>> prefabPool = new Dictionary<GameObject, Stack<GameObject>>();
    readonly Dictionary<GameObject, GameObject> prefabOrigins = new Dictionary<GameObject, GameObject>();
    Transform prefabStorage;
    public void Initialize(ChunkSpawner source = null)
    {
        journey = LifeJourneySession.Instance;
        player = PlayerController.Instance;
        if (journey == null || !journey.IsStageScene || player == null)
        {
            enabled = false;
            return;
        }

        profile = journey.Profile;
        prefabSource = source;
        if (prefabSource != null && !PrepareCatalogue()) { enabled = false; return; }
        eventPoints = FindObjectsOfType<LifeEventTrigger>();
        length = UsesPrefabChunks ? prefabSource.spawnDistance : JourneyRoutePlan.SegmentLength(profile);
        count = UsesPrefabChunks ? Mathf.Max(3, Mathf.CeilToInt(profile.targetSeconds * journey.DurationScale * profile.moveSpeed / length))
            : JourneyRoutePlan.SegmentCount(profile, journey.DurationScale);
        origin = UsesPrefabChunks ? prefabSource.initialSpawnPosition.x : player.transform.position.x - 2;
        GroundTop = player.GetComponent<Collider2D>().bounds.min.y - 0.05f;
        gravity = player.Gravity;
        ground = profile.groundSprite != null ? profile.groundSprite : journey.Settings.groundSprite;
        obstacle = profile.obstacleSprite != null ? profile.obstacleSprite : journey.Settings.obstacleSprite;
        memory = journey.Settings.memorySprite;
        Obstacles = gameObject.AddComponent<JourneyObstacleSpawner>();
        int checkpoint = Mathf.Clamp(journey.Save.checkpointSegment, 0, count - 2);
        // Checkpoints always use a known flat segment, including saves created with older tuning.
        checkpoint -= checkpoint % Mathf.Max(2, journey.Settings.checkpointEverySegments);
        journey.Save.checkpointSegment = checkpoint;
        player.transform.position = new Vector3(origin + checkpoint * length + 2, player.transform.position.y, player.transform.position.z);
        var pendingEvent = journey.Save.lifeEvents?.pending;
        if (pendingEvent != null && pendingEvent.stageIndex == journey.StageIndex)
        {
            player.transform.position = new Vector3(pendingEvent.x, pendingEvent.y, pendingEvent.z);
            player.GetComponent<Rigidbody2D>().velocity = new Vector2(pendingEvent.velocityX, pendingEvent.velocityY);
        }
        nextIndex = Mathf.Max(-1, Mathf.FloorToInt((player.transform.position.x - origin) / length) - 2);
        GenerateAhead();
        if (UsesPrefabChunks && pendingEvent == null) PlacePlayerOnPrefabGround();
        journey.RegisterCourse(this);
        // Authored chunks already include their landscape. Adding another scrolling
        // landscape here creates a second horizon underneath the real ground.
        if (!UsesPrefabChunks && profile.backgroundPrefab != null)
        {
            var backgroundRoot = new GameObject("Existing stage background");
            var background = backgroundRoot.AddComponent<InfiniteBackground>();
            background.backgroundPrefab = profile.backgroundPrefab;
            background.moveSpeed = profile.moveSpeed * 0.15f;
        }

        if (Camera.main != null)
        {
            if (UsesPrefabChunks)
                JourneyBackgroundAlignment.FrameCamera(Camera.main, GetComponentsInChildren<JourneyBackgroundAlignment>());
            var follow = Camera.main.GetComponent<CameraFollow>();
            if (follow != null)
                follow.SnapToTarget(player.transform);
        }
    }

    void Update()
    {
        if (journey == null || !journey.IsPlaying || player == null)
            return;
        GenerateAhead();
        int current = Mathf.FloorToInt((player.transform.position.x - origin) / length);
        if (current >= 0 && current < count && current % Mathf.Max(2, journey.Settings.checkpointEverySegments) == 0 && player.IsGrounded && player.transform.position.x > origin + current * length + 1)
            journey.ReachCheckpoint(current);
        retired.Clear();
        foreach (var segment in segments)
            if (segment.Key < current - 2)
            {
                if (UsesPrefabChunks) ReleasePrefabSegment(segment.Value);
                else { Obstacles.ReleaseSegment(segment.Value.transform); Destroy(segment.Value); }
                retired.Add(segment.Key);
            }

        foreach (int index in retired)
            segments.Remove(index);
    }

    void GenerateAhead()
    {
        int current = Mathf.FloorToInt((player.transform.position.x - origin) / length);
        nextIndex = Mathf.Max(nextIndex, current - 2);
        float viewWidth = Camera.main != null ? Camera.main.orthographicSize * Camera.main.aspect : 12;
        float ahead = player.transform.position.x + (UsesPrefabChunks ? Mathf.Max(viewWidth + length, prefabSource.spawnOffset) : viewWidth + length * 2);
        if (UsesPrefabChunks) ahead = Mathf.Max(ahead, origin + Mathf.Max(0, prefabSource.initialChunkCount) * length);
        while (nextIndex <= count && origin + nextIndex * length < ahead)
        {
            Generate(nextIndex);
            nextIndex++;
        }

        Physics2D.SyncTransforms();
    }

    void Generate(int index)
    {
        if (UsesPrefabChunks) { GeneratePrefabSegment(index); return; }
        var root = new GameObject("Journey segment " + index);
        root.transform.SetParent(transform);
        segments.Add(index, root);
        float start = origin + index * length;
        var definition = journey.Encounter != null ? journey.Encounter.Definition : null;
        var plan = JourneyRoutePlan.Build(profile, index, count, journey.Settings.checkpointEverySegments, gravity, journey.Save.routeSeed, journey.Settings.generationSettings.tuning, definition != null ? definition.startProgress : 0.45f, definition != null ? definition.arenaEndProgress : 0.7f);
        foreach (var point in eventPoints)
        {
            if (point == null || !point.isActiveAndEnabled || !point.reserveFlatGround) continue;
            var bounds = point.Area.bounds;
            if (bounds.max.x + point.safePadding >= start && bounds.min.x - point.safePadding <= start + length)
            { plan.obstacle = JourneyObstacle.None; break; }
        }
        float center = start + length * plan.centerFraction;
        Sprite obstacleArt = journey.Settings.generationSettings.SpriteFor(plan.obstacle, obstacle);
        if (plan.obstacle == JourneyObstacle.Gap)
        {
            float gapStart = center - plan.gapWidth * 0.5f;
            Floor(root.transform, start, gapStart - start);
            Floor(root.transform, gapStart + plan.gapWidth, start + length - gapStart - plan.gapWidth);
        }
        else
            Floor(root.transform, start, length);
        switch (plan.obstacle)
        {
            case JourneyObstacle.Step:
                Obstacles.Rent(plan.obstacle, root.transform, new Vector2(center, GroundTop + plan.obstacleHeight * 0.5f), new Vector2(Mathf.Max(0.45f, profile.moveSpeed * 0.12f), plan.obstacleHeight), obstacleArt, GroundTop, index);
                break;
            case JourneyObstacle.Ceiling:
                Obstacles.Rent(plan.obstacle, root.transform, new Vector2(center, GroundTop + 1.2f), new Vector2(Mathf.Max(1, profile.moveSpeed * 0.4f), 1.1f), obstacleArt, GroundTop, index);
                break;
            case JourneyObstacle.Gate:
                Obstacles.Rent(plan.obstacle, root.transform, new Vector2(center, GroundTop + 1.3f), new Vector2(0.7f, 2.6f), obstacleArt, GroundTop, index);
                break;
        }

        if (plan.memorySlot >= 0 && !journey.Save.HasMemory(journey.StageIndex, plan.memorySlot))
        {
            var token = new GameObject("Memory " + plan.memorySlot);
            token.transform.SetParent(root.transform);
            token.transform.position = new Vector3(start + length * 0.5f, GroundTop + 0.55f, 0);
            var sprite = token.AddComponent<SpriteRenderer>();
            sprite.sprite = memory;
            sprite.sortingOrder = 8;
            if (memory != null)
                token.transform.localScale = Vector3.one * (0.5f / Mathf.Max(memory.bounds.size.x, 0.01f));
            var trigger = token.AddComponent<CircleCollider2D>();
            trigger.radius = memory != null ? memory.bounds.size.x * 0.6f : 0.35f;
            trigger.isTrigger = true;
            token.AddComponent<JourneyMemory>().slot = plan.memorySlot;
        }
    }

    bool PrepareCatalogue()
    {
        if (prefabSource.spawnDistance <= 0 || prefabSource.difficultyLevels == null)
        { Debug.LogError("Chunk Spawner 需要有效的 Spawn Distance 和预制体列表。", prefabSource); return false; }
        var levels = prefabSource.difficultyLevels;
        prefabGroups = new GameObject[levels.Length][];
        groupSizes = new int[levels.Length];
        groupRepeats = new int[levels.Length];
        int total = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            var valid = new List<GameObject>();
            if (levels[i]?.chunks != null)
                foreach (var prefab in levels[i].chunks) if (prefab != null) valid.Add(prefab);
            prefabGroups[i] = valid.ToArray();
            groupSizes[i] = valid.Count;
            groupRepeats[i] = levels[i] != null ? Mathf.Max(1, levels[i].appearanceCount) : 1;
            total += valid.Count;
        }
        if (total == 0) { Debug.LogError("Chunk Spawner 列表为空。请在 Difficulty Levels → Chunks 中配置原始关卡预制体。", prefabSource); return false; }
        prefabStorage = new GameObject("Inactive prefab chunks").transform;
        prefabStorage.SetParent(transform, false);
        prefabStorage.gameObject.SetActive(false);
        return true;
    }

    void GeneratePrefabSegment(int index)
    {
        // The behind-start guard must use the first authored chunk, never a random prefab.
        int group, item;
        if (!JourneyPrefabPlan.Resolve(Mathf.Max(0, index), groupSizes, groupRepeats, out group, out item)) return;
        var prefab = prefabGroups[group][item];
        if (!prefabPool.TryGetValue(prefab, out var pool))
        { pool = new Stack<GameObject>(); prefabPool.Add(prefab, pool); }
        GameObject chunk = null;
        while (pool.Count > 0 && chunk == null) chunk = pool.Pop();
        if (chunk == null)
        {
            // Instantiate under an inactive parent so legacy Start callbacks cannot run
            // before isolation. Source prefab assets and their authored geometry are untouched.
            chunk = Instantiate(prefab, prefabStorage);
            chunk.SetActive(false);
            JourneyLegacyIsolation.ApplyRoot(chunk);
            AlignExistingBackgrounds(chunk);
            prefabOrigins.Add(chunk, prefab);
        }
        var root = new GameObject("Prefab segment " + index + " · " + prefab.name);
        root.transform.SetParent(transform, false);
        segments.Add(index, root);
        chunk.transform.SetParent(root.transform, false);
        chunk.transform.position = new Vector3(origin + index * length, prefabSource.initialSpawnPosition.y, prefab.transform.position.z);
        chunk.SetActive(true);
        foreach (var coin in chunk.GetComponentsInChildren<Coin>(true)) coin.gameObject.SetActive(true);
        Physics2D.SyncTransforms();
        // Memories remain part of the new journey, placed on actual authored ground.
        for (int slot = 0; slot < 3; slot++)
            if (index == Mathf.FloorToInt(count * (0.2f + slot * 0.3f)) && !journey.Save.HasMemory(journey.StageIndex, slot))
            {
                float x = origin + index * length + length * 0.5f;
                var hit = Physics2D.Raycast(new Vector2(x, GroundTop + 5), Vector2.down, 30, 1 << 9);
                if (hit.collider != null)
                {
                    var token = new GameObject("Memory " + slot);
                    token.transform.SetParent(root.transform);
                    token.transform.position = new Vector3(x, hit.point.y + 0.55f, 0);
                    var art = token.AddComponent<SpriteRenderer>();
                    art.sprite = memory;
                    art.sortingOrder = 8;
                    if (memory != null) token.transform.localScale = Vector3.one * (0.5f / Mathf.Max(memory.bounds.size.x, 0.01f));
                    var trigger = token.AddComponent<CircleCollider2D>();
                    trigger.radius = memory != null ? memory.bounds.size.x * 0.6f : 0.35f;
                    trigger.isTrigger = true;
                    token.AddComponent<JourneyMemory>().slot = slot;
                }
            }
    }

    void AlignExistingBackgrounds(GameObject chunk)
    {
        // A restored authored chunk may use another age's landscape. Match the actual
        // existing art against all configured landscape roots, not just the stage override.
        var landscapeSprites = new HashSet<Sprite>();
        foreach (var stage in journey.Settings.stages)
        {
            if (stage.backgroundPrefab == null) continue;
            var reference = stage.backgroundPrefab.transform.root.GetComponent<SpriteRenderer>();
            if (reference != null && reference.sprite != null) landscapeSprites.Add(reference.sprite);
        }
        foreach (var renderer in chunk.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!landscapeSprites.Contains(renderer.sprite) || renderer.GetComponentInChildren<Collider2D>(true) != null) continue;
            var alignment = renderer.GetComponent<JourneyBackgroundAlignment>();
            if (alignment == null) alignment = renderer.gameObject.AddComponent<JourneyBackgroundAlignment>();
            alignment.Initialize(renderer);
        }
    }

    void ReleasePrefabSegment(GameObject segment)
    {
        foreach (Transform child in segment.transform)
            if (prefabOrigins.TryGetValue(child.gameObject, out var original))
            {
                var instance = child.gameObject;
                instance.SetActive(false);
                instance.transform.SetParent(prefabStorage, false);
                prefabPool[original].Push(instance);
                break;
            }
        Destroy(segment);
    }

    void PlacePlayerOnPrefabGround()
    {
        var capsule = player.GetComponent<Collider2D>();
        float feetOffset = player.transform.position.y - capsule.bounds.min.y;
        var start = player.transform.position;
        // Checkpoints may fall in an authored gap. Find a nearby ground surface before unpausing.
        for (int i = 0; i < 16; i++)
        {
            float x = start.x + i * 0.5f;
            var hit = Physics2D.Raycast(new Vector2(x, start.y + 3), Vector2.down, 40, 1 << 9);
            if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < 0.5f) continue;
            GroundTop = hit.point.y;
            player.transform.position = new Vector3(x, hit.point.y + feetOffset + 0.05f, start.z);
            player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Physics2D.SyncTransforms();
            return;
        }
        Debug.LogWarning("预制体出生位置附近没有找到 Ground 层地面，请检查预制体地面和 Initial Spawn Position。", prefabSource);
    }

    void Floor(Transform parent, float start, float width)
    {
        if (width < 0.01f)
            return;
        // Tile the existing floor sprite rather than stretching an entire level prefab.
        var floor = new GameObject("Ground");
        floor.layer = 9;
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3(start + width * 0.5f, GroundTop - 0.4f, 0);
        var collider = floor.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width + 0.005f, 0.8f);
        if (ground == null)
            return;
        float tileWidth = Mathf.Max(0.5f, ground.bounds.size.x);
        int tiles = Mathf.CeilToInt(width / tileWidth);
        for (int i = 0; i < tiles; i++)
        {
            float w = Mathf.Min(tileWidth, width - i * tileWidth);
            var tile = new GameObject("Floor tile");
            tile.transform.SetParent(floor.transform);
            tile.transform.localPosition = new Vector3(-width * 0.5f + i * tileWidth + w * 0.5f, 0, 0);
            tile.transform.localScale = new Vector3(w / ground.bounds.size.x, 0.8f / ground.bounds.size.y, 1);
            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = ground;
            renderer.sortingOrder = 1;
        }
    }
}
