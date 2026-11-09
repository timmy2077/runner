using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LifeEventTools
{
    public const string SettingsPath = "Assets/Resources/LifeEventSettings.asset";
    public const string PrefabPath = "Assets/Prefabs/Events/LifeEventTrigger.prefab";

    [MenuItem("Life Journey/人生事件/打开事件配置")]
    public static void OpenSettings()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<LifeEventSettings>(SettingsPath);
        if (Selection.activeObject == null) Debug.LogError("缺少 " + SettingsPath);
    }

    [MenuItem("GameObject/Life Journey/人生事件点", false, 10)]
    public static void CreatePoint(MenuCommand command)
    {
        var root = new GameObject("Life Event Point");
        Undo.RegisterCreatedObjectUndo(root, "创建人生事件点");
        GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
        var point = root.AddComponent<LifeEventTrigger>();
        var campaign = LifeJourneySettings.Load();
        int stage = campaign != null ? campaign.FindStage(root.scene.name) : 0;
        point.eventId = new[] { "I01", "C01", "T01", "Y01", "M01", "O01" }[Mathf.Clamp(stage, 0, 5)];
        if (SceneView.lastActiveSceneView != null && command.context == null)
        {
            var position = SceneView.lastActiveSceneView.pivot;
            position.z = 0;
            root.transform.position = position;
        }
        Selection.activeGameObject = root;
    }

    [MenuItem("Life Journey/人生事件/验证事件库和当前场景")]
    public static void ValidateCurrent()
    {
        var settings = LifeEventSettings.Load();
        var errors = settings != null ? settings.ValidateContent() : new List<string> { "缺少事件库。" };
        var campaign = LifeJourneySettings.Load();
        int stage = campaign != null ? campaign.FindStage(SceneManager.GetActiveScene().name) : -1;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        foreach (var point in UnityEngine.Object.FindObjectsOfType<LifeEventTrigger>(true))
        {
            if (point.gameObject.scene != SceneManager.GetActiveScene()) continue;
            count++;
            var item = settings != null ? settings.Find(point.eventId) : null;
            if (item == null) errors.Add(point.name + "：找不到唯一事件 ID " + point.eventId);
            else if (stage >= 0 && (int)item.stage != stage) errors.Add(point.name + "：事件所属阶段与当前场景不匹配。");
            if (!seen.Add(point.eventId)) Debug.LogWarning("同场景存在相同事件 ID，只会完成一次：" + point.eventId, point);
            if (!point.Area.isTrigger || !point.Area.enabled) errors.Add(point.name + "：BoxCollider2D 需要启用并勾选 Is Trigger。");
            if (point.transform.lossyScale.x <= 0 || point.transform.lossyScale.y <= 0) errors.Add(point.name + "：请使用正数缩放。");
        }
        foreach (var error in errors) Debug.LogError("人生事件校验：" + error);
        if (errors.Count == 0) Debug.Log("人生事件校验通过：事件 " + settings.events.Length + " 个，当前场景触发器 " + count + " 个（推荐三个，数量不强制）。");
    }

    [MenuItem("Life Journey/人生事件/在当前场景添加三处示例点")]
    public static void AddRecommendedPoints()
    {
        if (Application.isPlaying) { Debug.LogWarning("请退出播放后再布置场景点。"); return; }
        var campaign = LifeJourneySettings.Load();
        int stage = campaign != null ? campaign.FindStage(SceneManager.GetActiveScene().name) : -1;
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (stage < 0 || player == null) { Debug.LogWarning("请打开包含玩家的六个正式关卡之一。"); return; }
        if (UnityEngine.Object.FindObjectsOfType<LifeEventTrigger>(true).Length > 0)
        { Debug.LogWarning("场景已有事件点，不重复添加。可手动使用 GameObject 菜单新增。"); return; }
        var root = new GameObject("Life Event Points");
        Undo.RegisterCreatedObjectUndo(root, "添加三处人生事件点");
        var profile = campaign.stages[stage];
        float length = JourneyRoutePlan.SegmentLength(profile);
        int count = JourneyRoutePlan.SegmentCount(profile, campaign.shortJourneyScale);
        float origin = player.transform.position.x - 2;
        string prefix = new[] { "I", "C", "T", "Y", "M", "O" }[stage];
        float[] referencePositions = { 0.12f, 0.32f, 0.78f };
        for (int i = 0; i < 3; i++)
        {
            var child = new GameObject(prefix + "0" + (i + 1) + " Event Point");
            child.transform.SetParent(root.transform, false);
            child.transform.position = new Vector3(origin + length * Mathf.Max(1, Mathf.FloorToInt(count * referencePositions[i])) + 2,
                player.transform.position.y + 0.5f, 0);
            var point = child.AddComponent<LifeEventTrigger>();
            point.eventId = prefix + "0" + (i + 1);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("已创建三个可自由移动的示例触发器。位置按短篇路程初始估算，运行时不会随进度或模式移动。请保存场景。", root);
    }

    // Invoked only in the isolated QA project, or explicitly from a command line.
    public static void InstallExamplesAndPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Events")) AssetDatabase.CreateFolder("Assets/Prefabs", "Events");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            var root = new GameObject("Life Event Point");
            root.AddComponent<LifeEventTrigger>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }
        var campaign = LifeJourneySettings.Load();
        foreach (var profile in campaign.stages)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + profile.sceneName + ".unity");
            AddRecommendedPoints();
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
    }
}

