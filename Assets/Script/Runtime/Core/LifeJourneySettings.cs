using UnityEngine;

[CreateAssetMenu(menuName = "Life Journey/Campaign Settings")]
public class LifeJourneySettings : ScriptableObject
{
    public LifeStageProfile[] stages;
    public Sprite groundSprite;
    public Sprite obstacleSprite;
    public Sprite memorySprite;
    public Sprite bossSprite;
    public Sprite projectileSprite;
    public JourneyDemonSettings demonSettings;
    public JourneyGenerationSettings generationSettings;
    [Min(0.1f)]
    public float attackCooldown = 0.65f;
    [Min(2)]
    public int checkpointEverySegments = 4;
    [Range(0.1f, 1)]
    public float shortJourneyScale = 0.2f;
    [Min(0.5f)]
    public float deathFadeSeconds = 1.8f;
    [TextArea]
    public string endingText = "你曾跑得很快，也曾走得很慢。\n那些曾经拦住你的心魔，都已留在走过的路上。\n\n这一次，可以坐下来，看看海了。";
    public static LifeJourneySettings Load()
    {
        var settings = Resources.Load<LifeJourneySettings>("LifeJourneySettings");
        if (settings != null && settings.stages != null && settings.stages.Length == 6 && settings.demonSettings != null && settings.generationSettings != null)
            return settings;
        Debug.LogError("LifeJourneySettings must contain all six life stages.");
        return null;
    }

    public int FindStage(string sceneName)
    {
        for (int i = 0; i < stages.Length; i++)
            if (stages[i].sceneName == sceneName)
                return i;
        return -1;
    }
}
