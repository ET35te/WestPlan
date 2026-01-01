using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🔥 Debug 面板 UI 控制脚本
/// 负责：显示隐藏 + 绑定4个作弊按钮 + 事件ID输入框
/// </summary>
public class DebugPanelUI : MonoBehaviour
{
    [Header("按钮")]
    public Button BtnInfiniteResources;
    public Button BtnOneHitEnemy;
    public Button BtnSelfDestruct;
    public Button BtnJumpToEvent;
    public Button BtnCloseDebug;

    [Header("输入框")]
    public TMP_InputField EventIDInput;

    [Header("面板")]
    public GameObject DebugPanel;
    public GameObject OpenDebugBtn; // 角落的小按钮

    private void Start()
    {
        if (DebugPanel == null)
        {
            Debug.LogError("❌ DebugPanel 未绑定！");
            return;
        }

        // 初始状态：隐藏Debug面板
        DebugPanel.SetActive(false);

        // 绑定按钮事件
        if (OpenDebugBtn != null)
        {
            Button openBtn = OpenDebugBtn.GetComponent<Button>();
            if (openBtn) openBtn.onClick.AddListener(OpenDebugPanel);
        }

        if (BtnCloseDebug) BtnCloseDebug.onClick.AddListener(CloseDebugPanel);
        if (BtnInfiniteResources) BtnInfiniteResources.onClick.AddListener(OnInfiniteResourcesClicked);
        if (BtnOneHitEnemy) BtnOneHitEnemy.onClick.AddListener(OnOneHitEnemyClicked);
        if (BtnSelfDestruct) BtnSelfDestruct.onClick.AddListener(OnSelfDestructClicked);
        if (BtnJumpToEvent) BtnJumpToEvent.onClick.AddListener(OnJumpToEventClicked);
    }

    public void OpenDebugPanel()
    {
        if (DebugPanel != null) DebugPanel.SetActive(true);
        Debug.Log("🔧 [DEBUG] 打开 Debug 面板");
    }

    public void CloseDebugPanel()
    {
        if (DebugPanel != null) DebugPanel.SetActive(false);
        Debug.Log("🔧 [DEBUG] 关闭 Debug 面板");
    }

    // =========================================================
    // 4个作弊功能的UI回调
    // =========================================================

    private void OnInfiniteResourcesClicked()
    {
        if (DebugManager.Instance != null)
        {
            DebugManager.Instance.CheatInfiniteResources();
        }
    }

    private void OnOneHitEnemyClicked()
    {
        if (DebugManager.Instance != null)
        {
            DebugManager.Instance.CheatOneHitEnemy();
        }
    }

    private void OnSelfDestructClicked()
    {
        if (DebugManager.Instance != null)
        {
            DebugManager.Instance.CheatSelfDestruct();
        }
    }

    private void OnJumpToEventClicked()
    {
        if (EventIDInput != null && int.TryParse(EventIDInput.text, out int eventID))
        {
            if (DebugManager.Instance != null)
            {
                DebugManager.Instance.CheatJumpToEvent(eventID);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 请输入有效的事件 ID");
        }
    }
}
