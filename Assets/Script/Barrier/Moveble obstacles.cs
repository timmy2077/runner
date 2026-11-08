using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movebleobstacles : MonoBehaviour
{
    public float moveSpeed = 2f; // 移动速度
    private bool isMoving = false; // 是否开始移动
    private float moveTimer = 0f; // 移动计时器
    public float moveDuration = 2f; // 移动持续时间（秒）

    // 当玩家进入触发器范围
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isMoving = true;
            moveTimer = 0f; // 重置计时器
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            // 沿着 Local 坐标向 -x 轴移动
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.Self);
            
            // 累计移动时间
            moveTimer += Time.deltaTime;
            
            // 达到移动时间后停止
            if (moveTimer >= moveDuration)
            {
                isMoving = false;
            }
        }
    }
}
