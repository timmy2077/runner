using UnityEngine;

[CreateAssetMenu(menuName = "Loot")]
public abstract class LootSO : ScriptableObject
{
    public string lootName;
    public Sprite lootIcon;
    public string lootText;
    public string dialogue;
}
