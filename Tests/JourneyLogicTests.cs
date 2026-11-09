using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

// Run with Tools/Validate-Journey.ps1. These tests execute the real campaign rules against its asset.
public static class JourneyLogicTests
{
    static int assertions;
    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    static string Value(string block, string key)
    {
        var match = Regex.Match(block, @"(?m)^    " + key + @": (.+)\r?$");
        if (!match.Success) throw new Exception("Missing stage field: " + key);
        return match.Groups[1].Value.Trim();
    }
    static float Number(string block, string key) => float.Parse(Value(block, key), CultureInfo.InvariantCulture);

    static LifeStageProfile[] ReadProfiles(string asset)
    {
        var matches = Regex.Matches(File.ReadAllText(asset), @"(?ms)^  - stage: (\d+).*?(?=^  - stage:|^  groundSprite:)");
        Check(matches.Count == 6, "The campaign needs exactly six stages.");
        var profiles = new LifeStageProfile[6];
        for (int i = 0; i < 6; i++)
        {
            string block = matches[i].Value;
            profiles[i] = new LifeStageProfile
            {
                stage = (LifeStage)int.Parse(matches[i].Groups[1].Value), sceneName = Value(block, "sceneName"),
                moveSpeed = Number(block, "moveSpeed"), sprintSpeed = Number(block, "sprintSpeed"),
                jumpForce = Number(block, "jumpForce"), doubleJumpForce = Number(block, "doubleJumpForce"),
                canSprint = Value(block, "canSprint") == "1", canDoubleJump = Value(block, "canDoubleJump") == "1",
                canSlide = Value(block, "canSlide") == "1", targetSeconds = Number(block, "targetSeconds"),
                demon = (MindDemon)(int)Number(block, "demon"), bossHealth = (int)Number(block, "bossHealth")
            };
            Check((int)profiles[i].stage == i, "Stage order must follow age.");
            Check(Regex.Matches(block, @"(?m)^    - " ).Count == 3, "Each stage needs three memories.");
        }
        return profiles;
    }

    static void TestAbilities(LifeStageProfile[] p)
    {
        Check(!p[0].CanJump && !p[0].canSprint, "Infancy must be crawl-only.");
        Check(p[1].CanJump && p[1].canSprint && !p[1].canDoubleJump, "Childhood learns jumping and sprinting.");
        Check(p[2].canSprint && p[2].canDoubleJump, "Adolescence must have the full movement set.");
        Check(!p[3].canSprint && p[3].canDoubleJump, "Young adulthood loses sprinting.");
        Check(!p[4].canSprint && !p[4].canDoubleJump && p[4].CanJump, "Middle age loses the double jump.");
        Check(!p[5].canSlide && !p[5].canSprint && !p[5].canDoubleJump && p[5].CanJump, "Old age keeps only small jumps.");
        for (int i = 0; i < 6; i++)
        {
            Check(p[i].moveSpeed > 0 && p[i].targetSeconds >= 60, "Invalid stage speed/duration.");
            if (i != 2) Check(p[2].moveSpeed > p[i].moveSpeed && p[2].sprintSpeed > p[i].sprintSpeed, "Adolescence must be fastest.");
            if (i > 2) Check(p[i].moveSpeed < p[i - 1].moveSpeed, "Movement must decline after adolescence.");
            Check(!p[i].HasCombatEncounter || (p[i].CanJump && p[i].bossHealth > 1), "Boss battles need valid combat tuning.");
        }
        Check(p[0].demon == MindDemon.None && p[5].demon == MindDemon.Death && !p[5].HasCombatEncounter, "Death is an encounter without combat.");
        Check(p[5].jumpForce < p[4].jumpForce, "The final jump must feel smaller.");
    }

