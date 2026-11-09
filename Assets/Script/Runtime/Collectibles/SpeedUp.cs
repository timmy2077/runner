using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedUp : MonoBehaviour
{
    public float speedBoost = 1.5f; // 速度提升因子
    public float Duration = 2f; // 速度提升持续时间（秒）
    public GameObject prop;
    private float originalSpeed; // 原始速度
    private void Start()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
        {
            enabled = false;
            return;
        }

        if (PlayerController.Instance != null)
            originalSpeed = PlayerController.Instance.moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(SpeedBoostCoroutine());
            Destroy(prop, 0.1f);
        }
    }

    private IEnumerator SpeedBoostCoroutine()
    {
        // 应用速度提升
        float boostedSpeed = originalSpeed + speedBoost;
        PlayerController.Instance.moveSpeed = boostedSpeed;
        Debug.Log("Speed boosted to: " + boostedSpeed);
        // 等待指定时间
        yield return new WaitForSeconds(Duration);
        // 恢复原始速度
        PlayerController.Instance.moveSpeed = originalSpeed;
        Debug.Log("Speed restored to: " + originalSpeed);
    }
}
