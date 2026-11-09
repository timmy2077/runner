using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class JourneyValidation
{
    static IEnumerator smoke;
    static Func<bool> waiting;
    static double waitStarted;
    static string runtimeError;
    static JourneyValidation()
    {
        if (SessionState.GetBool("JourneySmoke.Active", false))
            EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    [MenuItem("Life Journey/Validate Campaign")]
    public static void Run()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LifeJourneySettings>("Assets/Resources/LifeJourneySettings.asset");
        Check(settings != null && settings.stages.Length == 6, "The campaign must contain six configured stages.");
        Check(settings.groundSprite != null && settings.obstacleSprite != null && settings.memorySprite != null && settings.bossSprite != null && settings.projectileSprite != null, "An existing-art reference is missing.");
        Check(settings.demonSettings != null && settings.generationSettings != null && settings.generationSettings.tuning.IsValid, "Missing or invalid system settings.");
        var kinds = new System.Collections.Generic.HashSet<MindDemon>();
        foreach (var definition in settings.demonSettings.demons)
            Check(definition != null && definition.IsValid && kinds.Add(definition.demon), "Invalid or duplicate mind demon definition.");
        for (int i = 0; i < 6; i++)
        {
            var profile = settings.stages[i];
            Check((int)profile.stage == i, "Life stages are out of order.");
            string path = "Assets/Scenes/" + profile.sceneName + ".unity";
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null, "Missing stage: " + path);
            Check(Array.Exists(EditorBuildSettings.scenes, scene => scene.enabled && scene.path == path), "Stage is disabled in Build Settings.");
            string sceneText = File.ReadAllText(path);
            Check(sceneText.Contains("afec30c5777d920448d3752fc36be1e6"), "The stage has no route generator binding.");
            Check(sceneText.Contains("5664431ba432b664faf677b98fb29a26"), "The stage has no player prefab binding.");
            Check(sceneText.Contains("m_TagString: MainCamera"), "The stage camera is not tagged MainCamera.");
            Check(profile.memories != null && profile.memories.Length == 3, "A stage needs three memories.");
            Check(profile.backgroundPrefab != null, "The stage has no existing-background reference.");
            Check(!sceneText.Contains("difficultyLevels: []") && sceneText.Contains("usePrefabChunks: 1"), "The stage lost its original prefab catalogue.");
            Check(profile.demon == MindDemon.None || settings.demonSettings.Find(profile.demon) != null, "Unconfigured stage encounter.");
            Check(!string.IsNullOrWhiteSpace(profile.introduction) && !string.IsNullOrWhiteSpace(profile.deathText), "Missing chapter narrative.");
        }

        Check(JourneySave.Decode("{not-json") == null, "A corrupt save must be rejected.");
        Check(JourneySave.Decode("{\"version\":99}") == null, "Unsupported save versions must be rejected.");
        var original = new JourneySaveData
        {
            stageIndex = 4,
            checkpointSegment = 8,
            demonMask = 6,
            shortJourney = true,
            routeSeed = -1729
        };
        original.Remember(2, 1);
        var restored = JourneySave.Decode(JsonUtility.ToJson(original));
        Check(restored != null && restored.HasMemory(2, 1) && restored.checkpointSegment == 8 && restored.demonMask == 6 && restored.shortJourney && restored.routeSeed == -1729, "Save serialization lost campaign progress or route seed.");
        Debug.Log("JOURNEY VALIDATION PASSED: six scenes, art references, stage bindings, narrative and save serialization.");
    }

    // Batch-only: -executeMethod JourneyValidation.RunSmoke -journeySmoke (omit -quit).
    public static void RunSmoke()
    {
        Check(Application.isBatchMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-journeySmoke") >= 0, "Smoke tests require batch mode and the isolated -journeySmoke save namespace.");
        JourneySystemsMigration.Run();
        Run();
        runtimeError = null;
        SessionState.SetBool("JourneySmoke.Active", true);
        SessionState.SetBool("JourneySmoke.Failed", false);
        EditorSceneManager.OpenScene("Assets/Scenes/GameStart.unity");
        EditorApplication.playModeStateChanged += PlayModeChanged;
        EditorApplication.EnterPlaymode();
    }

    static void PlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.logMessageReceived += CaptureError;
            smoke = Smoke();
            EditorApplication.update += Tick;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            PlayerPrefs.DeleteKey("runner.lifejourney.smoke");
            PlayerPrefs.DeleteKey("runner.lifejourney.smoke.backup");
            PlayerPrefs.Save();
            SessionState.SetBool("JourneySmoke.Active", false);
            EditorApplication.Exit(SessionState.GetBool("JourneySmoke.Failed", false) ? 1 : 0);
        }
    }

    static void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            runtimeError = message + "\n" + stack;
    }

    static void Tick()
    {
        try
        {
            if (runtimeError != null)
                throw new Exception(runtimeError);
            if (waiting != null && !waiting())
            {
                if (EditorApplication.timeSinceStartup - waitStarted > 30)
                    throw new TimeoutException("Campaign smoke step timed out. State=" + LifeJourneySession.Instance?.State + ", stage=" + LifeJourneySession.Instance?.StageIndex + ", dialog=" + LifeJourneySession.Instance?.UI.IsShowingDialog);
                return;
            }

            waiting = null;
            if (!smoke.MoveNext())
            {
                Finish();
                return;
            }

            waiting = smoke.Current as Func<bool>;
            waitStarted = EditorApplication.timeSinceStartup;
        }
        catch (Exception error)
        {
            SessionState.SetBool("JourneySmoke.Failed", true);
            Debug.LogException(error);
            Finish();
        }
    }

    static Func<bool> Seconds(double seconds)
    {
        double until = EditorApplication.timeSinceStartup + seconds;
        return () => EditorApplication.timeSinceStartup >= until;
    }

    static IEnumerator Smoke()
    {
        yield return (Func<bool>)(() => LifeJourneySession.Instance != null && LifeJourneySession.Instance.State == JourneyState.Menu && LifeJourneySession.Instance.UI.IsShowingDialog);
        var journey = LifeJourneySession.Instance;
        yield return Seconds(0.2);
        Capture("01-menu");
        journey.NewJourney(true);
        yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null && PlayerController.Instance.Profile != null);
        Capture("02-introduction");
        yield return Seconds(0.2);
        journey.BeginStage();
        float start = PlayerController.Instance.transform.position.x;
        yield return Seconds(0.4);
        Capture("03-infancy");
        Check(PlayerController.Instance.transform.position.x > start, "The infant did not start crawling.");
        journey.TogglePause();
        float pausedX = PlayerController.Instance.transform.position.x;
        yield return Seconds(0.2);
        Check(Time.timeScale == 0 && Mathf.Abs(PlayerController.Instance.transform.position.x - pausedX) < 0.01f, "Pause failed to freeze movement.");
        journey.Resume();
        journey.Resume();
        yield return Seconds(0.2);
        journey.SendMessage("OnApplicationFocus", false);
        yield return Seconds(3.1);
        Check(journey.State == JourneyState.Paused && Time.timeScale == 0, "Losing focus during the resume countdown restarted gameplay in the background.");
        journey.Resume();
        yield return (Func<bool>)(() => journey.IsPlaying);
        Check(Time.timeScale == 1, "The resume countdown did not restore time.");
        journey.Remember(0);
        journey.Remember(0);
        Check(journey.Save.MemoryCount == 1, "A repeated pickup duplicated a memory.");
        var player = PlayerController.Instance;
        player.transform.position += Vector3.right * journey.Course.Length * 4;
        yield return (Func<bool>)(() => journey.Save.checkpointSegment >= 4);
        journey.Die();
        Check(Time.timeScale == 0 && journey.State == JourneyState.Dying, "Death did not freeze gameplay.");
        yield return (Func<bool>)(() => journey.State == JourneyState.Dead);
        Capture("04-death");
        yield return Seconds(0.2);
        var volume = Camera.main.GetComponent<Volume>();
        ColorAdjustments color;
        Check(volume != null && volume.sharedProfile.TryGet(out color) && color.saturation.value <= -99, "Death did not finish its grayscale transition in real time.");
        journey.RetryCheckpoint();
        yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null && PlayerController.Instance.Profile != null);
        Check(journey.Save.checkpointSegment >= 4 && journey.Save.MemoryCount == 1 && journey.Save.deaths == 1, "Checkpoint retry lost progress, memories or death count.");
        for (int stage = 0; stage < 6; stage++)
        {
            Check(journey.StageIndex == stage, "The chapter flow skipped a life stage.");
            journey.BeginStage();
            yield return Seconds(0.1);
            player = PlayerController.Instance;
            Check(player.Profile == journey.Profile && Mathf.Approximately(player.moveSpeed, journey.Profile.moveSpeed), "The player did not receive the stage ability profile.");
            float groundOnScreen = Camera.main.WorldToViewportPoint(new Vector3(player.transform.position.x, journey.Course.GroundTop, 0)).y;
            Check(groundOnScreen >= 0.2f && groundOnScreen <= 0.35f, "The ground overlaps the controls or falls outside the gameplay view.");
            Check(journey.Encounter != null && journey.Encounter.transform.parent == null, "Encounter director was attached to a pooled chunk.");
            Check(UnityEngine.Object.FindObjectsOfType<Boss>().Length == 0 && UnityEngine.Object.FindObjectsOfType<Bossapper>().Length == 0, "A legacy boss system is still active in a campaign scene.");
            if (stage == 0)
                CheckObstaclePool(journey);
            if (journey.Profile.HasCombatEncounter)
            {
                player.SetProtected(true);
                player.transform.position = new Vector3(journey.Course.FinishX - journey.Course.SegmentCount * journey.Course.Length * 0.52f, journey.Course.GroundTop + 0.6f, 0);
                yield return (Func<bool>)(() => journey.Boss != null && journey.Boss.CanTakeHit);
                Camera.main.GetComponent<CameraFollow>().SnapToTarget(player.transform);
                yield return Seconds(0.1);
                Capture("boss-" + stage);
                var boss = journey.Boss;
                int health = boss.Health;
                player.TryAttack();
                player.TryAttack();
                Check(UnityEngine.Object.FindObjectsOfType<JourneyProjectile>().Length == 1, "Attack cooldown allowed duplicate shots in one frame.");
                yield return (Func<bool>)(() => boss.Health < health);
                Check(!journey.BossDefeated, "One basic attack skipped the boss encounter.");
                player.transform.position = new Vector3(journey.Encounter.MovementLimit, journey.Course.GroundTop + 0.6f, 0);
                yield return (Func<bool>)(() => boss.Phase == JourneyDemonPhase.Telegraph && boss.ActiveAttackCount > 0);
                foreach (var attack in UnityEngine.Object.FindObjectsOfType<JourneyDemonAttack>())
                    Check(!attack.IsDangerous && attack.transform.parent == journey.Encounter.transform, "Attack warning is lethal or belongs to a streamed chunk.");
                Capture("telegraph-" + stage);
                if (stage == 1)
                {
                    journey.TogglePause();
                    yield return Seconds(0.25);
                    Check(boss.Phase == JourneyDemonPhase.Telegraph, "Paused attack advanced its phase.");
                    foreach (var attack in UnityEngine.Object.FindObjectsOfType<JourneyDemonAttack>())
                        Check(!attack.IsDangerous, "Paused warning activated its hitbox.");
                    journey.Resume();
                    yield return (Func<bool>)(() => journey.IsPlaying);
                }

                yield return (Func<bool>)(() => boss.Phase == JourneyDemonPhase.Attacking);
                yield return Seconds(0.05);
                Check(Array.Exists(UnityEngine.Object.FindObjectsOfType<JourneyDemonAttack>(), attack => attack.IsDangerous), "A telegraphed attack never became active.");
                yield return (Func<bool>)(() => boss.Phase == JourneyDemonPhase.Recovering);
                yield return Seconds(0.05);
                Check(boss.ActiveAttackCount == 0 && UnityEngine.Object.FindObjectsOfType<JourneyDemonAttack>().Length == 0, "Completed attacks were not cleaned up.");
                if (stage == 1)
                {
                    int routeSeed = journey.Save.routeSeed;
                    journey.Die();
                    yield return (Func<bool>)(() => journey.State == JourneyState.Dead);
                    journey.RetryCheckpoint();
                    yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null);
                    journey.BeginStage();
                    player = PlayerController.Instance;
                    player.SetProtected(true);
                    player.transform.position = new Vector3(journey.Course.XAtProgress(0.48f), journey.Course.GroundTop + 0.6f, 0);
                    yield return (Func<bool>)(() => journey.Boss != null && journey.Boss.CanTakeHit);
                    boss = journey.Boss;
                    Check(boss.Health == boss.MaxHealth && journey.Save.routeSeed == routeSeed, "Retry did not rebuild an unresolved boss at full health with the same route seed.");
                    Check(UnityEngine.Object.FindObjectsOfType<JourneyEncounterDirector>().Length == 1 && UnityEngine.Object.FindObjectsOfType<JourneyBossController>().Length == 1, "Retry duplicated the encounter.");
                }

                player.transform.position = new Vector3(journey.Course.FinishX + 0.1f, journey.Course.GroundTop + 0.6f, 0);
                yield return Seconds(0.1);
                journey.CompleteStage();
                Check(journey.IsPlaying, "A living boss did not block stage completion.");
                boss.TakeHit(boss.MaxHealth / 2);
                Check(boss.SecondPhase, "The boss did not enter its second attack phase.");
                boss.TakeHit(10000);
                Check(journey.BossDefeated && boss.Health == 0, "Defeating a boss was not recorded.");
            }
            else if (journey.Profile.demon == MindDemon.Death)
            {
                player.transform.position = new Vector3(journey.Course.XAtProgress(0.5f), journey.Course.GroundTop + 0.6f, 0);
                yield return (Func<bool>)(() => journey.Encounter.State == JourneyEncounterState.Active);
                Check(journey.Boss != null && !journey.Boss.IsAlive && !journey.Boss.CanTakeHit, "Death incorrectly entered combat.");
                journey.DefeatBoss();
                Check(!journey.EncounterResolved, "Death resolved before the companionship route was completed.");
                player.TryAttack();
                Check(UnityEngine.Object.FindObjectsOfType<JourneyProjectile>().Length == 0, "Death allowed shooting.");
                yield return Seconds(0.1);
                Check(UnityEngine.Object.FindObjectsOfType<JourneyDemonAttack>().Length == 0, "Death spawned damaging attacks.");
                Capture("death-companion");
                player.transform.position = new Vector3(journey.Course.FinishX + 0.1f, journey.Course.GroundTop + 0.6f, 0);
                yield return (Func<bool>)(() => journey.EncounterResolved);
            }
            else
                player.transform.position = new Vector3(journey.Course.FinishX + 0.1f, journey.Course.GroundTop + 0.6f, 0);
            yield return (Func<bool>)(() => journey.State == JourneyState.Transition || journey.State == JourneyState.Ending);
            Check(journey.Course.ActiveSegmentCount < 20, "Streaming kept too many old level segments.");
            if (stage < 5)
            {
                journey.NextStage();
                yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null && PlayerController.Instance.Profile != null);
            }
        }

        Check(journey.State == JourneyState.Ending && journey.Save.finished && journey.Save.completedStages == 63, "The final stage did not reach the ending.");
        Capture("09-ending");
        yield return Seconds(0.3);
        var saved = JourneySave.Load();
        Check(saved != null && saved.finished && saved.demonMask == 62, "The finished save lost a boss victory or acceptance of death.");
        journey.ReturnToMenu();
        yield return (Func<bool>)(() => journey.State == JourneyState.Menu);
        journey.ContinueJourney();
        Check(journey.State == JourneyState.Ending, "A completed journey could not reopen its ending.");
        Debug.Log("JOURNEY SMOKE PASSED: six stages; legacy isolation; obstacle pool reuse; attack telegraph/active/recovery and pause; seeded boss retry; four combat demons; peaceful death companion; chapter gates; ending resume.");
    }

    static void CheckObstaclePool(LifeJourneySession journey)
    {
        var pool = journey.Course.Obstacles;
        var parent = new GameObject("Pool regression check").transform;
        parent.SetParent(journey.Course.transform);
        var first = pool.Rent(JourneyObstacle.Gate, parent, Vector2.left * 10000, new Vector2(0.7f, 2.6f), journey.Settings.obstacleSprite, 0, 0);
        int created = pool.CreatedCount;
        first.transform.rotation = Quaternion.Euler(0, 0, 30);
        pool.ReleaseSegment(parent);
        Check(!first.gameObject.activeSelf && !first.GetComponent<BoxCollider2D>().enabled, "Returning a gate left its hitbox active.");
        var reused = pool.Rent(JourneyObstacle.Gate, parent, Vector2.left * 10000, new Vector2(0.7f, 2.6f), journey.Settings.obstacleSprite, 0, 0);
        Check(first == reused && pool.CreatedCount == created && reused.transform.rotation == Quaternion.identity && !reused.GetComponent<BoxCollider2D>().enabled, "Obstacle pool did not reuse and reset its gate.");
        Check(pool.Rent(JourneyObstacle.Pressure, parent, Vector2.zero, Vector2.one, null, 0, 0) == null, "Ordinary pool accepted a boss attack.");
        pool.ReleaseSegment(parent);
        UnityEngine.Object.Destroy(parent.gameObject);
    }

    static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            return;
        var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
        if (camera == null)
            return;
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/JourneyScreenshots"));
        Directory.CreateDirectory(directory);
        var target = new RenderTexture(1280, 720, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var ui = LifeJourneySession.Instance.UI.GetComponentInChildren<Canvas>();
        var previousMode = ui.renderMode;
        var previousCamera = ui.worldCamera;
        try
        {
            camera.targetTexture = target;
            ui.renderMode = RenderMode.ScreenSpaceCamera;
            ui.worldCamera = camera;
            ui.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            ui.renderMode = previousMode;
            ui.worldCamera = previousCamera;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureError;
        PlayerPrefs.DeleteKey("runner.lifejourney.smoke");
        PlayerPrefs.DeleteKey("runner.lifejourney.smoke.backup");
        PlayerPrefs.Save();
        EditorApplication.ExitPlaymode();
    }
}
