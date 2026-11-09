using UnityEngine;

public enum JourneyObstacle
{
    None,
    Step,
    Gap,
    Gate,
    Ceiling,
    Falling,
    Pressure,
    Moving
}

public struct JourneySegmentPlan
{
    public JourneyObstacle obstacle;
    public float gapWidth;
    public float obstacleHeight;
    public float centerFraction;
    public int memorySlot;
    public bool checkpoint;
}

// Each segment has its own random stream, independent of load/recycle order.
public static class JourneyRoutePlan
{
    static readonly JourneyGenerationTuning defaults = new JourneyGenerationTuning();
    public static float SegmentLength(LifeStageProfile profile) => Mathf.Max(8, profile.moveSpeed * 6);
    public static int SegmentCount(LifeStageProfile profile, float durationScale) => Mathf.Max(16, Mathf.CeilToInt(profile.targetSeconds * durationScale * profile.moveSpeed / SegmentLength(profile)));
    public static bool IsOrdinaryObstacle(JourneyObstacle kind) => kind == JourneyObstacle.Step || kind == JourneyObstacle.Gap || kind == JourneyObstacle.Gate || kind == JourneyObstacle.Ceiling;
    public static JourneySegmentPlan Build(LifeStageProfile profile, int index, int count, int checkpointEvery, float gravity, int seed = 0, JourneyGenerationTuning tuning = null, float encounterStart = 0.45f, float encounterEnd = 0.7f)
    {
        var plan = new JourneySegmentPlan
        {
            memorySlot = -1,
            centerFraction = 0.58f
        };
        if (count <= 0 || index < 0 || index >= count)
            return plan;
        tuning = tuning ?? defaults;
        plan.checkpoint = index % Mathf.Max(2, checkpointEvery) == 0;
        for (int slot = 0; slot < 3; slot++)
            if (index == Mathf.FloorToInt(count * (0.2f + slot * 0.3f)))
                plan.memorySlot = slot;
        if (index < 2 || index >= count - 2 || plan.checkpoint || plan.memorySlot >= 0)
            return plan;
        // Reserve flat ground for encounters, with one approach segment on each side.
        if (profile.HasCombatEncounter && index >= Mathf.FloorToInt(count * encounterStart) - 1 && index <= Mathf.CeilToInt(count * encounterEnd))
            return plan;
        if (profile.demon == MindDemon.Death && index >= Mathf.FloorToInt(count * encounterStart) - 1)
            return plan;
        uint random = Mix(unchecked((uint)seed) ^ unchecked((uint)(index + 1) * 0x9e3779b9u) ^ unchecked((uint)profile.stage * 0x85ebca6bu));
        float progress = (float)index / count;
        if (Next(ref random) >= Mathf.Lerp(tuning.startDensity, tuning.endDensity, progress))
            return plan;
        float total = 0;
        if (tuning.rules == null)
            return plan;
        foreach (var rule in tuning.rules)
            if (Eligible(rule, profile, progress, gravity))
                total += rule.weight;
        if (total <= 0)
            return plan;
        float choice = Next(ref random) * total;
        foreach (var rule in tuning.rules)
        {
            if (!Eligible(rule, profile, progress, gravity))
                continue;
            choice -= rule.weight;
            if (choice <= 0)
            {
                plan.obstacle = rule.obstacle;
                break;
            }
        }

        float speed = profile.canSprint ? Mathf.Max(profile.moveSpeed, profile.sprintSpeed) : profile.moveSpeed;
        float margin = Mathf.Max(2, speed * tuning.recoverySeconds);
        float minFraction = Mathf.Clamp(margin / SegmentLength(profile), 0.35f, 0.45f);
        plan.centerFraction = Mathf.Lerp(minFraction, 1 - minFraction, Next(ref random));
        plan.obstacleHeight = Mathf.Clamp(profile.JumpHeight(gravity) * Mathf.Lerp(0.22f, 0.35f, Next(ref random)), 0.12f, 0.8f);
        plan.gapWidth = Mathf.Min(profile.JumpRange(gravity) * Mathf.Lerp(0.28f, 0.45f, Next(ref random)), SegmentLength(profile) * 0.2f);
        return plan;
    }

    static bool Eligible(JourneyObstacleRule rule, LifeStageProfile profile, float progress, float gravity)
    {
        if (rule == null || rule.weight <= 0 || !IsOrdinaryObstacle(rule.obstacle) || (rule.stageMask & (1 << (int)profile.stage)) == 0 || progress < rule.fromProgress || progress > rule.untilProgress)
            return false;
        if (!profile.CanJump || profile.stage == LifeStage.OldAge)
            return rule.obstacle == JourneyObstacle.Gate;
        if (rule.obstacle == JourneyObstacle.Ceiling)
            return profile.canSlide;
        if (rule.obstacle == JourneyObstacle.Gap)
            return profile.JumpRange(gravity) * 0.35f > 0.54f;
        return true;
    }

    static uint Mix(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return (value ^ (value >> 16)) | 1u;
        }
    }

    static float Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0x00ffffffu) / 16777216f;
    }
}
