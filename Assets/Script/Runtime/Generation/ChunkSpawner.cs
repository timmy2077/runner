using UnityEngine;

// Scene-owned prefab catalogue. The campaign uses the same authored chunks as the original project.
public class ChunkSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ChunkDifficulty
    {
        public GameObject[] chunks;
        public int appearanceCount = 1;
    }

    [Header("关卡片段预设（按数组顺序生成）")]
    public ChunkDifficulty[] difficultyLevels;
    [Header("生成设置（原始项目参数）")]
    public float spawnDistance = 18;
    public float spawnOffset = 5;
    public int initialChunkCount = 3;
    public Vector2 initialSpawnPosition;
    [Header("关卡来源")]
    [Tooltip("启用：使用上方原始 Chunk 预制体；关闭：使用程序生成地形。")]
    public bool usePrefabChunks = true;
    void Start()
    {
        var session = LifeJourneySession.Instance;
        if (session != null && session.IsStageScene)
        {
            var course = GetComponent<LifeCourseGenerator>();
            if (course == null)
                course = gameObject.AddComponent<LifeCourseGenerator>();
            course.Initialize(usePrefabChunks ? this : null);
        }
        else
        {
            var legacy = gameObject.AddComponent<LegacyChunkStreamer>();
            legacy.Initialize(this);
        }

        enabled = false;
    }
}
