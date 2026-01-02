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
    // 通用弹窗 (用于战斗开场等)
    // ==============================
    [Header("--- 通用弹窗 ---")]
    public GameObject MessagePanel; 
    public TMP_Text MessageText;    

    // ==============================
    // 状态缓存与外部引用
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
    }

    // 🔥 核心修复：正确的生命周期
    private void OnEnable()
    {
        // 1. 监听场景加载
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 2. 尝试连接战斗管理器 (如果已存在)
        ConnectBattleManager();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DisconnectBattleManager();
    }

    // 辅助方法：连接 BattleManager
    private void ConnectBattleManager()
    {
        if (SceneBattleManager != null)
        {
            SceneBattleManager.OnBattleEnded -= OnBattleVictory; // 防止重复
            SceneBattleManager.OnBattleEnded += OnBattleVictory;
        }
    }

    // 辅助方法：断开 BattleManager
    private void DisconnectBattleManager()
    {
        if (SceneBattleManager != null)
        {
            SceneBattleManager.OnBattleEnded -= OnBattleVictory;
        }
    }

    // ==============================
    // 场景切换回调
    // ==============================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 场景加载: {scene.name}");

        AutoBindUI(); 
        ConnectBattleManager();
        currentEvent = null;

        // --- 🔴 删除或修改这部分判断 ---
        // if (scene.name == "SampleScene") ...
        // else if (scene.name == "MainMenu") ...
        
        // --- ✅ 改为统一逻辑：任何时候加载完，都先进主菜单 ---
        BindCommonButtons();
        
        // 如果是刚刚启动游戏，或者重置回来
        SwitchState(UIState.MainMenu); 

        // ❌ 删掉这行！不要直接开始！
        // ShowNextEvent(); 
    }

    // ==============================
    // ⚔️ 战斗胜利回调 (自动跳转逻辑)
    // ==============================
    private void OnBattleVictory(string resultMsg)
    {
        Debug.Log("UIManager: 收到胜利消息");

        // 1. 显示结算面板
        ShowResult(resultMsg);

        // 2. 告诉 GameManager 增加计数 (可选)
        if (GameManager.Instance) 
        {
             // GameManager.Instance.CurrentEventCount++; 
        }

        // 3. 启动自动跳转协程 (3秒后返回)
        StartCoroutine(AutoQuitBattle(3.0f));
    }

    IEnumerator AutoQuitBattle(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log("UIManager: 自动切换回 Gameplay 状态");
        
        // 4. 切回大地图
        SwitchState(UIState.Gameplay);
        
        // 5. 触发下一步逻辑 (检查是否该换节点了)
        if (GameManager.Instance) 
            GameManager.Instance.CheckGameStateAfterResult();
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
        if (DataManager.Instance == null || DataManager.Instance.AllEvents.Count == 0)
        {
            Debug.LogError("❌ DataManager 缺失或无数据！");
            return;
        }

        currentEvent = DataManager.Instance.GetRandomEvent();
        if (currentEvent == null) return;

        Debug.Log($"✅ 抽中事件: [ID:{currentEvent.ID}] {currentEvent.Title}");

        if (currentEvent.IsPeaceful)
        {
            ShowPeacefulEvent(currentEvent);
        }
        else
        {
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
            // 检查条件
            CheckOptionCondition(ButtonB, evt.OptB_Condition);
        }
    }

    private void EnterBattleLogic(DataManager.EventData evt)
    {
        SwitchState(UIState.Battle);

        int.TryParse(evt.OptA_Res1_Data, out int enemyID);
        
        // 再次确保引用存在
        if (SceneBattleManager == null) SceneBattleManager = FindObjectOfType<BattleManager>();
        // 再次确保订阅 (双重保险)
        ConnectBattleManager();

        if (SceneBattleManager != null && DataManager.Instance != null)
        {
            var enemy = DataManager.Instance.GetEnemyByID(enemyID);
            SceneBattleManager.StartBattle(enemy);
        }
    }

    // ==============================
    // 结果 / 结算 / 弹窗
    // ==============================
    public void ShowResult(string result)
    {
        SwitchState(UIState.Result);
        if (ResultText) ResultText.text = result;
        UpdateResourceDisplay();
        
        // 🔥 新增：启动战利品逐个弹出效果
        StartCoroutine(ShowLootSequence());
    }

    /// <summary>
    /// 战利品逐个弹出效果协程
    /// </summary>
    private IEnumerator ShowLootSequence()
    {
        // 等待一下，让结果面板显示出来
        yield return new WaitForSeconds(0.5f);

        // 🔥 获取战利品图标 (假设在 ResultPanel 中)
        // 命名约定：Loot_Food, Loot_Armor, Loot_XP 等
        
        if (ResultPanel == null)
            yield break;

        Transform resultTransform = ResultPanel.transform;
        
        // 查找战利品图标
        Image[] lootImages = new Image[3];
        string[] lootNames = { "Loot_Food", "Loot_Armor", "Loot_XP" };
        
        for (int i = 0; i < lootNames.Length; i++)
        {
            Transform lootT = FindChild(resultTransform, lootNames[i]);
            if (lootT != null)
            {
                lootImages[i] = lootT.GetComponent<Image>();
                // 初始状态：隐藏且缩放为 0
                if (lootImages[i] != null)
                {
                    lootImages[i].enabled = false;
                    lootT.localScale = Vector3.zero;
                }
            }
        }

        // 逐个显示战利品，间隔 0.3 秒
        for (int i = 0; i < lootImages.Length; i++)
        {
            if (lootImages[i] == null)
                continue;

            Debug.Log($"🎁 显示战利品 {i}: {lootNames[i]}");
            
            // 启用图片
            lootImages[i].enabled = true;
            
            // 弹出动画：从 0 缩放到 1
            Transform lootObj = lootImages[i].transform;
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 缓动：开始快，结束慢 (EaseOutElastic 效果)
                float scale = Mathf.Lerp(0, 1, t);
                lootObj.localScale = new Vector3(scale, scale, 1);
                yield return null;
            }
            
            lootObj.localScale = Vector3.one;
            
            // 两个战利品之间延迟 0.3 秒
            if (i < lootImages.Length - 1)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        Debug.Log("✅ 所有战利品已显示");
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

    // 🔥 通用弹窗方法 (BattleManager 调用)
    public void ShowMessage(string msg)
    {
        if (MessagePanel) 
        {
            MessagePanel.SetActive(true);
            MessagePanel.transform.SetAsLastSibling(); // 确保最前
        }
        if (MessageText) MessageText.text = msg;
    }

    public void HideMessage()
    {
        if (MessagePanel) MessagePanel.SetActive(false);
    }

    // ==============================
    // 交互与工具
    // ==============================
    private void OnSelectOption(bool chooseA)
    {
        if (currentEvent == null || GameManager.Instance == null) return;
        string result = GameManager.Instance.ResolveEventOption(currentEvent, chooseA);
        ShowResult(result);
    }

    private void OnClickNextNode()
    {
        if (GameManager.Instance != null) GameManager.Instance.GoToNextNode();
    }

    private void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        if (GameManager.Instance != null) GameManager.Instance.CheckGameStateAfterResult();
    }

    public void UpdatePlaceName(string place)
    {
        if (PlaceText) PlaceText.text = place;
    }

    public void UpdateResourceDisplay()
    {
        // 留给 ResourceManager 调用
    }

    // 🔥 核心修复：防爆解析 CheckOptionCondition
    private void CheckOptionCondition(Button btn, string cond)
    {
        btn.interactable = true;
        if (string.IsNullOrEmpty(cond) || cond == "0:0") return;

        string[] p = cond.Split(':');
        if (p.Length < 2) return;

        // 使用 TryParse 防止格式错误导致的崩溃
        if (int.TryParse(p[0], out int resID) && int.TryParse(p[1], out int need))
        {
            if (ResourceManager.Instance != null &&
                ResourceManager.Instance.GetResourceValue(resID) < need)
            {
                btn.interactable = false;
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ 忽略错误条件: '{cond}'");
        }
    }

    // ==============================
    // 自动绑定系统
    // ==============================
    private void AutoBindUI()
    {
        SceneBattleManager = FindObjectOfType<BattleManager>();
        
        Transform canvas = GameObject.Find("Canvas")?.transform;
        if (!canvas) return;

        // --- 面板绑定 ---
        MainMenuPanel = Find(canvas, "MainMenu_Panel");
        GameplayPanel = Find(canvas, "Gameplay_Panel");
        ResultPanel = Find(canvas, "Result_Panel");
        AchievementPanel = Find(canvas, "Achievement_Panel");
        NodeSummaryPanel = Find(canvas, "NodeSummary_Panel");
        BattlePanel = Find(canvas, "Battle_Panel");
        EventWindow = Find(canvas, "Event_Window");

        HUDLayer = Find(canvas, "Layer_2_HUD");
        EndingLayer = Find(canvas, "Layer_3_Ending");

        // --- 文本绑定 ---
        EventTitleText = FindText(canvas, "Event_Title");
        ContextText = FindText(canvas, "Event_Context");
        PlaceText = FindText(canvas, "Place_Title_Text");

        ResultText = FindText(canvas, "Result_Text");
        SummaryTitleText = FindText(canvas, "Summary_Title");
        SummaryContentText = FindText(canvas, "Summary_Content");
        ScrollingText = FindText(canvas, "Scrolling_Poem");

        // --- 按钮绑定 ---
        ButtonA = FindButton(canvas, "OptionA_Btn");
        ButtonB = FindButton(canvas, "OptionB_Btn");
        ConfirmResultBtn = FindButton(canvas, "Confirm_Result_Btn");
        ToBeContinueBtn = FindButton(canvas, "ToBeContinue_Btn");
        GlobalQuitToTitleBtn = FindButton(canvas, "QuitToTitle_Btn");

        // 🔥 新增：绑定主菜单的开始与退出按钮
        // 请确保 Unity 里按钮的名字叫 "Start_Btn" 和 "Quit_Btn"
        StartBtn = FindButton(canvas, "Start_Btn"); 
        QuitBtn = FindButton(canvas, "Quit_Btn");   
    }

    private void BindCommonButtons()
    {
        // --- 游戏内按钮 ---
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
                SwitchState(UIState.MainMenu); 
            });
        }

        // 🔥 主菜单按钮逻辑
        if (StartBtn)
        {
            StartBtn.onClick.RemoveAllListeners();
            StartBtn.onClick.AddListener(() => 
            {
                Debug.Log("UI: 点击开始游戏");
                if (GameManager.Instance) GameManager.Instance.StartNewGame();
                SwitchState(UIState.Gameplay);
                ShowNextEvent();
            });
        }

        // 🔥 新增：Continue 按钮逻辑 (这是缺失的!)
        if (ContinueBtn)
        {
            ContinueBtn.onClick.RemoveAllListeners();
            
            // 检查是否有存档
            bool hasSave = PlayerPrefs.GetInt("HasSave", 0) == 1;
            
            if (hasSave)
            {
                ContinueBtn.interactable = true;
                ContinueBtn.onClick.AddListener(() =>
                {
                    Debug.Log("UI: 点击继续游戏 - 加载存档");
                    if (GameManager.Instance)
                    {
                        GameManager.Instance.LoadGame();
                    }
                    SwitchState(UIState.Gameplay);
                    ShowNextEvent();
                });
            }
            else
            {
                // 没有存档时，禁用按钮
                ContinueBtn.interactable = false;
                Debug.Log("UI: 没有存档，Continue 按钮已禁用");
            }
        }

        if (QuitBtn)
        {
            QuitBtn.onClick.RemoveAllListeners();
            QuitBtn.onClick.AddListener(() => 
            {
                Debug.Log("UI: 退出游戏");
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            });
        }
    }

    // --- 查找工具 ---
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