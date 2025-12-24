using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("自动绑定状态")]
    public Button StartBtn;
    public Button QuitBtn;

    void Start()
    {
        // --- 1. 自动寻找按钮 (双重保险) ---
        // 找 "Start_Btn" (你的层级名字) 或 "StartBtn" (备用名)
        if (StartBtn == null)
        {
            GameObject obj = GameObject.Find("Start_Btn");
            if (obj == null) obj = GameObject.Find("StartBtn");
            if (obj != null) StartBtn = obj.GetComponent<Button>();
        }

        if (QuitBtn == null)
        {
            GameObject obj = GameObject.Find("Quit_Btn");
            if (obj == null) obj = GameObject.Find("QuitBtn");
            if (obj != null) QuitBtn = obj.GetComponent<Button>();
        }

        // --- 2. 绑定事件 ---
        if (StartBtn != null)
        {
            // 先移除旧的，防止点了没反应或点一次触发两次
            StartBtn.onClick.RemoveAllListeners();
            StartBtn.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            Debug.LogError("❌ MainMenuController: 找不到 Start_Btn！请检查 Inspector 或物体名字。");
        }

        if (QuitBtn != null)
        {
            QuitBtn.onClick.RemoveAllListeners();
            QuitBtn.onClick.AddListener(OnQuitGameClicked);
        }
    }

    public void OnStartGameClicked()
    {
        Debug.Log("🖱️ 点击开始，尝试进入游戏...");

        // 1. 安全重置数据
        // 如果因为改名问题导致 GM 还没挂载好，这里也不会报错，只会报 Warning 然后继续进游戏
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetDataOnly();
        }
        else
        {
            Debug.LogWarning("⚠️ 主菜单未找到 GameManager (可能是文件名没改对)，将尝试直接进入场景。");
        }

        // 2. 加载场景
        string sceneName = "SampleScene"; // 你的游戏场景名
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"❌ 无法加载场景 '{sceneName}'！请检查 File -> Build Settings 是否添加了该场景！");
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