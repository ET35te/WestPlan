using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按钮点击事件监听器 - 用于诊断按钮是否真的被点击
/// </summary>
public class ButtonClickDebugger : MonoBehaviour
{
    private Button btn;

    private void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null)
        {
            // 添加一个监听，用于诊断
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"🎯 ============ 按钮被物理点击: {gameObject.name} ============");
                Debug.Log($"   时间戳: {Time.time}");
                Debug.Log($"   Button.interactable: {btn.interactable}");
                Debug.Log($"   GameObject.activeSelf: {gameObject.activeSelf}");
                Debug.Log($"   GameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
            });
        }
    }
}
