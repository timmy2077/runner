using UnityEditor;
using UnityEngine;

public class BatchRenameTool : EditorWindow
{
    private string prefixName = "Res_";
    private int startNum = 0;
    // 在顶部菜单创建按钮
    [MenuItem("工具/批量重命名工具")]
    static void OpenToolWindow()
    {
        BatchRenameTool window = GetWindow<BatchRenameTool>("批量重命名");
        window.minSize = new Vector2(340, 180);
    }

    // 绘制工具窗口界面
    private void OnGUI()
    {
        GUILayout.Space(12);
        prefixName = EditorGUILayout.TextField("资源前缀：", prefixName);
        startNum = EditorGUILayout.IntField("起始数字：", startNum);
        GUILayout.Space(15);
        if (GUILayout.Button("一键批量重命名", GUILayout.Height(38)))
        {
            StartRename();
        }

        GUILayout.Space(12);
        EditorGUILayout.HelpBox("操作方法：左边Project窗口先选中多个资源，再点按钮", MessageType.Info);
    }

    // 真正执行重命名功能
    void StartRename()
    {
        // 获取你选中的资源
        Object[] selectFiles = Selection.objects;
        // 判断有没有选中东西
        if (selectFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("提醒", "请先选中资源！", "确定");
            return;
        }

        int num = startNum;
        foreach (var file in selectFiles)
        {
            // 获取文件路径
            string path = AssetDatabase.GetAssetPath(file);
            // 拼接新名字 例：Res_0、Res_1
            string newFileName = $"{prefixName}{num}";
            // 执行改名
            AssetDatabase.RenameAsset(path, newFileName);
            num++;
        }

        // 刷新Unity资源
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"一共修改{selectFiles.Length}个文件", "确定");
    }
}
