using UnityEngine;
using TMPro;

// Compatibility adapter for buttons serialized in the original restart panel.
// Restarting a life no longer depends on answering a quiz correctly.
public class Answer : MonoBehaviour
{
    public AnswerSO[] answerSO;
    public TextMeshProUGUI answerText;
    public TextMeshProUGUI[] keyTexts;
    public GameObject AnswerCorrect, AnswerError;
    void Start()
    {
        RandomQuestion();
    }

    public void RandomQuestion()
    {
        var journey = LifeJourneySession.Instance;
        if (answerText != null)
            answerText.text = journey != null && journey.Profile != null ? journey.Profile.deathText : "你可以停一下，再继续这一程。";
        string[] labels =
        {
            "继续这一程",
            "重活一世",
            "返回起点",
            "返回起点"
        };
        if (keyTexts != null)
            for (int i = 0; i < keyTexts.Length && i < labels.Length; i++)
                if (keyTexts[i] != null)
                    keyTexts[i].text = labels[i];
    }

    public void CheckAnswer(int index)
    {
        var journey = LifeJourneySession.Instance;
        if (journey == null || index < 0 || index > 3)
            return;
        if (index == 0)
            journey.RetryCheckpoint();
        else if (index == 1)
            journey.UI.ConfirmNewJourney(journey.Save != null && journey.Save.shortJourney);
        else
            journey.ReturnToMenu();
    }

    public void OnClickOption1()
    {
        CheckAnswer(0);
    }

    public void OnClickOption2()
    {
        CheckAnswer(1);
    }

    public void OnClickOption3()
    {
        CheckAnswer(2);
    }

    public void OnClickOption4()
    {
        CheckAnswer(3);
    }
}
