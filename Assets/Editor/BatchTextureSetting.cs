using UnityEditor;
using UnityEngine;

public class BatchTextureSetting : EditorWindow
{
    private bool useMipmap = true;
    private TextureImporterCompression compression = TextureImporterCompression.Compressed;

    [MenuItem("工具/批量贴图参数设置")]
    static void OpenWindow()
    {
        BatchTextureSetting win = GetWindow<BatchTextureSetting>("贴图批量设置");
        win.minSize = new Vector2(350, 200);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        useMipmap = EditorGUILayout.Toggle("开启Mipmap", useMipmap);
        compression = (TextureImporterCompression)EditorGUILayout.EnumPopup("贴图压缩", compression);
        GUILayout.Space(15);

        if (GUILayout.Button("一键应用设置", GUILayout.Height(35)))
        {
            ApplySetting();
        }

        EditorGUILayout.HelpBox("操作：选中多张图片贴图，执行批量修改导入配置", MessageType.Info);
    }

    void ApplySetting()
    {
        Object[] selects = Selection.objects;
        int count = 0;
        foreach (var obj in selects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            // 获取贴图导入器
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // 修改参数
            importer.mipmapEnabled = useMipmap;
            importer.textureCompression = compression;
            // 保存设置
            importer.SaveAndReimport();
            count++;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"成功处理{count}张贴图", "确定");
    }
}