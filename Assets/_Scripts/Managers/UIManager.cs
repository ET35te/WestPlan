using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 必须引用
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum UIState 
    { 
        MainMenu, Gameplay, Result, Achievement, Battle, NodeSummary, Ending 
    }
    
    [Header("--- 层级 1: 核心面板 ---")]
    public GameObject MainMenuPanel;
    public GameObject GameplayPanel;
    public GameObject ResultPanel;
    public GameObject AchievementPanel;
    public GameObject NodeSummaryPanel;
    public GameObject BattlePanel;

    [Header("--- 层级 2 & 3: 辅助层级 ---")]
    public GameObject HUDLayer;         
    public GameObject EndingLayer;      

    [Header("--- 1. 主菜单组件 ---")]
    public TMP_Text TitleText;          
    public Button StartBtn;             
    public Button QuitBtn;              
    public Button AchievementBtn;       

    [Header("--- 2. 游玩界面组件 ---")]
    public TMP_Text EventTitleText;     
    public TMP_Text PlaceText;          
    public TMP_Text ContextText;        
    public Button ButtonA;              
    public Button ButtonB;              

    [Header("--- 3. 结果界面组件 ---")]
    public TMP_Text ResultText;         
    public Button ConfirmResultBtn;     

    [Header("--- 4. 节点总结组件 ---")]
    public TMP_Text SummaryTitleText;   
    public TMP_Text SummaryContentText; 
    public Button ToBeContinueBtn;      

    [Header("--- 5. 战斗界面组件 ---")]
    public Transform CardContainer;     
    public Button ConfirmBattleBtn;     

    [Header("--- 6. 结局界面组件 ---")]
    public TMP_Text ScrollingText;      

    [Header("--- HUD 组件 ---")]
    public Button GlobalQuitToTitleBtn; 
    public TMP_Text ResourceInfoText;   

    private DataManager.EventData currentEvent;
    private UIState currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; } 
    }

    void Start()
    {
        StartBtn.onClick.AddListener(OnClickStartGame);
        QuitBtn.onClick.AddListener(OnClickQuitGame);
        AchievementBtn.onClick.AddListener(() => SwitchState(UIState.Achievement));

        ButtonA.onClick.AddListener(() => OnSelectOption(true));
        ButtonB.onClick.AddListener(() => OnSelectOption(false));

        ConfirmResultBtn.onClick.AddListener(ReturnToGameplay); 
        ToBeContinueBtn.onClick.AddListener(OnClickNextNode); 
        GlobalQuitToTitleBtn.onClick.AddListener(OnClickReturnToTitle);

        SwitchState(UIState.MainMenu);
    }

    public void SwitchState(UIState newState)
    {
        currentState = newState;

        MainMenuPanel.SetActive(false);
        GameplayPanel.SetActive(false);
        ResultPanel.SetActive(false);
        AchievementPanel.SetActive(false);
        NodeSummaryPanel.SetActive(false);
        BattlePanel.SetActive(false);

        bool showHUD = (newState != UIState.MainMenu && newState != UIState.Ending);
        HUDLayer.SetActive(showHUD);
        EndingLayer.SetActive(newState == UIState.Ending);

        switch (newState)
        {
            case UIState.MainMenu: MainMenuPanel.SetActive(true); break;
            case UIState.Gameplay: GameplayPanel.SetActive(true); break;
            case UIState.Result: ResultPanel.SetActive(true); break;
            case UIState.Achievement: AchievementPanel.SetActive(true); break;
            case UIState.NodeSummary: NodeSummaryPanel.SetActive(true); break;
            case UIState.Battle: BattlePanel.SetActive(true); break;
            case UIState.Ending: /* 结局逻辑 */ break;
        }
    }

    void OnClickStartGame()
    {
        SwitchState(UIState.Gameplay);
        UpdateResourceDisplay();
        ShowNextEvent(); 
    }

    void OnClickQuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnClickReturnToTitle()
    {
        SwitchState(UIState.MainMenu);
    }

    public void ShowNextEvent()
    {
        currentEvent = DataManager.Instance.GetRandomEvent();
        if (currentEvent == null) return;

        if (currentEvent.IsPeaceful == false)
        {
            SwitchState(UIState.Battle);
            BattleManager.Instance.StartBattle(currentEvent);
        }
        else
        {
            SwitchState(UIState.Gameplay);
            EventTitleText.text = currentEvent.Title;
            ContextText.text = currentEvent.Context;
            
            ButtonA.GetComponentInChildren<TMP_Text>().text = currentEvent.OptA_Text;
            ButtonB.GetComponentInChildren<TMP_Text>().text = currentEvent.OptB_Text;
        }
    }

    void OnSelectOption(bool isA)
    {
        string resultStr = GameManager.Instance.ResolveEventOption(currentEvent, isA);
        ShowResult(resultStr); // 调用下面的 ShowResult
    }

    // --- 修复点：这里是新增的 ShowResult 方法 ---
    public void ShowResult(string resultStr)
    {
        SwitchState(UIState.Result);
        ResultText.text = resultStr;
        UpdateResourceDisplay();
    }
    // ------------------------------------------

    void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        ShowNextEvent(); // 自动接下一个事件
    }

    public void ShowNodeSummary(string title, string content)
    {
        SwitchState(UIState.NodeSummary);
        SummaryTitleText.text = title;
        SummaryContentText.text = content;
    }

    void OnClickNextNode()
    {
        GameManager.Instance.GoToNextNode();
    }

    public void UpdateResourceDisplay()
    {
        if (ResourceManager.Instance != null && ResourceInfoText != null)
        {
            ResourceInfoText.text = $"粮:{ResourceManager.Instance.Grain} 信:{ResourceManager.Instance.Belief}";
        }
    }
    
    public void UpdatePlaceName(string name)
    {
        if(PlaceText != null) PlaceText.text = name;
    }
}
