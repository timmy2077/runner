using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("跟随设置")]
    private Transform player; // 玩家引用
    public float fixedDistance = 10f; // 与玩家的固定距离
    public float smoothSpeed = 5f; // 平滑跟随速度
    [Header("生成设置")]
    public Transform target;
    public GameObject obstaclePrefab; // 要生成的预制体
    public float generateInterval = 2f; // 生成间隔（秒）
    private float generateTimer = 0f; // 生成计时器
    private bool isFollowing = false; // 是否在触发器范围内
    private void Start()
    {
        // 查找场景中带有 "Player" 标签的角色
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
            return;
        // 生成障碍物的计时器逻辑
        generateTimer += Time.deltaTime;
        if (generateTimer >= generateInterval)
        {
            if (target != null && obstaclePrefab != null)
            {
                Instantiate(obstaclePrefab, target.position, Quaternion.identity);
            }

            generateTimer = 0f;
        }
    }

    private void LateUpdate()
    {
        if (player != null && isFollowing)
        {
            Vector3 targetPos = new Vector3(player.position.x + fixedDistance, transform.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isFollowing = true;
        }
    }
}
