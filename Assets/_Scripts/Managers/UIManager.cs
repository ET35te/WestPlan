using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ==============================
    // UI 状态定义
    // ==============================
    public enum UIState
    {
        MainMenu,
        Gameplay,
        Result,
        Achievement,
        Battle,
        NodeSummary,
        Ending
    }

    // ==============================
    // 面板引用
    // ==============================
    [Header("--- Panels ---")]
    public GameObject MainMenuPanel;
    public GameObject GameplayPanel;
    public GameObject ResultPanel;
    public GameObject AchievementPanel;
    public GameObject NodeSummaryPanel;
    public GameObject BattlePanel;
    public GameObject EventWindow;

    [Header("--- Layers ---")]
    public GameObject HUDLayer;
    public GameObject EndingLayer;

    // ==============================
    // UI 元素
    // ==============================
    [Header("--- Buttons ---")]
    public Button StartBtn;
    public Button ContinueBtn;
    public Button QuitBtn;
    public Button AchievementBtn;

    public Button ButtonA;
    public Button ButtonB;
    public Button ConfirmResultBtn;
    public Button ToBeContinueBtn;
    public Button GlobalQuitToTitleBtn;

    [Header("--- Text ---")]
    public TMP_Text EventTitleText;
    public TMP_Text ContextText;
    public TMP_Text PlaceText;

    public TMP_Text ResultText;
    public TMP_Text SummaryTitleText;
    public TMP_Text SummaryContentText;
    public TMP_Text ScrollingText;

    // ==============================
    // 状态缓存
    // ==============================
    private UIState currentState;
    private DataManager.EventData currentEvent;

    public BattleManager SceneBattleManager;

    // ==============================
    // 生命周期
    // ==============================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AutoBindUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        void Start()
         {
    // 监听战斗管理器的消息
        if (SceneBattleManager != null)
        {
        SceneBattleManager.OnBattleEnded += ShowResult; // 只要它喊结束，我就执行 ShowResult
        }
        }

