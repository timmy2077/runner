using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetachChild : MonoBehaviour
{
    public GameObject objectToDetach; // 要脱离的子物体
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene) return;
        if (other.CompareTag("Player"))
        {
            if (objectToDetach != null)
            {
                objectToDetach.transform.SetParent(null);
            }

            Destroy(gameObject);
        }
    }
}
