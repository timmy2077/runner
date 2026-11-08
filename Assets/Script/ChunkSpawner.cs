using UnityEngine; // 导入 Unity 核心库
using System.Collections.Generic; // 导入集合类库

// 关卡生成管理器：负责动态生成关卡、管理难度和回收 Chunk
public class ChunkSpawner : MonoBehaviour
{
    [System.Serializable] // 使该类可在 Inspector 中编辑
    public class ChunkDifficulty // 难度配置类
    {
        public GameObject[] chunks; // 该难度下的所有 Chunk 预制体数组
        public int appearanceCount = 1; // 每个 Chunk 出现的次数
    }

    [Header("关卡片段预设（按难度分组）")] // 编辑器分组标题
    public ChunkDifficulty[] difficultyLevels; // 难度层级数组

    [Header("生成设置")] // 编辑器分组标题
    public float spawnDistance = 18f; // 每个 Chunk 的长度（米）
    public float spawnOffset = 5f; // 提前生成距离（米）
    public int initialChunkCount = 3; // 初始生成的 Chunk 数量


    private Vector2 nextSpawnPosition; // 下一个 Chunk 的生成位置
    private List<GameObject> chunkPool = new List<GameObject>(); // Chunk 对象池（用于复用）
    private int currentDifficultyLevel = 0; // 当前难度等级

    private int currentChunkIndex = 0; // 当前 Chunk 索引
    private int currentChunkAppearance = 0; // 当前 Chunk 已出现次数

    [Header("初始设置")] // 编辑器分组标题
    public Vector2 initialSpawnPosition; // 明确设置初始生成位置


    
    // 初始化方法
    void Start()
    {
        nextSpawnPosition = initialSpawnPosition; // 设置初始生成位置
        for (int i = 0; i < initialChunkCount; i++) // 循环生成初始 Chunk
        {
            SpawnRandomChunk(); // 调用生成 Chunk 方法
        }
        // isInitialSpawning = false; // 初始生成完成

    }

    // 每帧更新
    void Update()
    {
        // 检测是否需要生成新 Chunk（玩家距离下一个生成点小于 spawnOffset）
        if (PlayerController.Instance != null && 
            PlayerController.Instance.transform.position.x > nextSpawnPosition.x - spawnOffset)
        {
           SpawnRandomChunk(); // 生成新 Chunk

                    // 检查是否需要提升难度（当前难度所有 Chunk 都生成完毕后）
        if (currentDifficultyLevel < difficultyLevels.Length - 1)
        {
            ChunkDifficulty currentLevel = difficultyLevels[currentDifficultyLevel];
            int totalAppearances = currentLevel.chunks.Length * currentLevel.appearanceCount;
            int currentAppearances = currentChunkIndex * currentLevel.appearanceCount + currentChunkAppearance;
            
            if (currentAppearances >= totalAppearances)
            {
                currentDifficultyLevel++; // 提升难度等级
                currentChunkIndex = 0; // 重置 Chunk 索引
                currentChunkAppearance = 0; // 重置 Chunk 出现次数
                Debug.Log("难度提升：" + currentDifficultyLevel); // 输出难度提升信息
            }
        }
        }




        RecycleOffscreenChunks(); // 回收超出视野的 Chunk
    }



    // 生成 Chunk（按顺序）
    void SpawnRandomChunk()
    {
        GameObject chunkPrefab = null; // 存储要生成的 Chunk 预制体

        ChunkDifficulty currentLevel = difficultyLevels[currentDifficultyLevel]; // 获取当前难度配置

        // 按顺序选择 Chunk
        if (currentLevel.chunks.Length > 0)
        {
            // 检查当前 Chunk 是否达到出现次数
            if (currentChunkAppearance >= currentLevel.appearanceCount)
            {
                // 切换到下一个 Chunk
                currentChunkIndex = (currentChunkIndex + 1) % currentLevel.chunks.Length;
                currentChunkAppearance = 0;
            }

            // 获取当前 Chunk
            chunkPrefab = currentLevel.chunks[currentChunkIndex];
            currentChunkAppearance++;
        }

        if (chunkPrefab == null) // 防止空引用
        {
            if (currentLevel.chunks.Length > 0)
            {
                chunkPrefab = currentLevel.chunks[0]; // 使用默认 Chunk
            }
        }

        GameObject newChunk = GetChunkFromPool(chunkPrefab); // 从对象池获取 Chunk
        newChunk.transform.position = nextSpawnPosition; // 设置位置
        newChunk.SetActive(true); // 激活 Chunk

        // 重新启用所有金币
        Coin[] coins = newChunk.GetComponentsInChildren<Coin>(true);
        foreach (Coin coin in coins)
        {
            coin.gameObject.SetActive(true);
        }



        nextSpawnPosition.x += spawnDistance; // 更新下一个生成位置
    }



    // 从对象池获取 Chunk（复用，避免频繁创建销毁）
    GameObject GetChunkFromPool(GameObject prefab)
    {
        foreach (GameObject chunk in chunkPool) // 遍历对象池
        {
            // 查找未激活且类型匹配的 Chunk
            if (!chunk.activeSelf && chunk.name == prefab.name + "(Clone)")
            {
                return chunk; // 返回找到的 Chunk
            }
        }

        // 没有找到则创建新的
        GameObject newChunk = Instantiate(prefab); // 实例化新 Chunk
        chunkPool.Add(newChunk); // 添加到对象池
        return newChunk; // 返回新 Chunk
    }

    // 回收超出视野的 Chunk
    void RecycleOffscreenChunks()
    {
        if (PlayerController.Instance == null) return; // 玩家控制器不存在则返回
        float recycleX = PlayerController.Instance.transform.position.x - 45f; // 回收线位置（玩家左侧 45 米）
        foreach (GameObject chunk in chunkPool) // 遍历对象池
        {
            // 检查 Chunk 是否激活且在回收线左侧
            if (chunk.activeSelf && chunk.transform.position.x < recycleX)
            {
                chunk.SetActive(false); // 禁用 Chunk（回收）
            }
        }
    }


}
