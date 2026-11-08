using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForwardMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Vector2 moveDirection = new Vector2(-1, 0);
    public float destroyTime = 3f; // 销毁时间（秒）

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime);
    }

    
}
