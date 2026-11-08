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
        gameOverMenu.SetActive(true);
    }
    
    public void RestartGame()
    {
        gameOverMenu.SetActive(false);
        restartMenu.SetActive(true);
    }

    public void QuitGame()
    {
        SceneManager.LoadScene(0);
        Time.timeScale = 1;
    }
}
