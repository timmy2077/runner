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

    // Start is called before the first frame update
    void Start()
    {
        bosshealthBar = GetComponent<Image>();
        
        //HealthCurrent = HealthMax;
    }

    // Update is called once per frame
    void Update()
    {
        bosshealthBar.fillAmount = (float)BossHealthCurrent / (float)BossHealthMax;
        delayBar.fillAmount = Mathf.Lerp(delayBar.fillAmount,bosshealthBar.fillAmount, Time.deltaTime * 2f);
        bosshealthText.text = BossHealthCurrent.ToString() + "/" + BossHealthMax.ToString();
    }
}
