using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
                LifeJourneySession.Instance.CompleteStage();
            else if (GameManager.Instance != null)
                GameManager.Instance.GameClear();
        }
    }
}
