using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("请手动拖拽按钮")]
    public Button StartBtn;
    public Button QuitBtn;

    void Start()
    {
        // 1. 绑定开始按钮
        if (StartBtn != null)
        {
            StartBtn.onClick.RemoveAllListeners();
            StartBtn.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            Debug.LogError("❌ MainMenuController: 未绑定 StartBtn！请在 Inspector 中拖拽。");
        }

        // 2. 绑定退出按钮
        if (QuitBtn != null)
        {
            QuitBtn.onClick.RemoveAllListeners();
            QuitBtn.onClick.AddListener(OnQuitGameClicked);
        }
        // 退出按钮如果不拖，只是不能退出，不报错也可以
    }

    public void OnStartGameClicked()
    {
        Debug.Log("🖱️ 点击开始...");
        if (GameManager.Instance != null) GameManager.Instance.ResetDataOnly();

        // 加载场景
        SceneManager.LoadScene("SampleScene");
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