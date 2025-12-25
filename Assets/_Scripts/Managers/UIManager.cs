using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // --- 状态定义 ---
    public enum UIState
    {
        MainMenu,    // 主菜单
        Gameplay,    // 探索/事件界面
        Battle,      // 战斗界面
        Result,      // 结算界面
        NodeSummary, // 节点过场 (每月总结)
        Ending,      // 结局界面
        Achievement  // 成就 (可选)
    }

    // ==========================================
    // 🖱️ 手动引用区 (请在 Inspector 拖拽)
    // ==========================================

    [Header("--- 面板容器 (Panels) ---")]
    public GameObject MainMenuPanel;
    public GameObject GameplayPanel;
    public GameObject BattlePanel;
    public GameObject ResultPanel;
    public GameObject NodeSummaryPanel;
    public GameObject AchievementPanel; // 如果有的话
    public GameObject EventWindow;      // 事件弹窗 (通常在 GameplayPanel 里)
    public GameObject EndingLayer;      // 结局遮罩

    [Header("--- 主菜单按钮 ---")]
    public Button MmStartBtn;  // 开始游戏
    public Button MmQuitBtn;   // 退出游戏

    [Header("--- 游戏内按钮 ---")]
    public Button ButtonA;            // 选项 A
    public Button ButtonB;            // 选项 B
    public Button ConfirmResultBtn;   // 结果确认
    public Button ToBeContinueBtn;    // 节点结算确认 (前往下一站)
    public Button GlobalQuitToTitleBtn; // 返回主菜单 (右上角那个)

    [Header("--- 文本组件 ---")]
    public TMP_Text PlaceText;        // 地点名 (左上角)
    public TMP_Text EventTitleText;   // 事件标题
    public TMP_Text ContextText;      // 事件正文
    public TMP_Text ResultText;       // 结果描述
    public TMP_Text SummaryTitleText; // 节点总结标题
    public TMP_Text SummaryContentText; // 节点总结内容
    public TMP_Text ScrollingText;    // 结局滚动字幕

    [Header("--- 外部引用 ---")]
    // 直接把场景里的 BattleManager 拖进来
    public BattleManager SceneBattleManager;

    // --- 内部变量 ---
    private DataManager.EventData currentEvent;

    // ==========================================
    // 🚀 初始化
    // ==========================================

    void Awake()
    {
        // 单场景单例模式 (不需要 DontDestroyOnLoad)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 1. 绑定主菜单按钮
        if (MmStartBtn)
        {
            MmStartBtn.onClick.RemoveAllListeners();
            MmStartBtn.onClick.AddListener(OnStartGameClicked);
        }
        if (MmQuitBtn)
        {
            MmQuitBtn.onClick.RemoveAllListeners();
            MmQuitBtn.onClick.AddListener(() => Application.Quit());
        }

        // 2. 绑定通用的游戏内按钮 (返回主菜单、确认等)
        BindCommonButtons();

        // 3. 游戏启动，进入主菜单状态
        SwitchState(UIState.MainMenu);
    }

    // ==========================================
    // 🔄 状态切换
    // ==========================================

    public void SwitchState(UIState newState)
    {
        // 1. 暴力关闭所有面板 (防穿帮)
        if (MainMenuPanel) MainMenuPanel.SetActive(false);
        if (GameplayPanel) GameplayPanel.SetActive(false);
        if (BattlePanel) BattlePanel.SetActive(false);
        if (ResultPanel) ResultPanel.SetActive(false);
        if (NodeSummaryPanel) NodeSummaryPanel.SetActive(false);
        if (AchievementPanel) AchievementPanel.SetActive(false);
        if (EndingLayer) EndingLayer.SetActive(false);

        // 2. 只打开当前需要的
        switch (newState)
        {
            case UIState.MainMenu:
                if (MainMenuPanel) MainMenuPanel.SetActive(true);
                break;

            case UIState.Gameplay:
                if (GameplayPanel) GameplayPanel.SetActive(true);
                if (EventWindow) EventWindow.SetActive(true);
                break;

            case UIState.Battle:
                if (BattlePanel) BattlePanel.SetActive(true);
                break;

            case UIState.Result:
                if (ResultPanel) ResultPanel.SetActive(true);
                break;

            case UIState.NodeSummary:
                if (NodeSummaryPanel) NodeSummaryPanel.SetActive(true);
                break;

            case UIState.Ending:
                if (EndingLayer) EndingLayer.SetActive(true);
                break;
        }
    }

    // ==========================================
    // 🎮 核心流程控制
    // ==========================================

    // 点击“开始游戏”
    public void OnStartGameClicked()
    {
        Debug.Log("🚀 开始新游戏...");

        // 1. 通知 GameManager 重置所有数据
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }

        // 2. 切换到游戏界面
        SwitchState(UIState.Gameplay);

        // 3. 立即触发第一个事件
        ShowNextEvent();
    }

    // 显示下一个事件
    public void ShowNextEvent()
    {
        if (DataManager.Instance == null) return;

        // 获取随机事件
        currentEvent = DataManager.Instance.GetRandomEvent();
        HandleEventLogic(currentEvent);
    }

    // 显示特定 ID 的事件 (剧情跳转用)
    public void ShowSpecificEvent(int id)
    {
        if (DataManager.Instance == null) return;

        currentEvent = DataManager.Instance.AllEvents.Find(e => e.ID == id);
        HandleEventLogic(currentEvent);
    }

    // 内部逻辑分流
    private void HandleEventLogic(DataManager.EventData evt)
    {
        if (evt == null) return;

        if (evt.IsPeaceful)
        {
            ShowPeacefulEvent(evt);
        }
        else
        {
            EnterBattleLogic(evt);
        }
    }

    // --- 和平事件显示 ---
    private void ShowPeacefulEvent(DataManager.EventData evt)
    {
        SwitchState(UIState.Gameplay);

        // 更新文本
        if (EventTitleText) EventTitleText.text = evt.Title;
        if (ContextText) ContextText.text = evt.Context;

        // 更新按钮 A
        if (ButtonA)
        {
            ButtonA.interactable = true;
            var t = ButtonA.GetComponentInChildren<TMP_Text>();
            if (t) t.text = evt.OptA_Text;

            // 重新绑定点击事件 (防止点一次触发多次)
            ButtonA.onClick.RemoveAllListeners();
            ButtonA.onClick.AddListener(() => OnSelectOption(true));
        }

        // 更新按钮 B
        if (ButtonB)
        {
            var t = ButtonB.GetComponentInChildren<TMP_Text>();
            if (t) t.text = evt.OptB_Text;

            // 检查条件 (如果不满足条件，按钮变灰)
            CheckOptionCondition(ButtonB, evt.OptB_Condition);

            ButtonB.onClick.RemoveAllListeners();
            ButtonB.onClick.AddListener(() => OnSelectOption(false));
        }
    }

    // --- 战斗逻辑 ---
    private void EnterBattleLogic(DataManager.EventData evt)
    {
        SwitchState(UIState.Battle);

        // 解析敌人 ID (存在 OptA_Res1_Data 里)
        int.TryParse(evt.OptA_Res1_Data, out int enemyID);

        // 启动战斗
        if (SceneBattleManager != null && DataManager.Instance != null)
        {
            var enemy = DataManager.Instance.GetEnemyByID(enemyID);
            SceneBattleManager.StartBattle(enemy);
        }
        else
        {
            Debug.LogError("❌ 无法进入战斗：BattleManager 或 DataManager 缺失！");
        }
    }

    // ==========================================
    // 📝 结果与过场
    // ==========================================

    // 显示选项结果
    public void ShowResult(string result)
    {
        SwitchState(UIState.Result);
        if (ResultText) ResultText.text = result;
    }

    // 显示节点总结 (每月结算)
    public void ShowNodeSummary(string title, string content)
    {
        SwitchState(UIState.NodeSummary);
        if (SummaryTitleText) SummaryTitleText.text = title;
        if (SummaryContentText) SummaryContentText.text = content;
    }

    // 显示结局
    public void ShowEnding(string content)
    {
        SwitchState(UIState.Ending);
        if (ScrollingText) ScrollingText.text = content;
    }

    // 更新地点名字
    public void UpdatePlaceName(string place)
    {
        if (PlaceText) PlaceText.text = place;
    }

    // 更新资源栏 (留空，资源管理通常由 ResourceManager 直接更新)
    public void UpdateResourceDisplay() { }

    // ==========================================
    // 🔧 辅助方法
    // ==========================================

    // 处理选项点击
    private void OnSelectOption(bool chooseA)
    {
        if (currentEvent != null && GameManager.Instance != null)
        {
            string result = GameManager.Instance.ResolveEventOption(currentEvent, chooseA);
            ShowResult(result);
        }
    }

    // 绑定通用按钮 (退出、确认等)
    private void BindCommonButtons()
    {
        // 结果界面的确认 -> 返回游戏逻辑
        if (ConfirmResultBtn)
        {
            ConfirmResultBtn.onClick.RemoveAllListeners();
            ConfirmResultBtn.onClick.AddListener(() =>
            {
                SwitchState(UIState.Gameplay);
                if (GameManager.Instance) GameManager.Instance.CheckGameStateAfterResult();
            });
        }

        // 节点总结界面的确认 -> 前往下一站
        if (ToBeContinueBtn)
        {
            ToBeContinueBtn.onClick.RemoveAllListeners();
            ToBeContinueBtn.onClick.AddListener(() =>
            {
                if (GameManager.Instance) GameManager.Instance.GoToNextNode();
            });
        }

        // 全局“返回标题”按钮 (右上角)
        if (GlobalQuitToTitleBtn)
        {
            GlobalQuitToTitleBtn.onClick.RemoveAllListeners();
            GlobalQuitToTitleBtn.onClick.AddListener(() =>
            {
                // 返回主菜单状态
                SwitchState(UIState.MainMenu);
            });
        }
    }

    // 检查按钮条件 (例如：需要 100 金钱才能点击)
    private void CheckOptionCondition(Button btn, string cond)
    {
        btn.interactable = true;
        if (string.IsNullOrEmpty(cond) || cond == "0:0") return;

        string[] p = cond.Split(':');
        if (p.Length < 2) return;

        int resID = int.Parse(p[0]);
        int need = int.Parse(p[1]);

        if (ResourceManager.Instance != null &&
            ResourceManager.Instance.GetResourceValue(resID) < need)
        {
            btn.interactable = false;
        }
    }
}