// 记得在销毁时拔掉插头（防止报错）
    void OnDestroy() 
    {
    if (SceneBattleManager != null) {
       SceneBattleManager.OnBattleEnded -= ShowResult;
    }
    }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ==============================
    // 场景切换回调（关键）
    // ==============================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 场景加载: {scene.name}");

        AutoBindUI();

        // ⭐ 核心修复点：场景切换时清空事件缓存
        currentEvent = null;

        if (scene.name == "SampleScene")
        {
            BindCommonButtons();

            SwitchState(UIState.Gameplay);

            if (GameManager.Instance != null)
            {
                UpdatePlaceName(GameManager.Instance.GetCurrentNodeName());
                UpdateResourceDisplay();
            }

            // ⭐ 不做任何条件判断，直接触发事件
            ShowNextEvent();
        }
        else if (scene.name == "MainMenu")
        {
            BindCommonButtons();
            SwitchState(UIState.MainMenu);
        }
    }

    // ==============================
    // UI 状态切换
    // ==============================
    public void SwitchState(UIState newState)
    {
        currentState = newState;

        if (MainMenuPanel) MainMenuPanel.SetActive(false);
        if (GameplayPanel) GameplayPanel.SetActive(false);
        if (ResultPanel) ResultPanel.SetActive(false);
        if (AchievementPanel) AchievementPanel.SetActive(false);
        if (NodeSummaryPanel) NodeSummaryPanel.SetActive(false);
        if (BattlePanel) BattlePanel.SetActive(false);

        if (HUDLayer) HUDLayer.SetActive(newState != UIState.MainMenu && newState != UIState.Ending);
        if (EndingLayer) EndingLayer.SetActive(newState == UIState.Ending);

        switch (newState)
        {
            case UIState.MainMenu:
                if (MainMenuPanel) MainMenuPanel.SetActive(true);
                break;

            case UIState.Gameplay:
                if (GameplayPanel) GameplayPanel.SetActive(true);
                if (EventWindow) EventWindow.SetActive(true);
                break;

            case UIState.Result:
                if (ResultPanel) ResultPanel.SetActive(true);
                break;

            case UIState.Achievement:
                if (AchievementPanel) AchievementPanel.SetActive(true);
                break;

            case UIState.NodeSummary:
                if (NodeSummaryPanel) NodeSummaryPanel.SetActive(true);
                break;

            case UIState.Battle:
                if (BattlePanel) BattlePanel.SetActive(true);
                break;

            case UIState.Ending:
                if (EndingLayer) EndingLayer.SetActive(true);
                break;
        }
    }

    // ==============================
    // 事件流程
    // ==============================
    public void ShowNextEvent()
    {
    Debug.Log("🔍 UIManager: 准备显示下一个事件...");

    // 1. 检查数据源
    if (DataManager.Instance == null)
    {
        Debug.LogError("❌ 【致命】DataManager 实例不存在！请检查是否有物体挂载了 DataManager 脚本！");
        return;
    }

    if (DataManager.Instance.AllEvents.Count == 0)
    {
        Debug.LogError("❌ 【无数据】DataManager 里没有事件数据！请看上面的 DataManager 报错信息。");
        return;
    }

    // 2. 获取事件
    currentEvent = DataManager.Instance.GetRandomEvent();
    if (currentEvent == null)
    {
        Debug.LogError("❌ 【运气极差】GetRandomEvent 返回了 null，这理论上不该发生。");
        return;
    }

    Debug.Log($"✅ 抽中事件: [ID:{currentEvent.ID}] {currentEvent.Title}");

    // 3. 根据类型显示
    if (currentEvent.IsPeaceful)
    {
        Debug.Log("🕊️ 类型：和平事件，正在更新 UI...");
        ShowPeacefulEvent(currentEvent);

        // 🔥 诊断核心：检查 UI 绑定
        if (EventTitleText == null) Debug.LogError("❌ 【UI丢失】EventTitleText 是空的！请检查 Hierarchy 里是否有叫 'Event_Title' 的物体！");
        else Debug.Log($"   -> 标题已设置为: {EventTitleText.text}");

        if (ContextText == null) Debug.LogError("❌ 【UI丢失】ContextText 是空的！请检查 Hierarchy 里是否有叫 'Event_Context' 的物体！");
        
        // 检查面板可见性
        if (GameplayPanel != null && !GameplayPanel.activeSelf) Debug.LogError("⚠️ 【面板被藏】Gameplay_Panel 是关闭状态！");
        if (EventWindow != null && !EventWindow.activeSelf) Debug.LogError("⚠️ 【窗口被藏】Event_Window 是关闭状态！");
    }
    else
    {
        Debug.Log("⚔️ 类型：战斗事件，进入战斗逻辑...");
        EnterBattleLogic(currentEvent);
    }
    }
    public void ShowSpecificEvent(int eventID)
    {
        if (DataManager.Instance == null) return;

        currentEvent = DataManager.Instance.AllEvents.Find(e => e.ID == eventID);
        if (currentEvent == null)
        {
            ShowNextEvent();
            return;
        }

        if (currentEvent.IsPeaceful)
            ShowPeacefulEvent(currentEvent);
        else
            EnterBattleLogic(currentEvent);
    }

    private void ShowPeacefulEvent(DataManager.EventData evt)
    {
        SwitchState(UIState.Gameplay);

        if (EventTitleText) EventTitleText.text = evt.Title;
        if (ContextText) ContextText.text = evt.Context;

        if (ButtonA)
        {
            ButtonA.interactable = true;
            var t = ButtonA.GetComponentInChildren<TMP_Text>();
            if (t) t.text = evt.OptA_Text;
        }

        if (ButtonB)
        {
            var t = ButtonB.GetComponentInChildren<TMP_Text>();
            if (t) t.text = evt.OptB_Text;
            CheckOptionCondition(ButtonB, evt.OptB_Condition);
        }
        Debug.Log($"[UI] Show Event: {evt.Title}");

    }

    private void EnterBattleLogic(DataManager.EventData evt)
    {
        SwitchState(UIState.Battle);

        int.TryParse(evt.OptA_Res1_Data, out int enemyID);
        if (SceneBattleManager == null)
            SceneBattleManager = FindObjectOfType<BattleManager>();

        if (SceneBattleManager != null && DataManager.Instance != null)
        {
            var enemy = DataManager.Instance.GetEnemyByID(enemyID);
            SceneBattleManager.StartBattle(enemy);
        }
    }

    // ==============================
    // 结果 / 结算
    // ==============================
    public void ShowResult(string result)
    {
        SwitchState(UIState.Result);
        if (ResultText) ResultText.text = result;
        UpdateResourceDisplay();
    }

    public void ShowNodeSummary(string title, string content)
    {
        SwitchState(UIState.NodeSummary);
        if (SummaryTitleText) SummaryTitleText.text = title;
        if (SummaryContentText) SummaryContentText.text = content;
    }

    public void ShowEnding(string content)
    {
        SwitchState(UIState.Ending);
        if (ScrollingText) ScrollingText.text = content;
    }

    // ==============================
    // 交互
    // ==============================
    private void OnSelectOption(bool chooseA)
    {
        if (currentEvent == null || GameManager.Instance == null) return;
        string result = GameManager.Instance.ResolveEventOption(currentEvent, chooseA);
        ShowResult(result);
    }

    private void OnClickNextNode()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToNextNode();
    }

    private void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        if (GameManager.Instance != null)
            GameManager.Instance.CheckGameStateAfterResult();
    }

    // ==============================
    // 工具方法
    // ==============================
    public void UpdatePlaceName(string place)
    {
        if (PlaceText) PlaceText.text = place;
    }

    public void UpdateResourceDisplay()
    {
        // 保留接口，由 ResourceManager 控制
    }

    private void CheckOptionCondition(Button btn, string cond)
    {
        btn.interactable = true;
        if (string.IsNullOrEmpty(cond) || cond == "0:0") return;

        string[] p = cond.Split(':');
        int resID = int.Parse(p[0]);
        int need = int.Parse(p[1]);

        if (ResourceManager.Instance != null &&
            ResourceManager.Instance.GetResourceValue(resID) < need)
        {
            btn.interactable = false;
        }
    }

    // ==============================
    // 自动绑定
    // ==============================
    private void AutoBindUI()
    {
        SceneBattleManager = FindObjectOfType<BattleManager>();
        Transform canvas = GameObject.Find("Canvas")?.transform;
        if (!canvas) return;

        MainMenuPanel = Find(canvas, "MainMenu_Panel");
        GameplayPanel = Find(canvas, "Gameplay_Panel");
        ResultPanel = Find(canvas, "Result_Panel");
        AchievementPanel = Find(canvas, "Achievement_Panel");
        NodeSummaryPanel = Find(canvas, "NodeSummary_Panel");
        BattlePanel = Find(canvas, "Battle_Panel");
        EventWindow = Find(canvas, "Event_Window");

        HUDLayer = Find(canvas, "Layer_2_HUD");
        EndingLayer = Find(canvas, "Layer_3_Ending");

        EventTitleText = FindText(canvas, "Event_Title");
        ContextText = FindText(canvas, "Event_Context");
        PlaceText = FindText(canvas, "Place_Title_Text");

        ResultText = FindText(canvas, "Result_Text");
        SummaryTitleText = FindText(canvas, "Summary_Title");
        SummaryContentText = FindText(canvas, "Summary_Content");
        ScrollingText = FindText(canvas, "Scrolling_Poem");

        ButtonA = FindButton(canvas, "OptionA_Btn");
        ButtonB = FindButton(canvas, "OptionB_Btn");
        ConfirmResultBtn = FindButton(canvas, "Confirm_Result_Btn");
        ToBeContinueBtn = FindButton(canvas, "ToBeContinue_Btn");
        GlobalQuitToTitleBtn = FindButton(canvas, "QuitToTitle_Btn");
    }

    private void BindCommonButtons()
    {
        if (ButtonA)
        {
            ButtonA.onClick.RemoveAllListeners();
            ButtonA.onClick.AddListener(() => OnSelectOption(true));
        }

        if (ButtonB)
        {
            ButtonB.onClick.RemoveAllListeners();
            ButtonB.onClick.AddListener(() => OnSelectOption(false));
        }

        if (ConfirmResultBtn)
        {
            ConfirmResultBtn.onClick.RemoveAllListeners();
            ConfirmResultBtn.onClick.AddListener(ReturnToGameplay);
        }

        if (ToBeContinueBtn)
        {
            ToBeContinueBtn.onClick.RemoveAllListeners();
            ToBeContinueBtn.onClick.AddListener(OnClickNextNode);
        }

        if (GlobalQuitToTitleBtn)
        {
            GlobalQuitToTitleBtn.onClick.RemoveAllListeners();
            GlobalQuitToTitleBtn.onClick.AddListener(() =>
            {
                if (GameManager.Instance) GameManager.Instance.ResetDataOnly();
                SceneManager.LoadScene("MainMenu");
            });
        }
    }

    // ==============================
    // 查找工具
    // ==============================
    private GameObject Find(Transform r, string n)
    {
        var t = FindChild(r, n);
        return t ? t.gameObject : null;
    }

    private Transform FindChild(Transform p, string n)
    {
        if (p.name == n) return p;
        foreach (Transform c in p)
        {
            var r = FindChild(c, n);
            if (r) return r;
        }
        return null;
    }

    private Button FindButton(Transform r, string n)
    {
        var t = FindChild(r, n);
        return t ? t.GetComponent<Button>() : null;
    }

    private TMP_Text FindText(Transform r, string n)
    {
        var t = FindChild(r, n);
        return t ? t.GetComponent<TMP_Text>() : null;
    }
}
