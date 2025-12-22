using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum UIState { MainMenu, Gameplay, Result, Achievement, Battle, NodeSummary, Ending }
    
    // ... (保留你之前所有的 Header 和变量) ...
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
    public Button ContinueBtn; 
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
    
    public GameObject EventWindow; 

    private DataManager.EventData currentEvent;
    private UIState currentState;
    public BattleManager SceneBattleManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; } 
    }

    void Start()
    {
        // 1. 通用按钮绑定
        if (QuitBtn != null) QuitBtn.onClick.AddListener(OnClickQuitGame);
        if (GlobalQuitToTitleBtn != null) GlobalQuitToTitleBtn.onClick.AddListener(OnClickReturnToTitle);
        if (AchievementBtn != null) AchievementBtn.onClick.AddListener(() => SwitchState(UIState.Achievement));

        // 2. 场景逻辑分流
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "MainMenu") 
        {
            if (StartBtn != null)
            {
                StartBtn.onClick.RemoveAllListeners();
                StartBtn.onClick.AddListener(() => 
                {
                    GameManager.Instance.ResetDataOnly(); 
                    SceneManager.LoadScene("SampleScene"); // ⚠️ 确保场景名正确
                });
            }

            if (ContinueBtn != null)
            {
                ContinueBtn.onClick.RemoveAllListeners();
                ContinueBtn.onClick.AddListener(() => 
                {
                    GameManager.Instance.LoadGame(); 
                    SceneManager.LoadScene("SampleScene");
                });
                if (!PlayerPrefs.HasKey("HasSave")) ContinueBtn.gameObject.SetActive(false);
            }

            SwitchState(UIState.MainMenu);
        }
        else 
        {
            if (ButtonA != null) ButtonA.onClick.AddListener(() => OnSelectOption(true));
            if (ButtonB != null) ButtonB.onClick.AddListener(() => OnSelectOption(false));
            if (ConfirmResultBtn != null) ConfirmResultBtn.onClick.AddListener(ReturnToGameplay);
            if (ToBeContinueBtn != null) ToBeContinueBtn.onClick.AddListener(OnClickNextNode);

            if (HUDLayer != null) HUDLayer.SetActive(true);

            if (GameManager.Instance != null)
            {
                UpdatePlaceName(GameManager.Instance.GetCurrentNodeName());
                UpdateResourceDisplay();
                if (currentEvent == null) ShowNextEvent();
            }
        }
    }

    public void SwitchState(UIState newState)
    {
        currentState = newState;

        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
        if (GameplayPanel != null) GameplayPanel.SetActive(false);
        if (ResultPanel != null) ResultPanel.SetActive(false);
        if (AchievementPanel != null) AchievementPanel.SetActive(false);
        if (NodeSummaryPanel != null) NodeSummaryPanel.SetActive(false);
        if (BattlePanel != null) BattlePanel.SetActive(false);

        bool showHUD = (newState != UIState.MainMenu && newState != UIState.Ending);
        if (HUDLayer != null) HUDLayer.SetActive(showHUD);
        if (EndingLayer != null) EndingLayer.SetActive(newState == UIState.Ending);

        switch (newState)
        {
            case UIState.MainMenu: 
                if (MainMenuPanel != null) MainMenuPanel.SetActive(true); 
                break;
            case UIState.Gameplay: 
                if (GameplayPanel != null) 
                {
                    GameplayPanel.SetActive(true); 
                    if(EventWindow != null) EventWindow.SetActive(true); 
                }
                break;
            case UIState.Result: 
                if (ResultPanel != null) ResultPanel.SetActive(true); 
                break;
            case UIState.Achievement: 
                if (AchievementPanel != null) AchievementPanel.SetActive(true); 
                break;
            case UIState.NodeSummary: 
                if (NodeSummaryPanel != null) NodeSummaryPanel.SetActive(true); 
                break;
            case UIState.Battle: 
                if (BattlePanel != null) BattlePanel.SetActive(true); 
                break;
        }
    }

    public void ShowNextEvent()
    {
        if (DataManager.Instance == null) return;
        currentEvent = DataManager.Instance.GetRandomEvent();
        if (currentEvent == null) return;

        if (currentEvent.IsPeaceful == false)
        {
            // ⚔️ 进入战斗分支
            EnterBattleLogic(currentEvent);
        }
        else
        {
            // 🕊️ 进入剧情分支
            SwitchState(UIState.Gameplay);
            if (EventTitleText != null) EventTitleText.text = currentEvent.Title;
            if (ContextText != null) ContextText.text = currentEvent.Context;

            if (ButtonA != null)
            {
                var txtA = ButtonA.GetComponentInChildren<TMP_Text>();
                if (txtA != null) txtA.text = currentEvent.OptA_Text;
                ButtonA.interactable = true;
            }

            if (ButtonB != null)
            {
                var txtB = ButtonB.GetComponentInChildren<TMP_Text>();
                if (txtB != null) txtB.text = currentEvent.OptB_Text;
                CheckOptionCondition(ButtonB, currentEvent.OptB_Condition);
            }
        }
    }

    // 🔥【修复重点】统一处理进入战斗的逻辑
    private void EnterBattleLogic(DataManager.EventData evtData)
    {
        if (SceneBattleManager == null)
        {
            Debug.LogError("❌ UIManager: SceneBattleManager 没拖！");
            return;
        }

        SwitchState(UIState.Battle);

        // 1. 尝试解析敌人ID (假设存在 OptA_Res1_Data 字段里)
        int enemyID = 0;
        int.TryParse(evtData.OptA_Res1_Data, out enemyID);

        // 2. 查找敌人数据
        DataManager.EnemyData enemy = DataManager.Instance.GetEnemyByID(enemyID);

        // 3. 开启战斗 (传 EnemyData)
        SceneBattleManager.StartBattle(enemy);
    }

    public void ShowSpecificEvent(int eventID)
    {
        currentEvent = DataManager.Instance.AllEvents.Find(e => e.ID == eventID);
        if (currentEvent == null) 
        {
            ShowNextEvent(); 
            return;
        }

        if (currentEvent.IsPeaceful == false)
        {
            EnterBattleLogic(currentEvent); // 复用修复后的逻辑
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

    void CheckOptionCondition(Button btn, string conditionStr)
    {
        btn.interactable = true;
        if (string.IsNullOrEmpty(conditionStr) || conditionStr == "0:0") return;

        try 
        {
            string[] parts = conditionStr.Split(':');
            int resID = int.Parse(parts[0]);
            int threshold = int.Parse(parts[1]);
            int currentVal = ResourceManager.Instance.GetResourceValue(resID);

            if (currentVal < threshold)
            {
                btn.interactable = false;
                var txt = btn.GetComponentInChildren<TMP_Text>();
                string resName = ResourceManager.Instance.GetResName(resID);
                txt.text += $"\n<color=red><size=80%>(需 {resName} {threshold})</size></color>";
            }
        }
        catch { }
    }

    void OnSelectOption(bool isA)
    {
        if (currentEvent == null || GameManager.Instance == null) return;
        string resultStr = GameManager.Instance.ResolveEventOption(currentEvent, isA);
        ShowResult(resultStr);
    }

    public void ShowResult(string resultStr)
    {
        if (currentState == UIState.Ending) return;
        SwitchState(UIState.Result);
        if(ResultText != null) ResultText.text = resultStr;
        UpdateResourceDisplay();
    }

    void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        GameManager.Instance.CheckGameStateAfterResult(); 
    }

    public void ShowNodeSummary(string title, string content)
    {
        SwitchState(UIState.NodeSummary);
        if(SummaryTitleText != null) SummaryTitleText.text = title;
        if(SummaryContentText != null) SummaryContentText.text = content;
    }

    public void ShowEnding(string content)
    {
        SwitchState(UIState.Ending);
        if(ScrollingText != null) ScrollingText.text = content;
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
        // 资源刷新逻辑
    }
    
    public void UpdatePlaceName(string name)
    {
        if(PlaceText != null) PlaceText.text = name;
    }
    // --- 补回缺失的随机事件获取方法 ---
    
}