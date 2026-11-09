using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChunkSpawner))]
public class JourneyChunkSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LifeJourneySettings>("Assets/Resources/LifeJourneySettings.asset");
        var source = (ChunkSpawner)target;
        if (settings == null || settings.FindStage(source.gameObject.scene.name) < 0)
        {
            DrawDefaultInspector();
            return;
        }

        EditorGUILayout.HelpBox("原始关卡配置已恢复：在 Difficulty Levels → Chunks 中拖入关卡预制体，按列表顺序生成。双击预制体编辑地面、平台和装饰。心魔和人生事件继续使用新系统。", MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("阶段与能力配置"))
            Selection.activeObject = settings;
        if (GUILayout.Button("心魔与出招配置"))
            Selection.activeObject = settings.demonSettings;
        if (GUILayout.Button("随机障碍与权重配置"))
            Selection.activeObject = settings.generationSettings;
    }
}