    static void TestRoutes(LifeStageProfile[] profiles)
    {
        const float gravity = 19.62f;
        foreach (var p in profiles)
            foreach (float scale in new[] { 0.2f, 1f })
            {
                int count = JourneyRoutePlan.SegmentCount(p, scale);
                float length = JourneyRoutePlan.SegmentLength(p);
                int memories = 0;
                for (int i = 0; i < count; i++)
                {
                    var plan = JourneyRoutePlan.Build(p, i, count, 4, gravity);
                    var again = JourneyRoutePlan.Build(p, i, count, 4, gravity);
                    Check(plan.obstacle == again.obstacle && plan.gapWidth == again.gapWidth, "Retries must have deterministic obstacles.");
                    if (plan.checkpoint) Check(plan.obstacle == JourneyObstacle.None, "Checkpoints must be on safe flat ground.");
                    if (plan.memorySlot >= 0) { memories++; Check(plan.obstacle == JourneyObstacle.None, "Memories must be reachable without a jump."); }
                    if (!p.CanJump || p.stage == LifeStage.OldAge)
                        Check(plan.obstacle == JourneyObstacle.None || plan.obstacle == JourneyObstacle.Gate, "Aged movement cannot be forced into an impossible jump.");
                    if (plan.obstacle == JourneyObstacle.Gap)
                        Check(plan.gapWidth + 0.54f < p.JumpRange(gravity) * 0.8f && plan.gapWidth < length * 0.3f,
                            "A gap requires sprinting or exceeds the single-jump range.");
                    if (plan.obstacle == JourneyObstacle.Ceiling) Check(p.canSlide, "A low ceiling requires an unavailable slide.");
                    if (plan.obstacle == JourneyObstacle.Step)
                    {
                        double clearanceTime = 2 * Math.Sqrt(p.jumpForce * p.jumpForce - 2 * gravity * plan.obstacleHeight) / gravity;
                        float width = Math.Max(0.45f, p.moveSpeed * 0.12f);
                        Check((width + 0.54) / p.moveSpeed < clearanceTime * 0.85, "An obstacle cannot be cleared at normal speed.");
                    }
                }
                Check(memories == 3, "Every route length must contain all three memories.");
                Check(count * length / p.moveSpeed + 0.01f >= p.targetSeconds * scale, "Route duration is shorter than its tuning.");
            }
    }

    static void TestSave()
    {
        var save = new JourneySaveData();
        Check(save.IsValid && save.MemoryCount == 0, "A fresh save should be valid.");
        for (int stage = 0; stage < 6; stage++)
            for (int slot = 0; slot < 3; slot++)
            {
                Check(save.Remember(stage, slot), "A new memory was not recorded.");
                Check(!save.Remember(stage, slot), "A retry duplicated a memory.");
                Check(save.HasMemory(stage, slot), "A memory was stored under the wrong stage.");
            }
        Check(save.MemoryCount == 18 && save.IsValid, "All memories must fit the save format.");
        Check(!save.Remember(-1, 0) && !save.Remember(6, 0) && !save.Remember(0, 3), "Out-of-range memory ids must be rejected.");
        save.elapsedSeconds = float.NaN; Check(!save.IsValid, "Corrupt elapsed time was accepted.");
        save.elapsedSeconds = -1; Check(!save.IsValid, "Negative elapsed time was accepted.");
        save.elapsedSeconds = 0; save.stageIndex = 6; Check(!save.IsValid, "Invalid chapter was accepted.");
        save.stageIndex = 5; save.finished = true; Check(!save.IsValid, "An incomplete run was marked finished.");
        save.completedStages = 63; Check(save.IsValid, "A finished run should remain reviewable.");
        save.version = 9; Check(!save.IsValid, "Unsupported save versions must be rejected.");
    }

