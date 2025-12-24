using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class HierarchyInfoEditor : EditorWindow
{
    private List<string> hierarchyPaths = new List<string>();
    private Vector2 scrollPos; // 滚动视图位置

    // 在编辑器菜单中添加入口
    [MenuItem("Tools/查看Hierarchy层级信息")]
    public static void OpenWindow()
    {
        // 打开自定义编辑器窗口
        HierarchyInfoEditor window = GetWindow<HierarchyInfoEditor>("Hierarchy层级查看器");
        window.minSize = new Vector2(400, 500);
        window.ScanHierarchyInEditor();
    }

    void OnEnable()
    {
        // 窗口激活时重新扫描
        ScanHierarchyInEditor();
    }

    void OnGUI()
    {
        GUILayout.Label("当前场景Hierarchy完整层级", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 刷新按钮
        if (GUILayout.Button("刷新层级信息", GUILayout.Height(30)))
        {
            ScanHierarchyInEditor();
        }
        GUILayout.Space(10);

        // 滚动视图展示层级列表
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var path in hierarchyPaths)
        {
            GUILayout.Label(path);
        }
        EditorGUILayout.EndScrollView();

        // 底部统计信息
        GUILayout.Space(10);
        GUILayout.Label($"总对象数：{hierarchyPaths.Count}", EditorStyles.miniLabel);
    }

    /// <summary>
    /// 编辑模式下扫描Hierarchy
    /// </summary>
    void ScanHierarchyInEditor()
    {
        hierarchyPaths.Clear();
        // 获取当前激活场景
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        foreach (GameObject rootObj in rootObjects)
        {
            RecursiveGetEditorNode(rootObj.transform, 0, rootObj.name);
        }
    }

    /// <summary>
    /// 编辑模式下递归获取节点路径
    /// </summary>
    void RecursiveGetEditorNode(Transform trans, int depth, string parentPath)
    {
        string indent = new string(' ', depth * 2);
        string displayText = $"{indent}[{depth}] {parentPath}";
        hierarchyPaths.Add(displayText);

        for (int i = 0; i < trans.childCount; i++)
        {
            Transform child = trans.GetChild(i);
            string childPath = $"{parentPath}/{child.name}";
            RecursiveGetEditorNode(child, depth + 1, childPath);
        }
    }
}