using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScene : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("加载进度条")]
    public Slider loadingSlider;
    [Tooltip("加载进度文本")]
    public TextMeshProUGUI loadingText;

    
    [Header("加载设置")]
    [Tooltip("最小显示时间（秒）")]
    public float minDisplayTime = 2f;
    
    private AsyncOperation asyncLoad;
    private float startTime;
    
    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }
    
    IEnumerator LoadSceneAsync()
    {
        startTime = Time.time;
        
        int targetIndex = LoadingSceneData.targetSceneIndex > 0 ? LoadingSceneData.targetSceneIndex : 1;
        asyncLoad = SceneManager.LoadSceneAsync(targetIndex);
        asyncLoad.allowSceneActivation = false;
        
        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingSlider.value = progress;
            loadingText.text = (progress * 100).ToString("F0") + "%";
            
            yield return null;
        }
        
        // 确保至少显示指定时间
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minDisplayTime)
        {
            yield return new WaitForSeconds(minDisplayTime - elapsedTime);
        }
        
        // 完成加载
        loadingSlider.value = 1f;
        loadingText.text = "100%";
        yield return new WaitForSeconds(0.5f);
        
        asyncLoad.allowSceneActivation = true;
    }
}

public static class LoadingSceneData
{
    public static int targetSceneIndex = 1;
}