[CustomEditor(typeof(LifeEventTrigger))]
public class LifeEventTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var point = (LifeEventTrigger)target;
        var settings = LifeEventSettings.Load();
        var id = serializedObject.FindProperty("eventId");
        if (settings != null && settings.events != null)
        {
            var labels = new List<string> { "自定义 ID / 当前值：" + id.stringValue };
            foreach (var item in settings.events) labels.Add(item != null ? item.stage + " / " + item.eventId + " / " + item.title : "空事件");
            int selected = EditorGUILayout.Popup("从事件库选择", 0, labels.ToArray());
            if (selected > 0 && settings.events[selected - 1] != null) id.stringValue = settings.events[selected - 1].eventId;
        }
        EditorGUILayout.PropertyField(id, new GUIContent("事件 ID"));
        DrawPropertiesExcluding(serializedObject, "m_Script", "eventId");
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("移动 Transform 设置抽卡位置；修改 BoxCollider2D 的 Size / Offset 设置范围。每个事件 ID 每轮人生只确认一次。当前使用原始预制体关卡，请将触发器放在预制体实际地面上；Reserve Flat Ground 仅在程序地形模式生效。暂不执行能力，无刷新功能。", MessageType.Info);
        var definition = settings != null ? settings.Find(point.eventId) : null;
        string error = null;
        if (definition == null || !definition.IsValid(out error))
            EditorGUILayout.HelpBox(error ?? "找不到唯一事件 ID，请检查事件库。", MessageType.Error);
        else
        {
            EditorGUILayout.LabelField(definition.title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(definition.prompt, EditorStyles.wordWrappedLabel);
            foreach (var option in definition.options)
                EditorGUILayout.LabelField(option.answer + " → " + option.traitName + "：" + option.plannedEffect, EditorStyles.wordWrappedLabel);
            var campaign = LifeJourneySettings.Load();
            int stage = campaign != null ? campaign.FindStage(point.gameObject.scene.name) : -1;
            if (stage >= 0 && stage != (int)definition.stage)
                EditorGUILayout.HelpBox("该事件所属阶段与场景不匹配，真实触发会拒绝。预览允许跨阶段。", MessageType.Warning);
        }
        if (GUILayout.Button("打开事件配置")) LifeEventTools.OpenSettings();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("预览此事件（不写存档，可反复测试）"))
            {
                string previewError = "旅程尚未初始化。";
                if (LifeJourneySession.Instance == null || !LifeJourneySession.Instance.Events.Preview(point.eventId, out previewError))
                    Debug.LogWarning(previewError, point);
            }
            if (GUILayout.Button("移动玩家进入此点（真实触发并存档）") && PlayerController.Instance != null)
            {
                var player = PlayerController.Instance;
                player.transform.position = point.Area.bounds.center;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                Physics2D.SyncTransforms();
            }
        }
        if (Application.isPlaying && LifeJourneySession.Instance != null)
            EditorGUILayout.LabelField("运行状态", LifeJourneySession.Instance.Events.LastStatus, EditorStyles.wordWrappedLabel);
    }

    void OnSceneGUI()
    {
        var point = (LifeEventTrigger)target;
        Handles.Label(point.transform.position + Vector3.up * 2.5f, point.eventId + " · 人生抉择");
    }
}

[CustomEditor(typeof(LifeEventSettings))]
public class LifeEventSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("能力字段只展示并记录，尚未执行；事件 ID 区分大小写。修改 ID 后请同步修改场景触发器。播放期间可改文案，但已经弹出的事件使用存档快照。", MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("验证事件库"))
        {
            var errors = ((LifeEventSettings)target).ValidateContent();
            foreach (var error in errors) Debug.LogError(error, target);
            if (errors.Count == 0) Debug.Log("事件库校验通过。", target);
        }
        if (GUILayout.Button("打开调试面板")) LifeEventDebugWindow.Open();
    }
}

public class LifeEventDebugWindow : EditorWindow
{
    string eventId = "Y02";
    Vector2 scroll;
    [MenuItem("Life Journey/人生事件/调试面板")]
    public static void Open() { GetWindow<LifeEventDebugWindow>("人生事件调试"); }

    void OnInspectorUpdate() { Repaint(); }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("预览可跨阶段、反复选卡，不改玩家选择记录。真实触发请使用场景里的触发器。", MessageType.Info);
        eventId = EditorGUILayout.TextField("预览事件 ID", eventId);
        var journey = LifeJourneySession.Instance;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || journey == null))
        {
            if (GUILayout.Button("打开三卡预览"))
            {
                string error;
                if (!journey.Events.Preview(eventId, out error)) Debug.LogWarning(error);
            }
            if (GUILayout.Button("关闭预览")) journey.Events.ClosePreview();
        }
        if (GUILayout.Button("打开配置资产")) LifeEventTools.OpenSettings();
        if (journey == null) return;
        EditorGUILayout.LabelField("流程状态", journey.State.ToString());
        EditorGUILayout.LabelField("最近操作", journey.Events.LastStatus, EditorStyles.wordWrappedLabel);
        var data = journey.Save?.lifeEvents;
        EditorGUILayout.LabelField("待选事件", data?.pending?.snapshot?.eventId ?? "无");
        EditorGUILayout.LabelField("已记录选择", (data?.choices?.Count ?? 0).ToString());
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (data?.choices != null)
            foreach (var item in data.choices) EditorGUILayout.LabelField(item.eventId + " / " + item.optionId + " / " + item.answer, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }
}
