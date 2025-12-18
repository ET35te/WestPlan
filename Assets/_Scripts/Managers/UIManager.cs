using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 必须引用
using TMPro;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum UIState { MainMenu, Gameplay, Result, Achievement, Battle, NodeSummary, Ending }
    
    // ... (保留你之前所有的 Header 和 变量) ...
    [Header("--- 层级 1 ---")]
    public GameObject MainMenuPanel;
    public GameObject GameplayPanel;
    public GameObject ResultPanel;
    public GameObject AchievementPanel;
    public GameObject NodeSummaryPanel;
    public GameObject BattlePanel;

    [Header("--- 层级 2 & 3 ---")]
    public GameObject HUDLayer;         
    public GameObject EndingLayer;      

    [Header("--- 组件 ---")]
    public TMP_Text TitleText;          
    public Button StartBtn;
    public Button ContinueBtn; // NEW! 新增“继续游戏”按钮 (记得去UI里加一个)
    public Button QuitBtn;              
    public Button AchievementBtn;       

    public TMP_Text EventTitleText;     
    public TMP_Text PlaceText;          
    public TMP_Text ContextText;        
    public Button ButtonA;              
    public Button ButtonB;              

    public TMP_Text ResultText;         
    public Button ConfirmResultBtn;     

    public TMP_Text SummaryTitleText;   
    public TMP_Text SummaryContentText; 
    public Button ToBeContinueBtn;      

    public Transform CardContainer;     
    public Button ConfirmBattleBtn;     

    public TMP_Text ScrollingText;      

    public Button GlobalQuitToTitleBtn; 
    public TMP_Text ResourceInfoText;
    
    public GameObject EventWindow; // 记得这里

    private DataManager.EventData currentEvent;
    private UIState currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; } 
    }

    void Start()
    {
        StartBtn.onClick.AddListener(() => GameManager.Instance.StartNewGame());
        
        // 如果有继续按钮，绑定它
        if(ContinueBtn != null) 
        {
            ContinueBtn.onClick.AddListener(() => GameManager.Instance.LoadGame());
            // 如果没存档，隐藏继续按钮
            if (!PlayerPrefs.HasKey("HasSave")) ContinueBtn.gameObject.SetActive(false);
        }

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
            case UIState.Gameplay: GameplayPanel.SetActive(true); if(EventWindow) EventWindow.SetActive(true); break;
            case UIState.Result: ResultPanel.SetActive(true); break;
            case UIState.Achievement: AchievementPanel.SetActive(true); break;
            case UIState.NodeSummary: NodeSummaryPanel.SetActive(true); break;
            case UIState.Battle: BattlePanel.SetActive(true); break;
            case UIState.Ending: /* 结局逻辑 */ break;
        }
    }

    public void ShowNextEvent()
    {
        currentEvent = DataManager.Instance.GetRandomEvent();
        if (currentEvent == null) return;

        if (currentEvent.IsPeaceful == false)
        {
            SwitchState(UIState.Battle);
            if(BattleManager.Instance != null) BattleManager.Instance.StartBattle(currentEvent);
        }
        else
        {
            SwitchState(UIState.Gameplay);
            EventTitleText.text = currentEvent.Title;
            ContextText.text = currentEvent.Context;
            
            // 1. 设置按钮 A (默认总是可选)
            var txtA = ButtonA.GetComponentInChildren<TMP_Text>();
            if(txtA) txtA.text = currentEvent.OptA_Text;
            ButtonA.interactable = true;
            
            // 2. 设置按钮 B (带条件检查)
            var txtB = ButtonB.GetComponentInChildren<TMP_Text>();
            if(txtB) txtB.text = currentEvent.OptB_Text;
            
            // 核心调用：检查条件
            CheckOptionCondition(ButtonB, currentEvent.OptB_Condition);
        }
    }

    // --- 核心逻辑：解析 "102:500" (ID:阈值) ---
    void CheckOptionCondition(Button btn, string conditionStr)
    {
        // 先重置为可用
        btn.interactable = true;
        
        // 如果没有条件，直接返回
        if (string.IsNullOrEmpty(conditionStr) || conditionStr == "0:0") return;

        try 
        {
            string[] parts = conditionStr.Split(':');
            int resID = int.Parse(parts[0]);
            int threshold = int.Parse(parts[1]);
            
            int currentVal = ResourceManager.Instance.GetResourceValue(resID);

            // 如果资源不足
            if (currentVal < threshold)
            {
                btn.interactable = false; // 变灰禁用
                
                // 在按钮文字上加红色提示
                var txt = btn.GetComponentInChildren<TMP_Text>();
                string resName = ResourceManager.Instance.GetResName(resID);
                txt.text += $"\n<color=red><size=80%>(需 {resName} {threshold})</size></color>";
            }
        }
        catch 
        { 
            Debug.LogWarning($"条件解析失败: {conditionStr}"); 
        }
    }
    // --- 核心：条件解析逻辑 ---


    void OnSelectOption(bool isA)
    {
        string resultStr = GameManager.Instance.ResolveEventOption(currentEvent, isA);
        ShowResult(resultStr);
    }

    public void ShowResult(string resultStr)
    {
        if (currentState == UIState.Ending)
        {
            Debug.Log("[UI]Have been check the Panel Entered Ending, Stop the Result");
            return;
        }
        SwitchState(UIState.Result);
        ResultText.text = resultStr;
        UpdateResourceDisplay();
    }


    // --- 修改：点击结果界面的确认按钮后 ---
    void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        
        // 关键改动：不再直接 ShowNextEvent，而是问 GameManager 下一步干嘛
        // (是继续下一个随机事件？还是跳指定事件？还是进结算？)
        GameManager.Instance.CheckGameStateAfterResult(); 
    }

    // --- 新增：显示指定 ID 的事件 ---
    public void ShowSpecificEvent(int eventID)
    {
        // 从所有事件中查找
        currentEvent = DataManager.Instance.AllEvents.Find(e => e.ID == eventID);
        
        if (currentEvent == null) 
        {
            Debug.LogError($"找不到 ID 为 {eventID} 的事件！");
            // 保底：显示个随机的
            ShowNextEvent(); 
            return;
        }

        // 显示逻辑 (复用之前的)
        if (currentEvent.IsPeaceful == false)
        {
            SwitchState(UIState.Battle);
            if(BattleManager.Instance != null) BattleManager.Instance.StartBattle(currentEvent);
        }
        else
        {
            SwitchState(UIState.Gameplay);
            EventTitleText.text = currentEvent.Title;
            ContextText.text = currentEvent.Context;
            
            var txtA = ButtonA.GetComponentInChildren<TMP_Text>();
            if(txtA) txtA.text = currentEvent.OptA_Text;
            
            var txtB = ButtonB.GetComponentInChildren<TMP_Text>();
            if(txtB) txtB.text = currentEvent.OptB_Text;

            CheckOptionCondition(ButtonB, currentEvent.OptB_Condition);
        }
    }
    public void ShowNodeSummary(string title, string content)
    {
        SwitchState(UIState.NodeSummary);
        SummaryTitleText.text = title;
        SummaryContentText.text = content;
    }

    // --- 💀 周日思考：结局显示 ---
    public void ShowEnding(string content)
    {
        SwitchState(UIState.Ending);
        ScrollingText.text = content;
        // 这里可以加一个协程让它滚动
    }

    void OnClickNextNode()
    {
        GameManager.Instance.GoToNextNode();
    }

    public void OnClickReturnToTitle()
    {
        SwitchState(UIState.MainMenu);
    }

    void OnClickQuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void UpdateResourceDisplay()
    {
        // ... (ResourceItem 已经处理了显示，这里可以留空或做其他刷新)
    }
    
    public void UpdatePlaceName(string name)
    {
        if(PlaceText != null) PlaceText.text = name;
    }
}