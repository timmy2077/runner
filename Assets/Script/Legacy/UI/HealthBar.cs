using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public TextMeshProUGUI bosshealthText;
    public static int BossHealthCurrent;
    public static int BossHealthMax;
    public Image delayBar;
    private Image bosshealthBar;
    void Start()
    {
        if (LifeJourneySession.Instance != null && LifeJourneySession.Instance.IsStageScene)
        {
            gameObject.SetActive(false);
            return;
        }

        bosshealthBar = GetComponent<Image>();
    //HealthCurrent = HealthMax;
    }

    void Update()
    {
        if (bosshealthBar == null || delayBar == null || bosshealthText == null || BossHealthMax <= 0)
            return;
        bosshealthBar.fillAmount = (float)BossHealthCurrent / (float)BossHealthMax;
        delayBar.fillAmount = Mathf.Lerp(delayBar.fillAmount, bosshealthBar.fillAmount, Time.deltaTime * 2f);
        bosshealthText.text = BossHealthCurrent.ToString() + "/" + BossHealthMax.ToString();
    }
}
