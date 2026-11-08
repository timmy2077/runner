using UnityEngine;

public class ResetObstaclePosition : MonoBehaviour
{
    [Header("初始位置和旋转")]
    public bool useInitialPosition = true; // 是否使用初始位置
    public Vector3 targetPosition = Vector3.zero; // 目标位置（本地坐标）
    public Vector3 targetRotation = Vector3.zero; // 目标旋转（本地坐标）

    private Vector3 initialPosition; // 记录初始位置
    private Quaternion initialRotation; // 记录初始旋转

    private void Start()
    {
        // 记录初始位置和旋转
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        ResetPosition();
    }

    private void OnDisable()
    {
        ResetPosition();
    }

    private void ResetPosition()
    {
        // 重置位置和旋转
        // if (useInitialPosition)
        // {
        //     // 使用初始位置
        //     transform.localPosition = initialPosition;
        //     transform.localRotation = initialRotation;
        // }
        // else
        // {
            // 使用手动设置的位置
            transform.localPosition = targetPosition;
            transform.localRotation = Quaternion.Euler(targetRotation);
        // }
    }
}