using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only flight recorder: captures the user's actual camera and route without moving either.
[InitializeOnLoad]
public static class JourneyViewDiagnostics
{
    const string Folder = "Logs/JourneyValidation/LiveView";
    static readonly Queue<string> recent = new Queue<string>();
    static double nextSample;
    static bool wasPlaying;
    static float previousVerticalSpeed;
    static int jumps;
    static bool airborne;
    static double captureAfter;

    static JourneyViewDiagnostics()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!EditorApplication.isPlaying || Application.isBatchMode) return;
        if (type == LogType.Exception || type == LogType.Error)
            Record("ERROR " + message + "\n" + stack);
    }

    static void Record(string text)
    {
        recent.Enqueue(DateTime.Now.ToString("HH:mm:ss.fff") + " " + text);
        while (recent.Count > 100) recent.Dequeue();
        Directory.CreateDirectory(Folder);
        File.WriteAllLines(Path.Combine(Folder, "latest.txt"), recent);
    }

    static void Tick()
    {
        if (Application.isBatchMode || EditorApplication.isCompiling) return;
        if (!EditorApplication.isPlaying)
        {
            wasPlaying = false;
            return;
        }
        var player = PlayerController.Instance;
        var journey = LifeJourneySession.Instance;
        if (journey == null || !journey.IsStageScene || player == null) return;
        double now = EditorApplication.timeSinceStartup;
        if (now < nextSample) return;
        nextSample = now + 0.1;
        if (!wasPlaying)
        {
            recent.Clear(); jumps = 0; airborne = false; previousVerticalSpeed = 0;
            wasPlaying = true;
            Record("Project=" + Application.dataPath);
        }
        var body = player.GetComponent<Rigidbody2D>();
        var camera = Camera.main;
        var course = journey.Course;
        var detail = "state=" + journey.State + " stage=" + journey.StageIndex
            + " player=" + player.transform.position.ToString("F3") + " velocity=" + body.velocity.ToString("F3")
            + " grounded=" + player.IsGrounded + " course=" + (course != null ? course.ActiveSegmentCount.ToString() : "null");
        int visible = 0, total = 0;
        if (camera != null)
        {
            detail += " camera=" + camera.transform.position.ToString("F3") + " rotation=" + camera.transform.eulerAngles
                + " size=" + camera.orthographicSize + " enabled=" + camera.enabled
                + " clip=" + camera.nearClipPlane + "/" + camera.farClipPlane + " mask=" + camera.cullingMask
                + " target=" + camera.targetTexture;
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (course != null)
                foreach (var renderer in course.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    total++;
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy
                        && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0
                        && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) visible++;
                }
        }
        Record(detail + " visibleRouteSprites=" + visible + "/" + total);
        bool takeoff = body.velocity.y > 1 && previousVerticalSpeed <= 1;
        previousVerticalSpeed = body.velocity.y;
        if (takeoff && jumps < 5)
        {
            jumps++; airborne = true; captureAfter = now + 0.5;
            Capture("jump-" + jumps + "-takeoff");
        }
        if (airborne && now > captureAfter)
        {
            Capture("jump-" + jumps + "-after");
            airborne = false;
        }
    }

    [MenuItem("Life Journey/诊断/记录当前游戏画面")]
    public static void CaptureCurrent() { Capture("manual"); }

    static void Capture(string name)
    {
        if (Camera.main == null) return;
        Directory.CreateDirectory(Folder);
        Record("CAPTURE " + name);
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
    }
}
