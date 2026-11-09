using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public static event Action<LootSO> OnLootTextChanged;
    public Inventory[] itemSlots;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (itemSlots == null)
            return;
        foreach (var slot in itemSlots)
            if (slot != null)
                slot.UpdateUI();
    }

    void OnEnable()
    {
        Loot.OnItemLooted += AddItem;
    }

    void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddItem(LootSO loot)
    {
        if (loot == null || itemSlots == null)
            return;
        if (loot is BuffSo buff)
        {
            // Scan every slot; an unrelated first slot must not abort the search.
            foreach (var slot in itemSlots)
            {
                if (slot == null || buff.itemSO == null || slot.lootSO != buff.itemSO)
                    continue;
                slot.lootSO = buff;
                slot.UpdateUI();
                UseBuff(buff);
                OnLootTextChanged?.Invoke(loot);
                return;
            }

            return;
        }

        foreach (var slot in itemSlots)
        {
            if (slot == null || slot.lootSO != null)
                continue;
            slot.lootSO = loot;
            slot.UpdateUI();
            OnLootTextChanged?.Invoke(loot);
            return;
        }
    }

    public void DropItem(Inventory slot)
    {
        if (slot == null)
            return;
        slot.lootSO = null;
        slot.UpdateUI();
    }

    static void UseBuff(BuffSo buff)
    {
        var player = PlayerController.Instance;
        if (player == null)
            return;
        if (buff.EnergyBuff != 0)
            player.UpdateEnergy(buff.EnergyBuff);
        if (buff.HealthBuff != 0)
            player.SetProtected(true);
        if (buff.DamageBuff != 0)
            player.UpdateDamage(buff.DamageBuff);
    }
}
