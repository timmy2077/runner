using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothing;
    public Vector3 offset = new Vector3(0, 0, 0); // 相机相对玩家的偏移

    public Vector2 minPosition;

    // Start is called before the first frame update
    void Start()
    {
    }

    void FixedUpdate()
    {
        if(target != null)
        {
            Vector3 targetPos = target.position + offset;
            targetPos.x = Mathf.Max(targetPos.x, minPosition.x);
            targetPos.y = transform.position.y;
            targetPos.z = transform.position.z; // 保持相机 Z 轴不变
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothing * Time.deltaTime);
        }
    }

    public void SetCamPosLimit(Vector2 minPos)
    {
        minPosition = minPos;
    }
}
