using UnityEngine;
using System.Collections.Generic;

public class InfiniteBackground : MonoBehaviour
{
    public GameObject backgroundPrefab;
    public float moveSpeed = 2f; // 向左移动速度
    public float spawnInterval = 3f; // 生成间隔（秒）
    public int initialCount = 3; // 初始生成数量

    private float backgroundWidth;
    private List<GameObject> backgrounds = new List<GameObject>();
    private float spawnTimer;

    void Start()
    {
        if (backgroundPrefab == null) return;

        SpriteRenderer sr = backgroundPrefab.GetComponent<SpriteRenderer>();
        if (sr != null)
            backgroundWidth = sr.bounds.size.x;

        Vector3 pos = Vector3.zero;
        for (int i = 0; i < initialCount; i++)
        {
            GameObject bg = Instantiate(backgroundPrefab, pos, Quaternion.identity);
            backgrounds.Add(bg);
            pos.x += backgroundWidth;
        }
    }

    void Update()
    {
        // 所有背景向左移动
        for (int i = backgrounds.Count - 1; i >= 0; i--)
        {
            GameObject bg = backgrounds[i];
            if (bg == null)
            {
                backgrounds.RemoveAt(i);
                continue;
            }

            bg.transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);

            // 回收超出相机最左边的背景
            Camera cam = Camera.main;
            if (cam != null)
            {
                float camLeftEdge = cam.transform.position.x - cam.orthographicSize * cam.aspect;
                if (bg.transform.position.x + backgroundWidth < camLeftEdge)
                {
                    Destroy(bg);
                    backgrounds.RemoveAt(i);
                }
            }
        }

        // 定时在右边生成新背景
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && backgrounds.Count > 0)
        {
            SpawnRight();
            spawnTimer = 0f;
        }
    }

    void SpawnRight()
    {
        // 找到最右边的背景位置
        float rightX = 0f;
        foreach (var bg in backgrounds)
        {
            if (bg != null && bg.transform.position.x > rightX)
                rightX = bg.transform.position.x;
        }

        Vector3 pos = new Vector3(rightX + backgroundWidth, 0, 0);
        GameObject newBg = Instantiate(backgroundPrefab, pos, Quaternion.identity);
        backgrounds.Add(newBg);
    }
}
