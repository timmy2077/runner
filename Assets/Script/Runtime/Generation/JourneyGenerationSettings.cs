using System;
using UnityEngine;

[Serializable]
public class JourneyObstacleRule
{
    public JourneyObstacle obstacle;
    [Min(0)]
    public float weight = 1;
    [Tooltip("Six bits: infancy=1, childhood=2, adolescence=4, young adult=8, middle age=16, old age=32")]
    public int stageMask = 63;
    [Range(0, 1)]
    public float fromProgress;
    [Range(0, 1)]
    public float untilProgress = 1;
    public Sprite sprite;
}

[Serializable]
public class JourneyGenerationTuning
{
    [Range(0, 1)]
    public float startDensity = 0.35f;
    [Range(0, 1)]
    public float endDensity = 0.7f;
    [Min(1)]
    public float recoverySeconds = 1.5f;
    public JourneyObstacleRule[] rules =
    {
        new JourneyObstacleRule
        {
            obstacle = JourneyObstacle.Step,
            weight = 3,
            stageMask = 30
        },
        new JourneyObstacleRule
        {
            obstacle = JourneyObstacle.Gap,
            weight = 2,
            stageMask = 30
        },
        new JourneyObstacleRule
        {
            obstacle = JourneyObstacle.Gate,
            weight = 2
        },
        new JourneyObstacleRule
        {
            obstacle = JourneyObstacle.Ceiling,
            weight = 1,
            stageMask = 30
        }
    };
    public bool IsValid
    {
        get
        {
            if (startDensity < 0 || startDensity > 1 || endDensity < 0 || endDensity > 1 || recoverySeconds < 1 || rules == null)
                return false;
            foreach (var rule in rules)
                if (rule == null || rule.weight < 0 || float.IsNaN(rule.weight) || float.IsInfinity(rule.weight) || rule.stageMask < 0 || rule.stageMask > 63 || rule.fromProgress < 0 || rule.untilProgress > 1 || rule.fromProgress > rule.untilProgress || !JourneyRoutePlan.IsOrdinaryObstacle(rule.obstacle))
                    return false;
            return true;
        }
    }
}

[CreateAssetMenu(menuName = "Life Journey/Random Obstacle Settings")]
public class JourneyGenerationSettings : ScriptableObject
{
    public JourneyGenerationTuning tuning = new JourneyGenerationTuning();
    public Sprite SpriteFor(JourneyObstacle kind, Sprite fallback)
    {
        if (tuning != null && tuning.rules != null)
            foreach (var rule in tuning.rules)
                if (rule != null && rule.obstacle == kind && rule.sprite != null)
                    return rule.sprite;
        return fallback;
    }
}
