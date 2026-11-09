using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    public GameObject gameOverMenu;
    public GameObject restartMenu;
    void Awake()
    {
        Instance = this;
    }

    public void GameOver()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
        {
            LifeJourneySession.Instance.Die();
            return;
        }

        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);
        Time.timeScale = 0;
    }

    public void RestartGame()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
        {
            LifeJourneySession.Instance.RetryCheckpoint();
            return;
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        if (LifeJourneySession.Instance != null)
        {
            LifeJourneySession.Instance.ReturnToMenu();
            return;
        }

        SceneManager.LoadScene(0);
        Time.timeScale = 1;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
