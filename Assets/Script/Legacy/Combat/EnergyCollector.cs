using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyCollector : MonoBehaviour
{
    public int maxEnergy = 3; // 最大能量值
    public GameObject prefabToSpawn; // 要实例化的预制体
    public Transform spawnPosition; // 实例化位置
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("energy"))
        {
            if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
            {
                // Legacy energy is no longer ammunition in the life journey.
                other.gameObject.SetActive(false);
                return;
            }

            if (PlayerController.Instance == null)
                return;
            PlayerController.Instance.energy++;
            Debug.Log("能量: " + PlayerController.Instance.energy);
            if (PlayerController.Instance.energyText != null)
                PlayerController.Instance.energyText.text = "×" + PlayerController.Instance.energy.ToString();
            if (PlayerController.Instance.energy >= maxEnergy)
            {
                if (prefabToSpawn != null && spawnPosition != null)
                {
                    Instantiate(prefabToSpawn, spawnPosition.position, Quaternion.identity);
                    Debug.Log("能量已满，实例化物体");
                }

                PlayerController.Instance.energy = 0; // 重置能量
            }
        }
    }
}
