using UnityEngine;
using UnityEngine.SceneManagement; // 依然保留，防报错
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("请手动拖拽按钮")]
    public Button StartBtn;
    public Button QuitBtn;

    void Start()
    {
        if (StartBtn) {
            StartBtn.onClick.RemoveAllListeners();
            StartBtn.onClick.AddListener(OnStartGameClicked);
        }
        if (QuitBtn) {
            QuitBtn.onClick.RemoveAllListeners();
            QuitBtn.onClick.AddListener(OnQuitGameClicked);
        }
    }

    // 🔥 核心修改：手动挡点火逻辑
    public void OnStartGameClicked()
    {
        Debug.Log("🖱️ 点击开始：启动游戏流程...");

        // 1. 重置数据 (调用 GM 的新游戏逻辑)
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.StartNewGame();
        }

        // 2. 切换到游戏界面 (调用 UIManager)
        if (UIManager.Instance != null)
        {
            // 切换面板
            UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
            
            // 🔥 点火：手动触发第一个事件！
            // 之前我们在 OnSceneLoaded 里删掉了这行，现在必须在这里补上
            UIManager.Instance.ShowNextEvent();
        }
    }

    public void OnQuitGameClicked()
    {
        Debug.Log("🚪 退出游戏");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}