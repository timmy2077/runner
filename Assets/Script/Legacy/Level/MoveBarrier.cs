using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBarrier : MonoBehaviour
{
    public float moveSpeed = 2f; // 移动速度
    public float destroyTime = 2f; // 销毁时间（秒）
    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 沿着 Local 坐标向 -x 轴移动
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.Self);
    }
}
