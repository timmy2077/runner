using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch validation always runs in a separate checkout and an isolated PlayerPrefs namespace.
[InitializeOnLoad]
public static class LifeEventValidation
{
    static IEnumerator routine;
    static Func<bool> condition;
    static double started;
    static string runtimeError;
    const string Active = "LifeEventSmoke.Active";

    static LifeEventValidation()
    {
        if (SessionState.GetBool(Active, false)) EditorApplication.playModeStateChanged += OnPlayMode;
    }

    public static void RunSmoke()
    {
        Check(Application.isBatchMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-journeySmoke") >= 0,
            "Run only in an isolated project with -journeySmoke.");
        LifeEventTools.InstallExamplesAndPrefab();
        var settings = LifeEventSettings.Load();
        Check(settings != null && settings.ValidateContent().Count == 0 && settings.events.Length == 24, "Content validation failed.");
        Check(JourneySave.Decode("{\"version\":1,\"stageIndex\":2,\"checkpointSegment\":4,\"memoryMask\":3,\"demonMask\":2}")?.lifeEvents != null,
            "Legacy save was not extended without losing progress.");
        var old = JourneySave.Decode("{\"version\":1,\"stageIndex\":2,\"checkpointSegment\":4,\"memoryMask\":3,\"demonMask\":2}");
        Check(old != null && old.stageIndex == 2 && old.memoryMask == 3 && old.demonMask == 2 && old.checkpointSegment == 4, "Legacy progress changed.");
        PlayerPrefs.DeleteKey("runner.lifejourney.smoke");
        PlayerPrefs.DeleteKey("runner.lifejourney.smoke.backup");
        SessionState.SetBool(Active, true);
        SessionState.SetBool("LifeEventSmoke.Failed", false);
        EditorSceneManager.OpenScene("Assets/Scenes/GameStart.unity");
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            runtimeError = null;
            Application.logMessageReceived += CaptureError;
            routine = Smoke();
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            SessionState.SetBool(Active, false);
            PlayerPrefs.DeleteKey("runner.lifejourney.smoke");
            PlayerPrefs.DeleteKey("runner.lifejourney.smoke.backup");
            PlayerPrefs.Save();
            EditorApplication.Exit(SessionState.GetBool("LifeEventSmoke.Failed", false) ? 1 : 0);
        }
    }

    static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException("EVENT QA: " + message); }
    static void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = message + "\n" + stack;
    }

    static void Tick()
    {
        try
        {
            if (runtimeError != null) throw new Exception(runtimeError);
            if (condition != null && !condition())
            {
                if (EditorApplication.timeSinceStartup - started > 35)
                    throw new TimeoutException("Event smoke timed out; state=" + LifeJourneySession.Instance?.State + "; " + LifeJourneySession.Instance?.Events.LastStatus);
                return;
            }
            condition = null;
            if (!routine.MoveNext()) { Finish(); return; }
            condition = routine.Current as Func<bool>;
            started = EditorApplication.timeSinceStartup;
        }
        catch (Exception error)
        {
            SessionState.SetBool("LifeEventSmoke.Failed", true);
            Debug.LogException(error);
            Finish();
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureError;
        EditorApplication.ExitPlaymode();
    }

    static Func<bool> Seconds(double seconds)
    {
        double until = EditorApplication.timeSinceStartup + seconds;
        return () => EditorApplication.timeSinceStartup >= until;
    }

    static void MoveInto(string eventId)
    {
        var point = Array.Find(UnityEngine.Object.FindObjectsOfType<LifeEventTrigger>(), item => item.eventId == eventId);
        Check(point != null, "Missing scene trigger " + eventId);
        var journey = LifeJourneySession.Instance;
        var player = PlayerController.Instance;
        player.SetTouchStop(true);
        player.transform.position = new Vector3(point.Area.bounds.center.x, journey.Course.GroundTop + 0.8f, 0);
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        Camera.main.GetComponent<CameraFollow>().SnapToTarget(player.transform);
        Physics2D.SyncTransforms();
    }

    static IEnumerator Smoke()
    {
        yield return (Func<bool>)(() => LifeJourneySession.Instance != null && LifeJourneySession.Instance.State == JourneyState.Menu);
        var journey = LifeJourneySession.Instance;
        journey.Events.Settings.resumeCountdownSeconds = 0;
        journey.NewJourney(true);
        yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null && PlayerController.Instance.Profile != null);
        journey.BeginStage();
        yield return Seconds(0.15);
        Check(journey.Save.lifeEvents.choices.Count == 0, "A choice was granted without entering a trigger.");
        MoveInto("I01");
        yield return (Func<bool>)(() => journey.State == JourneyState.EventChoice);
        Check(Time.timeScale == 0 && journey.Events.Pending.snapshot.eventId == "I01" && journey.Events.SelectedIndex == -1, "Trigger did not open an unselected modal.");
        journey.Events.Confirm();
        Check(journey.Save.lifeEvents.choices.Count == 0, "Confirm without selection granted a choice.");
        float x = PlayerController.Instance.transform.position.x;
        float elapsed = journey.Save.elapsedSeconds;
        yield return Seconds(0.3);
        Check(Mathf.Abs(PlayerController.Instance.transform.position.x - x) < 0.001f && elapsed == journey.Save.elapsedSeconds, "Event modal did not freeze gameplay.");
        Capture("01-event-choice");
        yield return Seconds(0.15);
        journey.Events.Select(2);
        Debug.Log("EVENT QA STEP: restore unconfirmed selection");
        journey.Events.SaveAndReturn();
        yield return (Func<bool>)(() => journey.State == JourneyState.Menu);
        journey.ContinueJourney();
        yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null);
        journey.BeginStage();
        Check(journey.State == JourneyState.EventChoice && journey.Events.SelectedIndex == 2 && journey.Save.lifeEvents.choices.Count == 0, "Pending card selection was lost across scene reload.");
        Check(Mathf.Abs(PlayerController.Instance.transform.position.x - x) < 0.01f, "Pending event did not restore its trigger position.");
        journey.Events.Confirm();
        journey.Events.Confirm();
        Check(journey.State == JourneyState.EventResult && journey.Save.lifeEvents.choices.Count == 1 && journey.Save.lifeEvents.choices[0].optionId == "I01_C", "Commit duplicated or selected wrong option.");
        Capture("02-event-result");
        yield return Seconds(0.15);
        Debug.Log("EVENT QA STEP: restore committed result");
        journey.Events.SaveAndReturn();
        yield return (Func<bool>)(() => journey.State == JourneyState.Menu);
        journey.ContinueJourney();
        yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null);
        journey.BeginStage();
        Check(journey.State == JourneyState.EventResult && journey.Save.lifeEvents.choices.Count == 1, "Committed result was not restored exactly once.");
        journey.Events.Continue();
        yield return (Func<bool>)(() => journey.IsPlaying);
        PlayerController.Instance.SetTouchStop(true);
        yield return Seconds(0.25);
        Check(journey.IsPlaying && journey.Save.lifeEvents.choices.Count == 1, "Staying inside a completed trigger reopened it.");
        journey.TogglePause();
        Debug.Log("EVENT QA STEP: preview isolation");
        string before = JsonUtility.ToJson(journey.Save);
        string error;
        Check(journey.Events.Preview("Y02", out error), "Cross-stage preview failed: " + error);
        journey.Events.Select(0);
        journey.Events.Confirm();
        journey.Events.Continue();
        Check(journey.State == JourneyState.Paused && before == JsonUtility.ToJson(journey.Save), "Preview modified campaign save or pause state.");
        journey.ResumeAfterEvent(0);
        Debug.Log("EVENT QA STEP: death retry");
        journey.Die();
        yield return (Func<bool>)(() => journey.State == JourneyState.Dead);
        journey.RetryCheckpoint();
        yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null);
        Check(journey.Save.lifeEvents.choices.Count == 1 && journey.Save.lifeEvents.pending == null, "Death retry lost choices.");
        journey.BeginStage();
        MoveInto("I01");
        yield return Seconds(0.3);
        Check(journey.IsPlaying, "Reentering a completed event after death reopened it.");

        // Exercise real, independently placed triggers in every stage, all three answers,
        // plus stage advancement and the existing combat victory gate.
        for (int stage = 0; stage < 6; stage++)
        {
            Debug.Log("EVENT QA STEP: stage " + stage);
            if (stage > 0)
            {
                journey.NextStage();
                yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null && PlayerController.Instance.Profile != null);
                journey.BeginStage();
            }
            string prefix = new[] { "I", "C", "T", "Y", "M", "O" }[stage];
            for (int slot = stage == 0 ? 1 : 0; slot < 3; slot++)
            {
                MoveInto(prefix + "0" + (slot + 1));
                yield return (Func<bool>)(() => journey.State == JourneyState.EventChoice);
                journey.Events.Select(slot);
                journey.Events.Confirm();
                if (stage == 3 && slot == 1)
                {
                    Capture("03-marriage-result");
                    yield return Seconds(0.15);
                }
                journey.Events.Continue();
                yield return (Func<bool>)(() => journey.IsPlaying);
                // Avoid entering the next point accidentally while the script checks results.
                PlayerController.Instance.SetTouchStop(true);
            }
            Check(journey.Save.lifeEvents.choices.Count == (stage + 1) * 3, "A stage did not record three distinct choices.");
            Check(Mathf.Approximately(PlayerController.Instance.moveSpeed, journey.Profile.moveSpeed)
                && Mathf.Approximately(PlayerController.Instance.jumpForce, journey.Profile.jumpForce), "Display-only choices changed player abilities.");
            if (journey.Profile.HasCombatEncounter)
            {
                Check(!journey.BossDefeated, "Event choice incorrectly bypassed the mind demon.");
                // Combat itself is covered by the existing campaign smoke. This suite checks the gate only.
                journey.Save.demonMask |= 1 << stage;
            }
            var player = PlayerController.Instance;
            player.SetProtected(true);
            player.transform.position = new Vector3(journey.Course.FinishX + 0.1f, journey.Course.GroundTop + 0.8f, 0);
            yield return (Func<bool>)(() => journey.State == JourneyState.Transition || journey.State == JourneyState.Ending);
        }
        Check(journey.Save.finished && journey.Save.lifeEvents.choices.Count == 18, "Campaign did not finish with eighteen choices.");
        Check(journey.Save.lifeEvents.choices.Find(item => item.eventId == "Y02").narrativeFlag == "CareerFirst", "Marriage route marker was lost.");
        journey.UI.ShowEventHistory(10, journey.UI.ShowEnding);
        Capture("04-history");
        yield return Seconds(0.15);
        journey.Persist();
        var decoded = JourneySave.Decode(JsonUtility.ToJson(journey.Save));
        if (decoded == null) Debug.Log("EVENT QA SAVE DIAGNOSTIC: valid=" + journey.Save.IsValid + " json=" + JsonUtility.ToJson(journey.Save));
        Check(decoded != null && decoded.finished && decoded.lifeEvents.choices.Count == 18, "Finished save could not round-trip.");
        Debug.Log("LIFE EVENT SMOKE PASSED: 24 events / 72 options; physical trigger entry; pause; choice/result save restoration; idempotency; preview isolation; death retry; six stages / 18 choices; history and ending.");
    }

    static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        string folder = Path.GetFullPath("Logs/LifeEventValidation/screenshots");
        Directory.CreateDirectory(folder);
        var camera = Camera.main;
        if (camera == null) return;
        var target = new RenderTexture(1280, 720, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var canvases = LifeJourneySession.Instance.GetComponentsInChildren<Canvas>();
        var oldModes = new RenderMode[canvases.Length];
        var oldCameras = new Camera[canvases.Length];
        var oldDistances = new float[canvases.Length];
        var oldLayers = new int[canvases.Length];
        try
        {
            camera.targetTexture = target;
            for (int i = 0; i < canvases.Length; i++)
            {
                oldModes[i] = canvases[i].renderMode;
                oldCameras[i] = canvases[i].worldCamera;
                oldDistances[i] = canvases[i].planeDistance;
                oldLayers[i] = canvases[i].sortingLayerID;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = 1;
                canvases[i].sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = oldModes[i];
                canvases[i].worldCamera = oldCameras[i];
                canvases[i].planeDistance = oldDistances[i];
                canvases[i].sortingLayerID = oldLayers[i];
            }
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
