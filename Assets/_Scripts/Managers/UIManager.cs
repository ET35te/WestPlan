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
            if(NewBattleManager.Instance != null) NewBattleManager.Instance.StartBattle(currentEvent);
        }
        else
        {
            SwitchState(UIState.Gameplay);
            EventTitleText.text = currentEvent.Title;
            ContextText.text = currentEvent.Context;
            
            // 设置按钮文字
            var txtA = ButtonA.GetComponentInChildren<TMP_Text>();
            if(txtA) txtA.text = currentEvent.OptA_Text;
            
            var txtB = ButtonB.GetComponentInChildren<TMP_Text>();
            if(txtB) txtB.text = currentEvent.OptB_Text;

            // --- 📝 周二任务：选项B 条件检查 ---
            CheckOptionCondition(ButtonB, currentEvent.OptB_Condition);
        }
    }

    // --- 核心：条件解析逻辑 ---
    void CheckOptionCondition(Button btn, string conditionStr)
    {
        // 默认可用
        btn.interactable = true;
        
        // 如果条件为空，直接返回
        if (string.IsNullOrEmpty(conditionStr)) return;

        // 解析 "104:500" -> ID 104, 阈值 500
        string[] parts = conditionStr.Split(':');
        if (parts.Length == 2)
        {
            int resID = int.Parse(parts[0]);
            int threshold = int.Parse(parts[1]);
            int currentVal = ResourceManager.Instance.GetResourceValue(resID);

            if (currentVal < threshold)
            {
                // 条件不满足：置灰禁用
                btn.interactable = false;
                // 可选：在按钮文字上加红色提示
                var txt = btn.GetComponentInChildren<TMP_Text>();
                txt.text += $"\n<color=red>(需 {GameManager.Instance.GetResName(resID)} {threshold})</color>";
            }
        }
    }

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

    void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        // ShowNextEvent(); // 暂时不连续弹，等时间走完
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