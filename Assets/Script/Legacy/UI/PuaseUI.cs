using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class PuaseUI : MonoBehaviour
{
    public GameObject pauseMenu;
    private bool isPaused = false;
    private bool isCountingDown = false;
    public TextMeshProUGUI countdownText;
    void Update()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
            return;
        if (Input.GetKeyDown(KeyCode.Escape) && !isCountingDown)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.Play("Esc");
            OnClickResume();
        }
    }

    public void OnClickResume()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
        {
            LifeJourneySession.Instance.TogglePause();
            return;
        }

        isPaused = !isPaused;
        if (pauseMenu != null)
            pauseMenu.SetActive(isPaused);
        if (isPaused)
        {
            Time.timeScale = 0;
        }
        else
        {
            StartCoroutine(CountdownResume());
        }
    }

    private IEnumerator CountdownResume()
    {
        isCountingDown = true;
        float countdown = 3f;
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        while (countdown > 0)
        {
            int displayCountdown = Mathf.CeilToInt(countdown);
            Debug.Log("Resume in: " + displayCountdown);
            if (countdownText != null)
            {
                countdownText.text = displayCountdown.ToString();
            }

            yield return new WaitForSecondsRealtime(1f);
            countdown -= 1f;
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        isCountingDown = false;
        Time.timeScale = 1;
        Debug.Log("Game resumed");
    }
}
