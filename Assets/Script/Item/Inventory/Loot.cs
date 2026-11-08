using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class Loot : MonoBehaviour
{
    public LootSO lootSO;
    public static event Action<LootSO> OnItemLooted;
    public TMPro.TextMeshPro dialogueText;

    // public SpriteRenderer sr;

    private void OnValidate()
    {
        if (lootSO == null)
            return;
        UpdateAppearance();
    }

    
    private void UpdateAppearance()
    {
        // sr.sprite = lootSO.lootIcon;
        this.name = lootSO.lootName;

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Collect();
            
        }


    }

    private void Collect()
{
    OnItemLooted?.Invoke(lootSO);
    dialogueText.text = lootSO.dialogue;
    dialogueText.gameObject.SetActive(true);
    if(lootSO is ItemSO itemSo)
    {
      Destroy(gameObject, 0.5f);  
    }
    Debug.Log("Loot Destroyed");
}


}
