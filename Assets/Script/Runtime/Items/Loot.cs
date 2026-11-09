using System;
using TMPro;
using UnityEngine;

public class Loot : MonoBehaviour
{
    public LootSO lootSO;
    public static event Action<LootSO> OnItemLooted;
    public TextMeshPro dialogueText;
    bool collected;
    void OnEnable()
    {
        collected = false;
    }

    void OnValidate()
    {
        if (lootSO != null)
            name = lootSO.lootName;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || lootSO == null || !other.CompareTag("Player"))
            return;
        collected = true;
        OnItemLooted?.Invoke(lootSO);
        if (dialogueText != null)
        {
            dialogueText.text = lootSO.dialogue;
            dialogueText.gameObject.SetActive(true);
        }

        Destroy(gameObject, 0.5f);
    }
}
