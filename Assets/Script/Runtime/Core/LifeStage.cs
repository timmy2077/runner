using System;
using UnityEngine;

public enum LifeStage
{
    Infancy,
    Childhood,
    Adolescence,
    YoungAdult,
    MiddleAge,
    OldAge
}

public enum JourneyState
{
    Menu,
    Loading,
    Introduction,
    Playing,
    Paused,
    Dying,
    Dead,
    Transition,
    Ending,
    EventChoice,
    EventResult
}

public enum MindDemon
{
    None,
    Fear,
    Anxiety,
    Confusion,
    Pressure,
    Death
}

[Serializable]
public class LifeStageProfile
{
    public LifeStage stage;
    public string sceneName;
    public string title;
    public GameObject backgroundPrefab;
    public Sprite groundSprite;
    public Sprite obstacleSprite;
    [TextArea]
    public string introduction;
    [TextArea]
    public string deathText;
    [TextArea]
    public string departureText;
    public string[] memories;
    [Min(0.1f)]
    public float moveSpeed = 3;
    [Min(0.1f)]
    public float sprintSpeed = 6;
    [Min(0)]
    public float jumpForce = 8;
    [Min(0)]
    public float doubleJumpForce = 7;
    public bool canSprint;
    public bool canDoubleJump;
    public bool canSlide;
    [Min(60)]
    public float targetSeconds = 600;
    public MindDemon demon;
    [Min(1)]
    public int bossHealth = 12;
    public bool CanJump => jumpForce > 0;
    public bool HasCombatEncounter => demon != MindDemon.None && demon != MindDemon.Death;
    public string Controls => "Z / ↓：停下" + (CanJump ? "    空格：跳跃" : "    慢慢爬行") + (canDoubleJump ? "（可二段跳）" : "") + (canSprint ? "    Shift：冲刺" : "") + (canSlide ? "    C：俯身" : "") + (HasCombatEncounter ? "    J：攻击心魔" : "") + "    Esc：暂停";

    public float JumpHeight(float gravity) => CanJump ? jumpForce * jumpForce / (2 * Mathf.Max(0.1f, gravity)) : 0;
    public float JumpRange(float gravity) => CanJump ? moveSpeed * 2 * jumpForce / Mathf.Max(0.1f, gravity) : 0;
}
