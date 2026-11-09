using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("开始游戏按钮")]
    public Button startButton;
    [Tooltip("退出游戏按钮")]
    public Button exitButton;
    [Header("加载设置")]
    [Tooltip("加载场景的索引")]
    public int loadingSceneIndex = 1;
    [Tooltip("目标游戏场景的索引")]
    public int targetSceneIndex = 2;
    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitGame);
        }
    }

    void OnStartGame()
    {
        if (LifeJourneySession.Instance != null)
        {
            LifeJourneySession.Instance.UI.ConfirmNewJourney(false);
            return;
        }

        if (loadingSceneIndex > 0 && targetSceneIndex > 0)
        {
            LoadingSceneData.targetSceneIndex = targetSceneIndex;
            SceneManager.LoadScene(loadingSceneIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitGame);
        }
    }
}
