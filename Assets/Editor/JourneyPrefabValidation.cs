using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyPrefabValidation
{
    static IEnumerator routine;
    static Func<bool> condition;
    static double started;
    static string runtimeError;
    const string Active = "JourneyPrefabSmoke.Active";
    static JourneyPrefabValidation()
    {
        if (SessionState.GetBool(Active, false)) EditorApplication.playModeStateChanged += OnPlayMode;
    }

    public static void RunSmoke()
    {
        Check(Application.isBatchMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-journeySmoke") >= 0, "Isolated QA project required.");
        JourneySystemsMigration.Audit();
        var settings = LifeJourneySettings.Load();
        foreach (var profile in settings.stages)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + profile.sceneName + ".unity");
            var source = UnityEngine.Object.FindObjectOfType<ChunkSpawner>();
            Check(source != null && source.usePrefabChunks && source.spawnDistance == 36 && source.spawnOffset == 25, "Original spawn parameters lost.");
            Check(source.difficultyLevels.Length == (profile.stage == LifeStage.OldAge ? 1 : 3), "Original catalogue group count lost.");
            foreach (var level in source.difficultyLevels)
                foreach (var prefab in level.chunks) Check(prefab != null, "Broken prefab GUID reference.");
        }
        PlayerPrefs.DeleteKey("runner.lifejourney.smoke");
        PlayerPrefs.DeleteKey("runner.lifejourney.smoke.backup");
        SessionState.SetBool(Active, true);
        SessionState.SetBool("JourneyPrefabSmoke.Failed", false);
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
            EditorApplication.Exit(SessionState.GetBool("JourneyPrefabSmoke.Failed", false) ? 1 : 0);
        }
    }

    static void Check(bool value, string message) { if (!value) throw new Exception("PREFAB QA: " + message); }
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
                    throw new TimeoutException("Prefab test timed out. State=" + LifeJourneySession.Instance?.State + "; stage=" + LifeJourneySession.Instance?.StageIndex
                        + "; progress=" + LifeJourneySession.Instance?.Course?.Progress + "; encounter=" + LifeJourneySession.Instance?.Encounter?.State);
                return;
            }
            condition = null;
            if (!routine.MoveNext()) { Finish(); return; }
            condition = routine.Current as Func<bool>;
            started = EditorApplication.timeSinceStartup;
        }
        catch (Exception error)
        {
            SessionState.SetBool("JourneyPrefabSmoke.Failed", true);
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

    static Vector3 OnGround(float x)
    {
        var journey = LifeJourneySession.Instance;
        var hit = Physics2D.Raycast(new Vector2(x, journey.Course.GroundTop + 8), Vector2.down, 40, 1 << 9);
        Check(hit.collider != null && !hit.collider.isTrigger, "No authored ground at x=" + x);
        return new Vector3(x, hit.point.y + 0.6f, 0);
    }

    static IEnumerator Smoke()
    {
        yield return (Func<bool>)(() => LifeJourneySession.Instance != null && LifeJourneySession.Instance.State == JourneyState.Menu);
        var journey = LifeJourneySession.Instance;
        journey.Events.Settings.resumeCountdownSeconds = 0;
        journey.NewJourney(Array.IndexOf(Environment.GetCommandLineArgs(), "-journeyStandard") < 0);
        for (int stage = 0; stage < 6; stage++)
        {
            yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null && PlayerController.Instance.Profile != null);
            Debug.Log("PREFAB QA STAGE " + stage);
            var source = UnityEngine.Object.FindObjectOfType<ChunkSpawner>();
            Check(journey.Course.UsesPrefabChunks, "Campaign used procedural terrain instead of original prefabs.");
            Check(journey.Course.GetComponentsInChildren<JourneyObstacleView>().Length == 0, "Procedural obstacles mixed with authored chunks.");
            var instances = journey.Course.GetComponentsInChildren<Transform>();
            int prefabCount = 0;
            foreach (var instance in instances)
                if (instance.name.StartsWith("Prefab segment "))
                {
                    prefabCount++;
                    bool actualPrefab = false;
                    foreach (Transform child in instance)
                        foreach (var level in source.difficultyLevels)
                            foreach (var prefab in level.chunks)
                                if (child.name == prefab.name + "(Clone)"
                                    && child.GetComponentsInChildren<SpriteRenderer>(true).Length == prefab.GetComponentsInChildren<SpriteRenderer>(true).Length
                                    && child.GetComponentsInChildren<Collider2D>(true).Length == prefab.GetComponentsInChildren<Collider2D>(true).Length)
                                {
                                    Check(Mathf.Abs(child.position.y - source.initialSpawnPosition.y) < 0.001f,
                                        "Prefab root moved vertically away from Initial Spawn Position.");
                                    CheckAuthoredTransforms(child, prefab.transform, true);
                                    actualPrefab = true;
                                }
                    Check(actualPrefab, "Segment did not instantiate an authored prefab.");
                }
            Check(prefabCount > 0, "No original chunks spawned.");
            journey.BeginStage();
            var player = PlayerController.Instance;
            player.SetTouchStop(true);
            yield return Seconds(0.3);
            Check(player.IsGrounded, "Player did not start on original prefab ground.");
            CheckAuthoredFraming(stage);
            Check(UnityEngine.Object.FindObjectsOfType<Boss>().Length == 0 && UnityEngine.Object.FindObjectsOfType<Bossapper>().Length == 0,
                "Embedded legacy boss activated beside campaign encounter.");
            Capture("stage-" + stage);
            if (journey.Profile.CanJump)
            {
                var jumpCamera = Camera.main;
                var cameraPose = jumpCamera.transform.position;
                var jumpBody = player.GetComponent<Rigidbody2D>();
                var takeoff = player.transform.position;
                player.RequestJump();
                yield return (Func<bool>)(() => jumpBody.velocity.y > 1);
                Debug.Log("JUMP QA takeoff stage=" + stage + "; player=" + takeoff + "; camera=" + cameraPose);
                if (journey.Profile.canDoubleJump)
                {
                    yield return Seconds(0.2);
                    float beforeDoubleJump = jumpBody.velocity.y;
                    player.RequestJump();
                    yield return (Func<bool>)(() => jumpBody.velocity.y > beforeDoubleJump + 0.5f);
                }
                yield return (Func<bool>)(() => jumpBody.velocity.y <= 0);
                Check(Mathf.Abs(jumpCamera.transform.position.y - cameraPose.y) < 0.01f
                    && Mathf.Abs(jumpCamera.transform.position.z - cameraPose.z) < 0.01f,
                    "Camera moved during a stationary jump: " + cameraPose + " -> " + jumpCamera.transform.position);
                CheckAuthoredFraming(stage);
                var apex = jumpCamera.WorldToViewportPoint(player.transform.position);
                Check(apex.z > 0 && apex.x >= 0 && apex.x <= 1 && apex.y >= 0 && apex.y <= 1,
                    "Player left the camera at jump apex: " + apex);
                Capture("stage-" + stage + "-jump");
                yield return (Func<bool>)(() => player.IsGrounded && jumpBody.velocity.y <= 0.1f);
                Check(journey.IsPlaying && Mathf.Abs(player.transform.position.y - takeoff.y) < 0.15f,
                    "Player did not land back on the original prefab floor.");
                // Move across a chunk boundary while airborne, using real player input and physics.
                var eventFixtures = UnityEngine.Object.FindObjectsOfType<LifeEventTrigger>();
                foreach (var fixture in eventFixtures) fixture.gameObject.SetActive(false);
                float crossingX = source.initialSpawnPosition.x + source.spawnDistance - 0.3f;
                player.transform.position = OnGround(crossingX);
                jumpBody.velocity = Vector2.zero;
                Camera.main.GetComponent<CameraFollow>().SnapToTarget(player.transform);
                Physics2D.SyncTransforms();
                yield return Seconds(0.3);
                player.SetTouchStop(false);
                player.RequestJump();
                yield return (Func<bool>)(() => jumpBody.velocity.y > 1);
                yield return (Func<bool>)(() => jumpBody.velocity.y <= 0);
                Check(journey.IsPlaying && player.transform.position.x > crossingX,
                    "Running jump failed at the chunk boundary.");
                CheckAuthoredFraming(stage);
                Capture("stage-" + stage + "-crossing-jump");
                player.SetTouchStop(true);
                // Return to the event fixture after exercising actual moving jumps.
                player.transform.position = takeoff;
                jumpBody.velocity = Vector2.zero;
                Camera.main.GetComponent<CameraFollow>().SnapToTarget(player.transform);
                Physics2D.SyncTransforms();
                yield return Seconds(0.3);
                foreach (var fixture in eventFixtures) if (fixture != null) fixture.gameObject.SetActive(true);
            }
            var start = player.transform.position;
            // Real trigger positioned on the existing authored floor; no generated replacement floor.
            var point = new GameObject("QA real prefab event").AddComponent<LifeEventTrigger>();
            point.eventId = new[] { "I01", "C01", "T01", "Y02", "M01", "O01" }[stage];
            point.transform.position = start;
            point.Area.size = new Vector2(1.5f, 4);
            Physics2D.SyncTransforms();
            player.SetTouchStop(false);
            yield return (Func<bool>)(() => journey.State == JourneyState.EventChoice);
            Check(journey.Events.Pending.snapshot.eventId == point.eventId, "Prefab route event ID mismatch.");
            journey.Events.Select(stage % 3);
            journey.Events.Confirm();
            Check(journey.Save.lifeEvents.choices.Count == stage + 1, "Event history lost across prefab stages.");
            if (stage == 0)
            {
                journey.Events.SaveAndReturn();
                yield return (Func<bool>)(() => journey.State == JourneyState.Menu);
                journey.ContinueJourney();
                yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null);
                journey.BeginStage();
                Check(journey.State == JourneyState.EventResult && journey.Events.Pending.snapshot.eventId == "I01", "Prefab event result failed to restore.");
                player = PlayerController.Instance;
            }
            journey.Events.Continue();
            yield return (Func<bool>)(() => journey.IsPlaying);
            player.SetTouchStop(true);
            if (point != null) UnityEngine.Object.Destroy(point.gameObject);
            if (stage == 0)
            {
                journey.Die();
                yield return (Func<bool>)(() => journey.State == JourneyState.Dead);
                journey.RetryCheckpoint();
                yield return (Func<bool>)(() => journey.State == JourneyState.Introduction && journey.Course != null);
                Check(journey.Course.UsesPrefabChunks && journey.Save.lifeEvents.choices.Count == 1, "Death retry lost authored route or event history.");
                journey.BeginStage();
                player = PlayerController.Instance;
                player.SetTouchStop(true);
            }

            // Teleport only accelerates the test. Ground generation and combat remain real.
            player.transform.position = new Vector3(journey.Course.XAtProgress(0.48f), journey.Course.GroundTop + 1, 0);
            yield return Seconds(0.15);
            player.transform.position = OnGround(player.transform.position.x);
            player.SetTouchStop(true);
            Physics2D.SyncTransforms();
            if (journey.Profile.HasCombatEncounter)
            {
                yield return (Func<bool>)(() => journey.Boss != null && journey.Boss.CanTakeHit);
                int health = journey.Boss.Health;
                player.TryAttack();
                yield return (Func<bool>)(() => journey.Boss.Health < health);
                Check(!journey.BossDefeated, "A basic shot skipped combat.");
                journey.Boss.TakeHit(10000);
                Check(journey.BossDefeated, "Boss victory did not persist on prefab route.");
            }
            else if (journey.Profile.demon == MindDemon.Death)
                yield return (Func<bool>)(() => journey.Encounter.State == JourneyEncounterState.Active);

            player.transform.position = new Vector3(journey.Course.FinishX + 0.1f, journey.Course.GroundTop + 0.6f, 0);
            Physics2D.SyncTransforms();
            yield return (Func<bool>)(() => journey.State == JourneyState.Transition || journey.State == JourneyState.Ending);
            Check(journey.Course.ActiveSegmentCount < 20, "Old prefab segments were not recycled.");
            if (stage < 5) journey.NextStage();
        }
        var decoded = JourneySave.Decode(JsonUtility.ToJson(journey.Save));
        Check(decoded != null && decoded.finished && decoded.lifeEvents.choices.Count == 6, "Final prefab campaign save invalid.");
        Debug.Log("PREFAB RESTORE SMOKE PASSED: six original prefab catalogues; real prefab instances; physical ground; event trigger and result restore; actual projectile hit; boss victories; recycling; ending save.");
    }

    static void CheckAuthoredTransforms(Transform instance, Transform original, bool root = false)
    {
        Check((root || Vector3.Distance(instance.localPosition, original.localPosition) < 0.001f)
            && Vector3.Distance(instance.localScale, original.localScale) < 0.001f
            && Quaternion.Angle(instance.localRotation, original.localRotation) < 0.01f,
            "Authored prefab transform changed: " + original.name);
        Check(instance.childCount == original.childCount, "Generated scenery was added to " + original.name);
        for (int i = 0; i < original.childCount; i++)
            CheckAuthoredTransforms(instance.GetChild(i), original.GetChild(i));
    }

    static void CheckAuthoredFraming(int stage)
    {
        var journey = LifeJourneySession.Instance;
        var camera = Camera.main;
        var backgrounds = UnityEngine.Object.FindObjectsOfType<InfiniteBackground>();
        Check(!Array.Exists(backgrounds, item => item.name == "Existing stage background"),
            "A second landscape was added beneath the authored chunk at stage " + stage);
        Check(!Array.Exists(journey.UI.GetComponentsInChildren<UnityEngine.UI.Image>(), item => item.name == "Controls backdrop"),
            "The background was hidden behind the old opaque control panel.");
        var aligned = journey.Course.GetComponentsInChildren<JourneyBackgroundAlignment>();
        Check(aligned.Length > 0, "No existing chunk backgrounds aligned at stage " + stage);
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        Check(Array.Exists(aligned, item => item.GetComponent<SpriteRenderer>().enabled
            && (camera.cullingMask & (1 << item.gameObject.layer)) != 0
            && GeometryUtility.TestPlanesAABB(planes, item.AuthoredBounds)),
            "The camera cannot see any authored landscape at stage " + stage);
        foreach (var background in aligned)
        {
            var bounds = background.AuthoredBounds;
            Check(camera.transform.position.y - camera.orthographicSize >= bounds.min.y - 0.001f
                && camera.transform.position.y + camera.orthographicSize <= bounds.max.y + 0.001f,
                "Camera extends outside the authored landscape at stage " + stage);
            Check(background.GetComponentsInChildren<MeshRenderer>(true).Length == 0,
                "A generated solid strip was added beneath the original landscape.");
            // Compare with the original nested prefab instance, including its authored overrides.
            var original = PrefabUtility.GetCorrespondingObjectFromSource(background.transform);
            if (original != null)
                Check(background.transform.localPosition == original.localPosition && background.transform.localScale == original.localScale,
                    "Background alignment moved the authored grass line away from the gameplay floor.");
        }
    }

    static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Camera.main == null) return;
        var camera = Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var canvases = LifeJourneySession.Instance.GetComponentsInChildren<Canvas>();
        var modes = new RenderMode[canvases.Length];
        var cameras = new Camera[canvases.Length];
        var layers = new int[canvases.Length];
        try
        {
            camera.targetTexture = target;
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; layers[i] = canvases[i].sortingLayerID;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1;
                canvases[i].sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
            }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            string folder = Path.GetFullPath("Logs/PrefabRestore/screenshots"); Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].sortingLayerID = layers[i]; }
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
