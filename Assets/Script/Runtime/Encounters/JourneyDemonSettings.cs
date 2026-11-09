using System;
using UnityEngine;

public enum JourneyEncounterState
{
    Dormant,
    Approaching,
    Active,
    Resolved
}

public enum JourneyDemonPhase
{
    Arriving,
    Telegraph,
    Attacking,
    Recovering,
    Departing
}

[Serializable]
public class JourneyDemonDefinition
{
    public MindDemon demon;
    public string title;
    public Sprite sprite;
    public Color tint = Color.white;
    [Range(0.1f, 0.9f)]
    public float startProgress = 0.45f;
    [Range(0.2f, 0.98f)]
    public float arenaEndProgress = 0.7f;
    [Min(0.5f)]
    public float arrivalSeconds = 2;
    [Min(1)]
    public float warningSeconds = 2;
    [Min(0.2f)]
    public float activeSeconds = 1;
    [Min(1)]
    public float recoverySeconds = 3;
    [Min(0.5f)]
    public float secondPhaseRecovery = 2;
    [TextArea]
    public string introduction;
    [TextArea]
    public string resolution;
    public bool IsCombat => demon != MindDemon.None && demon != MindDemon.Death;
    public bool IsValid => demon != MindDemon.None && !string.IsNullOrWhiteSpace(title) && startProgress >= 0.1f && arenaEndProgress > startProgress && arenaEndProgress < 1 && arrivalSeconds >= 0.5f && warningSeconds >= 1 && activeSeconds > 0 && recoverySeconds >= 1 && secondPhaseRecovery >= 0.5f;
}

[CreateAssetMenu(menuName = "Life Journey/Mind Demon Settings")]
public class JourneyDemonSettings : ScriptableObject
{
    public JourneyDemonDefinition[] demons;
    public JourneyDemonDefinition Find(MindDemon kind) => demons == null ? null : Array.Find(demons, item => item != null && item.demon == kind);
}
