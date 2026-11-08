using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;



public class Inventory : MonoBehaviour
{
    public LootSO lootSO;
    public Image itemImage;
    private InventoryManager inventoryManager;


    private void Start()
    {
        inventoryManager = GetComponentInParent<InventoryManager>();
        UpdateUI();
    }



    // public void OnPointerClick(PointerEventData eventData)
    // {
    //     if (eventData.button == PointerEventData.InputButton.Left)
    //     {

    //         inventoryManager.UseItem(this);
    //     }
        
    // }


public void UpdateUI()
{

        if (lootSO != null)
        {
            itemImage.sprite = lootSO.lootIcon;
            itemImage.gameObject.SetActive(true);

        }
        else
        {
            itemImage.gameObject.SetActive(false);

        }
    
    

}
}
