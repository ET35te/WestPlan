using UnityEngine;
using UnityEngine.UI;

public class UIDebugHelper : MonoBehaviour
{
    private void OnGUI()
    {
        // 右上角显示，避免被主菜单遮挡
        GUILayout.BeginArea(new Rect(Screen.width - 350, 10, 340, 300));
        
        GUI.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        GUILayout.Box("🔍 UI 诊断面板 (右上角)");
        GUI.backgroundColor = Color.white;
        
        GUILayout.Label("═══════════════════════");
        
        // 检查 UIManager
        if (UIManager.Instance)
        {
            GUILayout.Label("✅ UIManager 已初始化");
            
            // 检查关键按钮
            GUILayout.Label("按钮状态:");
            
            if (UIManager.Instance.MessagePanel)
            {
                bool isActive = UIManager.Instance.MessagePanel.activeSelf;
                GUILayout.Label($"  📌 MessagePanel: {(isActive ? "🟢 显示" : "🔴 隐藏")}");
            }
            else
            {
                GUILayout.Label($"  📌 MessagePanel: ❌ 未找到");
            }
            
            if (UIManager.Instance.ToBeContinueBtn)
            {
                bool isInteractable = UIManager.Instance.ToBeContinueBtn.interactable;
                GUILayout.Label($"  🔘 继续按钮: {(isInteractable ? "🟢 可点" : "🔴 不可")}");
            }
            else
            {
                GUILayout.Label($"  🔘 继续按钮: ❌ 未找到");
            }
            
            if (UIManager.Instance.ButtonA && UIManager.Instance.ButtonB)
            {
                bool a_ok = UIManager.Instance.ButtonA.interactable;
                bool b_ok = UIManager.Instance.ButtonB.interactable;
                GUILayout.Label($"  🔘 选项按钮: A {(a_ok ? "🟢" : "🔴")} B {(b_ok ? "🟢" : "🔴")}");
            }
        }
        else
        {
            GUILayout.Label("❌ UIManager 未初始化");
        }
        
        GUILayout.Label("═══════════════════════");
        
        // 检查 GameManager
        if (GameManager.Instance)
        {
            GUILayout.Label($"✅ GameManager");
            GUILayout.Label($"  📍 节点: {GameManager.Instance.CurrentNodeIndex}");
            GUILayout.Label($"  📅 月份: {GameManager.Instance.CurrentMonth}");
        }
        else
        {
            GUILayout.Label("❌ GameManager 未初始化");
        }
        
        GUILayout.Label("═══════════════════════");
        
        // 检查 DataManager
        if (DataManager.Instance && DataManager.Instance.IsReady)
        {
            GUILayout.Label("✅ DataManager 已就绪");
            GUILayout.Label($"  📊 事件(v2): {DataManager.Instance.AllEvents_v2.Count}");
            GUILayout.Label($"  📖 剧情: {DataManager.Instance.AllStoryPanels.Count}");
        }
        else
        {
            GUILayout.Label("❌ DataManager 未就绪");
        }
        
        GUILayout.Label("═══════════════════════");
        GUILayout.Label("运行游戏后查看此面板");
        
        GUILayout.EndArea();
    }
}

