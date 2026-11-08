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
            DontDestroyOnLoad(gameObject); // 切换场景不销毁
            if (timeText != null)
                DontDestroyOnLoad(timeText.transform.root.gameObject); // 对 Canvas 根物体使用
            if (gameVictoryPanel != null)
                DontDestroyOnLoad(gameVictoryPanel.transform.root.gameObject); // 对 Canvas 根物体使用
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
        gameVictoryPanel.SetActive(false);
    }

    void Update()
    {
        if (!isGameOver)
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
        timeText.text ="Time  " + string.Format("{0:00}:{1:00}", minutes, seconds);
    }


    // 游戏通关
    public void GameClear()
    {
        Debug.Log("游戏通关！");
        isGameOver = true;
        gameVictoryPanel.SetActive(true);
        Time.timeScale = 0; // 暂停游戏
    }


}