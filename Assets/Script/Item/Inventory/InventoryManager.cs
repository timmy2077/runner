using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public static event Action<LootSO> OnLootTextChanged;

    public Inventory[] itemSlots;


    private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
}

    private void Start()
    {
        foreach (var slot in itemSlots)
            {
                slot.UpdateUI();
            }
    }

    private void OnEnable()
    {
        Loot.OnItemLooted += AddItem;
    }

    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }

    public void AddItem(LootSO lootSO)
    {
        if (lootSO is BuffSo buffSo)
        {
            foreach (var slot in itemSlots)
            {
                if (slot.lootSO == buffSo.itemSO)
                {   
                    OnLootTextChanged?.Invoke(lootSO);
                    slot.lootSO = buffSo;
                    slot.UpdateUI();
                    UseBuff(buffSo);
                    return;
                }
                else return;
        
            }
        }

        foreach (var slot in itemSlots)
        {
            if (slot.lootSO == null)
            {
                OnLootTextChanged?.Invoke(lootSO);
                slot.lootSO = lootSO;
                slot.UpdateUI();
                return;
            }
        }
    }



    public void DropItem(Inventory slot)
    {
        if (slot.lootSO != null)
        {
            slot.lootSO = null;
            slot.UpdateUI();
        }
    }





    // public void UseItem(Inventory slot)
    // {
    //     if (slot.lootSO is BuffSo buffSo)
    //     {
    //         UseBuff(buffSo);
    //         slot.lootSO = null;
    //         slot.UpdateUI();
    //         Debug.Log("Use Item");
    //     }
        
    // }

    private void UseBuff(BuffSo buffSo)
    {
        if (buffSo.EnergyBuff != 0)
        {
            PlayerController.Instance.UpdateEnergy(buffSo.EnergyBuff);

        }
        if (buffSo.HealthBuff != 0)
        {
            PlayerController.Instance.SetProtected(true);
        }
        if (buffSo.DamageBuff != 0)
        {
            PlayerController.Instance.UpdateDamage(buffSo.DamageBuff);
        }
    }

}
