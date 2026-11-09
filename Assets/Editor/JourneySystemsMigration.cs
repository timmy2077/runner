using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Non-destructive: refresh presentation references without changing authored chunk catalogues.
public static class JourneySystemsMigration
{
    [MenuItem("Life Journey/Sync Stage Art References (Preserve Chunks)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before migration.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scene edits before migration.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var settings = AssetDatabase.LoadAssetAtPath<LifeJourneySettings>("Assets/Resources/LifeJourneySettings.asset");
        var demonSettings = AssetDatabase.LoadAssetAtPath<JourneyDemonSettings>("Assets/Resources/JourneyDemonSettings.asset");
        var generationSettings = AssetDatabase.LoadAssetAtPath<JourneyGenerationSettings>("Assets/Resources/JourneyGenerationSettings.asset");
        if (settings == null || demonSettings == null || generationSettings == null)
            throw new InvalidOperationException("Campaign system assets did not import as their expected ScriptableObject types.");
        settings.demonSettings = demonSettings;
        settings.generationSettings = generationSettings;
        var previousFlags = settings.hideFlags;
        var previousDemonFlags = demonSettings.hideFlags;
        var previousGenerationFlags = generationSettings.hideFlags;
        settings.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        demonSettings.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        generationSettings.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        try
        {
            foreach (var profile in settings.stages)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + profile.sceneName + ".unity");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var source in root.GetComponentsInChildren<ChunkSpawner>(true))
                    {
                        if (source.difficultyLevels == null || source.difficultyLevels.Length == 0)
                            continue;
                        foreach (var level in source.difficultyLevels)
                        {
                            if (level == null || level.chunks == null)
                                continue;
                            foreach (var prefab in level.chunks)
                            {
                                if (prefab == null)
                                    continue;
                                foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                                {
                                    if (renderer.sprite == null || renderer.GetComponentInParent<Boss>(true) != null || renderer.GetComponentInParent<BossHealth>(true) != null)
                                        continue;
                                    if (renderer.gameObject.layer == 9 && renderer.GetComponent<Collider2D>() != null && !renderer.CompareTag("Obstacle") && renderer.sprite.bounds.size.y <= 0.6f)
                                        profile.groundSprite = renderer.sprite;
                                    if (renderer.CompareTag("Obstacle"))
                                        profile.obstacleSprite = renderer.sprite;
                                }
                            }
                        }

                    }

                if (profile.groundSprite == null)
                    profile.groundSprite = settings.groundSprite;
                if (profile.obstacleSprite == null)
                    profile.obstacleSprite = settings.obstacleSprite;
                EditorSceneManager.SaveScene(scene);
            }

            settings.hideFlags = previousFlags;
            demonSettings.hideFlags = previousDemonFlags;
            generationSettings.hideFlags = previousGenerationFlags;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (settings != null)
                settings.hideFlags = previousFlags;
            if (demonSettings != null)
                demonSettings.hideFlags = previousDemonFlags;
            if (generationSettings != null)
                generationSettings.hideFlags = previousGenerationFlags;
            if (setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        Audit();
        Debug.Log("JOURNEY MIGRATION PASSED: presentation references refreshed; authored chunk catalogues preserved.");
    }

    [MenuItem("Life Journey/Audit Script And Scene Bindings")]
    public static void Audit()
    {
        var guids = new HashSet<string>();
        int scripts = 0;
        foreach (var path in System.IO.Directory.GetFiles("Assets/Script", "*.cs", System.IO.SearchOption.AllDirectories))
        {
            string guid = AssetDatabase.AssetPathToGUID(path.Replace('\\', '/'));
            if (string.IsNullOrEmpty(guid) || !guids.Add(guid))
                throw new Exception("Missing or duplicate script GUID: " + path);
            scripts++;
        }

        var settings = AssetDatabase.LoadAssetAtPath<LifeJourneySettings>("Assets/Resources/LifeJourneySettings.asset");
        foreach (var profile in settings.stages)
        {
            string text = System.IO.File.ReadAllText("Assets/Scenes/" + profile.sceneName + ".unity");
            if (text.Contains("difficultyLevels: []") || !text.Contains("usePrefabChunks: 1"))
                throw new Exception("Campaign must retain its authored chunk catalogue: " + profile.sceneName);
            if (profile.groundSprite == null || profile.obstacleSprite == null)
                throw new Exception("Stage art migration missing: " + profile.sceneName);
        }

        Debug.Log("JOURNEY SCRIPT AUDIT PASSED: " + scripts + " scripts with unique GUIDs; six campaign prefab catalogues preserved.");
    }
}
