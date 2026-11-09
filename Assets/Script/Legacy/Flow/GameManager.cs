using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 全局游戏管理器（单例模式）
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 单例，方便其他脚本调用
    [Header("UI显示")]
    public TextMeshProUGUI timeText; // 计时文本
    public GameObject gameVictoryPanel; // 通关面板
    private float gameTime; // 游戏时长（目标6分钟=360秒）
    private bool isGameOver; // 是否游戏结束
    void Awake()
    {
        // 单例初始化（确保全局唯一）
        if (Instance == null)
        {
            Instance = this;
        // Campaign state belongs to LifeJourneySession. Scene UI must not survive a scene change.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        gameTime = 0;
        isGameOver = false;
        if (gameVictoryPanel != null)
            gameVictoryPanel.SetActive(false);
    }

    void Update()
    {
        if (!isGameOver && (LifeJourneySession.Instance == null || LifeJourneySession.Instance.IsPlaying))
        {
            // 计时更新（6分钟=360秒，到时间通关）
            gameTime += Time.deltaTime;
            UpdateTimeUI();
        }
    }

    // 更新计时UI（格式：05:59）
    void UpdateTimeUI()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        if (timeText != null)
            timeText.text = "Time  " + string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 游戏通关
    public void GameClear()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
        {
            LifeJourneySession.Instance.CompleteStage();
            return;
        }

        Debug.Log("游戏通关！");
        isGameOver = true;
        if (gameVictoryPanel != null)
            gameVictoryPanel.SetActive(true);
        Time.timeScale = 0; // 暂停游戏
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
