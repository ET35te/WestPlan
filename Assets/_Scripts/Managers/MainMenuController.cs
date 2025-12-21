using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button StartBtn;

    void Start()
    {
        StartBtn.onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        // 1. 初始化数据 (可选)
        // GameManager.Instance.ResetData(); 
        
        // 2. 加载游戏场景
        // 这里的 "GameScene" 必须和你保存的场景名字一模一样
        SceneManager.LoadScene("SampleScene"); 
    }
    // 主菜单的“开始游戏”按钮应该绑这个方法
    public void OnStartButtonClick()
    {
        // 1. 让 GM 重置数据 (只是把月份、金钱归位，不要操作 UI)
        GameManager.Instance.ResetDataOnly(); 
        
        // 2. 加载场景 (场景加载完后，GameScene 的 UIManager 会自动执行 ShowNextEvent)
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
