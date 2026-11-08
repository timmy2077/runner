using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class Answer : MonoBehaviour
{
    public AnswerSO[] answerSO;
    public TextMeshProUGUI answerText;
    public TextMeshProUGUI[] keyTexts;
    public GameObject AnswerCorrect;
    public GameObject AnswerError;

    private AnswerSO currentQuestion;

    void Start() => RandomQuestion();

    public void RandomQuestion()
    {
        if (answerSO == null || answerSO.Length == 0) return;
        currentQuestion = answerSO[Random.Range(0, answerSO.Length)];
        DisplayQuestion();
    }

    void DisplayQuestion()
    {
        if (currentQuestion == null) return;
        answerText.text = currentQuestion.answerText;
        for (int i = 0; i < keyTexts.Length && i < currentQuestion.keySO.Length; i++)
            keyTexts[i].text = currentQuestion.keySO[i].key;
    }

    public void CheckAnswer(int index)
    {
        if (currentQuestion == null || index >= currentQuestion.keySO.Length) return;
        bool isCorrect = currentQuestion.keySO[index].isCorrect;
        if (isCorrect)
        {
            AnswerCorrect.SetActive(true);
            StartCoroutine(WaitAnswerCorrect());
            

        }
        else
        {
            AnswerError.SetActive(true);
            StartCoroutine(WaitAnswerError());
            

        }
    }

    IEnumerator WaitAnswerError()
    {
        
        yield return new WaitForSecondsRealtime(1);
        Debug.Log("回答错误！");
        Time.timeScale = 1;
        AnswerError.SetActive(false);
        SceneManager.LoadScene(0);
    }

    IEnumerator WaitAnswerCorrect()
    {
        Debug.Log("回答正确！");
        yield return new WaitForSecondsRealtime(1);
        Time.timeScale = 1;
        AnswerCorrect.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    

    public void OnClickOption1() => CheckAnswer(0);
    public void OnClickOption2() => CheckAnswer(1);
    public void OnClickOption3() => CheckAnswer(2);
    public void OnClickOption4() => CheckAnswer(3);


}
