using UnityEngine;

/// <summary>
/// 自动创建调试工具
/// </summary>
public class DebugSetup : MonoBehaviour
{
    private static bool initialized = false;

    private void Start()
    {
        if (initialized) return;
        
        // 创建一个空的 GameObject 来挂载调试脚本
        GameObject debugObj = new GameObject("_UIDebugHelper");
        debugObj.AddComponent<UIDebugHelper>();
        
        initialized = true;
        Debug.Log("✅ UI 诊断面板已加载（屏幕右上角）");
    }
}
