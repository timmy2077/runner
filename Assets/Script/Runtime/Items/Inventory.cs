using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public LootSO lootSO;
    public Image itemImage;
    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (itemImage == null)
            return;
        itemImage.gameObject.SetActive(lootSO != null);
        if (lootSO != null)
            itemImage.sprite = lootSO.lootIcon;
    }
}