    static void TestRandomGeneration(LifeStageProfile[] profiles)
    {
        const float gravity = 19.62f;
        int changed = 0, generated = 0;
        foreach (var profile in profiles)
            foreach (float scale in new[] { 0.2f, 1f })
                for (int seed = -8; seed < 24; seed++)
                {
                    int count = JourneyRoutePlan.SegmentCount(profile, scale);
                    for (int index = count - 1; index >= 0; index--)
                    {
                        var plan = JourneyRoutePlan.Build(profile, index, count, 4, gravity, seed);
                        var again = JourneyRoutePlan.Build(profile, index, count, 4, gravity, seed);
                        var other = JourneyRoutePlan.Build(profile, index, count, 4, gravity, seed + 101);
                        Check(plan.obstacle == again.obstacle && plan.centerFraction == again.centerFraction
                            && plan.obstacleHeight == again.obstacleHeight && plan.gapWidth == again.gapWidth, "A retry changed its seeded geometry.");
                        if (plan.obstacle != other.obstacle || plan.centerFraction != other.centerFraction) changed++;
                        Check(plan.obstacle == JourneyObstacle.None || JourneyRoutePlan.IsOrdinaryObstacle(plan.obstacle), "A mind demon attack leaked into the ordinary route.");
                        if (plan.checkpoint || plan.memorySlot >= 0 || index < 2 || index >= count - 2)
                            Check(plan.obstacle == JourneyObstacle.None, "A protected segment contains a random obstacle.");
                        if (profile.HasCombatEncounter && index >= UnityEngine.Mathf.FloorToInt(count * 0.45f) - 1 && index <= UnityEngine.Mathf.CeilToInt(count * 0.7f))
                            Check(plan.obstacle == JourneyObstacle.None, "The mind demon corridor is not flat.");
                        if (profile.demon == MindDemon.Death && index >= UnityEngine.Mathf.FloorToInt(count * 0.45f) - 1)
                            Check(plan.obstacle == JourneyObstacle.None, "Death's final stretch must remain peaceful.");
                        if (plan.obstacle == JourneyObstacle.None) continue;
                        generated++;
                        Check(plan.centerFraction >= 0.35f && plan.centerFraction <= 0.65f, "An obstacle is too close to a segment seam.");
                        if (!profile.CanJump || profile.stage == LifeStage.OldAge) Check(plan.obstacle == JourneyObstacle.Gate, "Randomization ignored age abilities.");
                        if (plan.obstacle == JourneyObstacle.Ceiling) Check(profile.canSlide, "Randomization required a locked ability.");
                        if (plan.obstacle == JourneyObstacle.Gap)
                            Check(plan.gapWidth + 0.54f < profile.JumpRange(gravity) * 0.8f, "Random gap cannot be crossed at normal speed.");
                    }
                }
        Check(changed > 100 && generated > 100, "New journey seeds did not vary ordinary obstacles.");
        var tuning = new JourneyGenerationTuning { startDensity = 1, endDensity = 1,
            rules = new[] { new JourneyObstacleRule { obstacle = JourneyObstacle.Gap, weight = 1, stageMask = 63 } } };
        Check(tuning.IsValid, "Valid weighted configuration rejected.");
        Check(JourneyRoutePlan.Build(profiles[0], 3, 100, 4, gravity, 3, tuning).obstacle == JourneyObstacle.None, "Unsafe weighted rule bypassed the infancy filter.");
        Check(JourneyRoutePlan.Build(profiles[1], 3, 100, 4, gravity, 3, tuning).obstacle == JourneyObstacle.Gap, "Weighted rule was not applied.");
        tuning.rules[0].stageMask = 0;
        Check(JourneyRoutePlan.Build(profiles[1], 3, 100, 4, gravity, 3, tuning).obstacle == JourneyObstacle.None, "Empty rule selection must fall back to safe ground.");
        tuning.rules[0].stageMask = 63;
        tuning.startDensity = tuning.endDensity = 0;
        Check(JourneyRoutePlan.Build(profiles[1], 3, 100, 4, gravity, 3, tuning).obstacle == JourneyObstacle.None, "Zero density must disable ordinary obstacles.");
        tuning.rules[0].obstacle = JourneyObstacle.Pressure;
        Check(!tuning.IsValid, "Ordinary generation accepted a mind demon attack rule.");
        Check(JourneyRoutePlan.Build(profiles[1], -1, 100, 4, gravity).obstacle == JourneyObstacle.None, "Behind-start guard segment is unsafe.");
    }

    static void TestDemonPatterns()
    {
        foreach (MindDemon demon in Enum.GetValues(typeof(MindDemon)))
            foreach (bool secondPhase in new[] { false, true })
                for (int sequence = 0; sequence < 4; sequence++)
                {
                    var strikes = JourneyDemonPatterns.Build(demon, sequence, secondPhase, 100, 3, 101);
                    bool combat = demon != MindDemon.None && demon != MindDemon.Death;
                    Check(combat ? strikes.Length > 0 && strikes.Length <= 2 : strikes.Length == 0, "Unexpected demon attack count.");
                    foreach (var strike in strikes)
                    {
                        Check(strike.x - strike.width * 0.5f - strike.travel > 101.3f, "A boss attack traps a player stopped at the arena boundary.");
                        Check(strike.width > 0 && strike.travel >= 0, "Invalid strike geometry.");
                    }
                }
        var pressure = JourneyDemonPatterns.Build(MindDemon.Pressure, 0, false, 100, 3, 200);
        Check(pressure[0].x == 100, "Pressure must sometimes challenge a stationary player away from the arena edge.");
    }

    public static int Main(string[] args)
    {
        try
        {
            var profiles = ReadProfiles(args[0]);
            string buildSettings = File.ReadAllText(args[1]);
            foreach (var p in profiles) Check(buildSettings.Contains("Assets/Scenes/" + p.sceneName + ".unity"), "A chapter is missing from Build Settings.");
            TestAbilities(profiles);
            TestRoutes(profiles);
            TestSave();
            TestRandomGeneration(profiles);
            TestDemonPatterns();
            assertions += LifeEventLogicTests.Run();
            assertions += JourneyPrefabLogicTests.Run();
            Console.WriteLine("PASS: " + assertions + " assertions across six stages, both journey lengths, route reachability, and save invariants.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
