using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "AnswerSystem/AnswerSO")]
public class AnswerSO : ScriptableObject
{
    public string answerText;
    public KeySO[] keySO;
}

[System.Serializable]
public class KeySO
{
    public string key;
    public bool isCorrect;
}
