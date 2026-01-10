using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
    
    [Header("--- 战斗专用面板 ---")]
    public GameObject BattleIntroPanel;      // 战斗开始介绍面板
    public TMP_Text BattleIntroText;         // 战斗介绍文本
    public Button BattleIntroFightBtn;       // 战斗按钮
    public Button BattleIntroFleeBtn;        // 逃离按钮
    
    public GameObject BattleResultPanel;     // 战斗结果面板
    public TMP_Text BattleResultText;        // 战斗结果文本
    public Button BattleResultConfirmBtn;    // 确认按钮    

    // ==============================
    // 状态缓存与外部引用
    // ==============================
    private UIState currentState;
    private DataManager.EventData currentEvent;
    public BattleManager SceneBattleManager;

    // 🔴 新增：UI 状态标志，用于键盘输入备用方案
    private bool isStoryPanelActive = false;
    private bool isEventUIActive = false;
    private bool isResultPanelActive = false;

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

    // 🔴 新增：Update 监听键盘输入作为备用方案
    private void Update()
    {
        // 🐛 按 ~ 键切换调试日志显示
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleDebugLogging();
        }

        // 如果故事面板在显示，监听任何键盘/鼠标输入作为"继续"
        if (isStoryPanelActive && Input.anyKeyDown)
        {
            Debug.Log("⌨️ 检测到键盘输入，触发继续按钮");
            isStoryPanelActive = false;
            OnToBeContinueBtnClicked();
        }

        // 如果事件UI在显示，监听数字键 1/2 对应选项A/B
        if (isEventUIActive)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("🔑 检测到按键 '1'，选择选项A");
                if (ButtonA && ButtonA.interactable)
                    ButtonA.onClick.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("🔑 检测到按键 '2'，选择选项B");
                if (ButtonB && ButtonB.interactable)
                    ButtonB.onClick.Invoke();
            }
        }

        // 如果结果面板在显示，监听任何键盘输入
        if (isResultPanelActive && Input.anyKeyDown)
        {
            Debug.Log("⌨️ 检测到键盘输入，触发确认按钮");
            isResultPanelActive = false;
            if (ConfirmResultBtn)
                ConfirmResultBtn.onClick.Invoke();
        }
    }

    private static bool debugLoggingEnabled = false;

    private void ToggleDebugLogging()
    {
        debugLoggingEnabled = !debugLoggingEnabled;
        Debug.Log($"🐛 调试日志 {(debugLoggingEnabled ? "启用 ✅" : "禁用 ❌")}");
    }

    public static bool IsDebugLoggingEnabled => debugLoggingEnabled;

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

        // 🔴 关键修复：强制确保 DataManager 已初始化
        if (DataManager.Instance == null)
        {
            Debug.LogError("❌ 场景加载时 DataManager.Instance 为空！尝试查找现有实例...");
            DataManager dm = FindObjectOfType<DataManager>();
            if (dm == null)
            {
                Debug.LogError("❌ 场景中没有 DataManager，将创建一个！");
                GameObject dmObj = new GameObject("DataManager");
                dm = dmObj.AddComponent<DataManager>();
                Debug.Log("✅ 已创建 DataManager");
            }
        }
        else
        {
            Debug.Log("✅ DataManager 已存在");
        }

        // 等待 DataManager 初始化
        if (!DataManager.Instance.IsReady)
        {
            Debug.LogWarning("⚠️ DataManager 尚未就绪，强制加载数据...");
            // 这里无法直接调用 private 方法，但可以通过反射或其他方式
            // 暂时输出警告
        }

        AutoBindUI(); 
        ConnectBattleManager();
        currentEvent = null;

        // --- 🔴 删除或修改这部分判断 ---
        // if (scene.name == "SampleScene") ...
        // else if (scene.name == "MainMenu") ...
        
        // --- ✅ 改为统一逻辑：任何时候加载完，都先进主菜单 ---
        BindCommonButtons();
        
        // ✅ 【关键修复】场景加载后立即关闭所有弹窗面板（防止残留）
        if (MessagePanel) MessagePanel.SetActive(false);
        if (BattleIntroPanel) BattleIntroPanel.SetActive(false);
        if (BattleResultPanel) BattleResultPanel.SetActive(false);
        
        // 如果是刚刚启动游戏，或者重置回来
        SwitchState(UIState.MainMenu);
        
        // 🔕 移除调试面板与实时日志的自动创建（正式版屏蔽）
        // CreateDebugPanel();
        // CreateOnScreenDebugLog();

        // ❌ 删掉这行！不要直接开始！
        // ShowNextEvent(); 
    }
    
    private static bool debugPanelCreated = false;
    private void CreateDebugPanel()
    {
        if (debugPanelCreated) return;
        
        GameObject debugObj = new GameObject("_UIDebugHelper");
        debugObj.AddComponent<UIDebugHelper>();
        debugPanelCreated = true;
    }

    private static bool onScreenDebugCreated = false;
    private void CreateOnScreenDebugLog()
    {
        if (onScreenDebugCreated) return;
        
        GameObject debugObj = new GameObject("_OnScreenDebugLog");
        debugObj.AddComponent<OnScreenDebugLog>();
        onScreenDebugCreated = true;
        Debug.Log("✅ 实时日志显示面板已创建（屏幕左上角）");
    }

    // ==============================
    // ⚔️ 战斗胜利回调 (自动跳转逻辑)
    // ==============================
    private void OnBattleVictory(string resultMsg)
    {
        Debug.Log("🏆 UIManager: 收到战斗胜利消息");

        // 使用专用战斗结果面板显示结果
        ShowBattleResultPanel(resultMsg, onConfirm: () =>
        {
            // 确认后返回游戏
            StartCoroutine(AutoReturnFromBattle(0.5f));
        });
    }

    IEnumerator AutoReturnFromBattle(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log("⏭️ UIManager: 战斗结束，自动返回剧情...");
        
        // 1. 切回 Gameplay 状态
        SwitchState(UIState.Gameplay);
        
        // 2. 检查是否在v2事件系统中
        if (GameManager.Instance != null)
        {
            // 关键：直接进入 v2 事件结果确认流程
            // 这样会自动跳转到 NextID 指定的下一个事件
            GameManager.Instance.ConfirmEventResult_v2();
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
        
        // ✅ 【关键修复】每次切换状态时都重置战斗面板（防止面板残留显示）
        if (BattleIntroPanel) BattleIntroPanel.SetActive(false);
        if (BattleResultPanel) BattleResultPanel.SetActive(false);
        if (MessagePanel) MessagePanel.SetActive(false);

        if (HUDLayer) HUDLayer.SetActive(newState != UIState.MainMenu && newState != UIState.Ending);
        if (EndingLayer) EndingLayer.SetActive(newState == UIState.Ending);

        switch (newState)
        {
            case UIState.MainMenu:
                if (MainMenuPanel) MainMenuPanel.SetActive(true);
                // 关闭所有键盘监听
                isStoryPanelActive = false;
                isEventUIActive = false;
                isResultPanelActive = false;
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
    // ❌ 旧事件系统已弃用 - 使用新系统 v2 (线性分支)
    // ==============================
    /*
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
    */

    // ❌ 旧系统方法(已弃用)
    /*
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
    */

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
    // 战斗介绍与结果面板
    // ==============================
    /// <summary>
    /// 显示战斗介绍面板（替代 MessagePanel）
    /// 只有在 BattleManager.StartBattle() 中显式调用时才会出现
    /// </summary>
    public void ShowBattleIntroPanel(string reason, System.Action onFight, System.Action onFlee)
    {
        if (BattleIntroPanel == null)
        {
            Debug.LogError("❌ BattleIntroPanel 未绑定！使用通用 MessagePanel 作为备选");
            ShowMessage(reason);
            return;
        }

        Debug.Log("🎭 [ShowBattleIntroPanel] 正在显示战斗介绍面板");
        BattleIntroPanel.SetActive(true);
        BattleIntroPanel.transform.SetAsLastSibling(); // 确保最前

        if (BattleIntroText) BattleIntroText.text = reason;

        if (BattleIntroFightBtn)
        {
            BattleIntroFightBtn.onClick.RemoveAllListeners();
            BattleIntroFightBtn.onClick.AddListener(() =>
            {
                Debug.Log("✅ 玩家选择战斗");
                HideBattleIntroPanel();
                onFight?.Invoke();
            });
        }

        if (BattleIntroFleeBtn)
        {
            BattleIntroFleeBtn.onClick.RemoveAllListeners();
            BattleIntroFleeBtn.onClick.AddListener(() =>
            {
                Debug.Log("🚫 玩家选择逃离");
                HideBattleIntroPanel();
                onFlee?.Invoke();
            });
        }

        Debug.Log("✅ 战斗介绍面板已显示");
    }

    public void HideBattleIntroPanel()
    {
        if (BattleIntroPanel)
        {
            Debug.Log("🔒 [HideBattleIntroPanel] 隐藏战斗介绍面板");
            BattleIntroPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示战斗结果面板（替代通用 ResultPanel）
    /// </summary>
    public void ShowBattleResultPanel(string result, System.Action onConfirm)
    {
        if (BattleResultPanel == null)
        {
            Debug.LogError("❌ BattleResultPanel 未绑定！使用通用 ResultPanel 作为备选");
            ShowResult(result);
            return;
        }

        Debug.Log("🏆 [ShowBattleResultPanel] 正在显示战斗结果面板");
        BattleResultPanel.SetActive(true);
        BattleResultPanel.transform.SetAsLastSibling(); // 确保最前

        if (BattleResultText) BattleResultText.text = result;

        if (BattleResultConfirmBtn)
        {
            BattleResultConfirmBtn.onClick.RemoveAllListeners();
            BattleResultConfirmBtn.onClick.AddListener(() =>
            {
                Debug.Log("✅ 战斗结果确认，关闭面板");
                HideBattleResultPanel();
                onConfirm?.Invoke();
            });
        }

        Debug.Log("✅ 战斗结果面板已显示");
    }

    public void HideBattleResultPanel()
    {
        if (BattleResultPanel)
        {
            Debug.Log("🔒 [HideBattleResultPanel] 隐藏战斗结果面板");
            BattleResultPanel.SetActive(false);
        }
    }
    public void ShowConfirmQuitDialog()
    {
        // 使用通用 MessagePanel 进行简易确认
        ShowMessage("确定要退出到主菜单吗？当前进度将不会保存。");
        // 暂时复用 ToBeContinueBtn 作为“确认退出”按钮
        if (ToBeContinueBtn)
        {
            ToBeContinueBtn.onClick.RemoveAllListeners();
            ToBeContinueBtn.onClick.AddListener(() =>
            {
                HideMessage();
                if (GameManager.Instance) GameManager.Instance.ResetDataOnly();
                SwitchState(UIState.MainMenu);
            });
        }
        // 同时允许玩家点击右上角的全局退出按钮再次关闭面板
    }

    // 交互与工具
    // ==============================
    // ❌ 旧系统方法(已弃用)
    /*
    private void OnSelectOption(bool chooseA)
    {
        if (currentEvent == null || GameManager.Instance == null) return;
        string result = GameManager.Instance.ResolveEventOption(currentEvent, chooseA);
        ShowResult(result);
    }
    */

    private void OnClickNextNode()
    {
        if (GameManager.Instance != null) GameManager.Instance.GoToNextNode();
    }

    // ❌ 旧系统方法(已弃用) - 不再调用CheckGameStateAfterResult
    /*
    private void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        if (GameManager.Instance != null) GameManager.Instance.CheckGameStateAfterResult();
    }
    */

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
        // 🔴 关键修复：确保 Canvas 有 GraphicRaycaster，否则 UI 点击无法工作
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        Debug.Log($"🔍 检查场景中的 Canvas ({allCanvases.Length} 个)...");
        foreach (Canvas canvasItem in allCanvases)
        {
            if (canvasItem.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogWarning($"⚠️ Canvas '{canvasItem.name}' 缺少 GraphicRaycaster，正在自动添加...");
                canvasItem.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"✅ 已为 Canvas '{canvasItem.name}' 添加 GraphicRaycaster");
            }
            else
            {
                Debug.Log($"✅ Canvas '{canvasItem.name}' 已有 GraphicRaycaster");
            }
        }

        // 🔴 关键修复：确保 EventSystem 存在
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ 场景中不存在 EventSystem，正在自动创建...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("✅ 已创建 EventSystem 和 StandaloneInputModule");
        }
        else
        {
            Debug.Log("✅ EventSystem 已存在");
        }

        SceneBattleManager = FindObjectOfType<BattleManager>();
        
        Transform canvasTransform = GameObject.Find("Canvas")?.transform;
        if (!canvasTransform) return;

        // --- 面板绑定 ---
        MainMenuPanel = Find(canvasTransform, "MainMenu_Panel");
        GameplayPanel = Find(canvasTransform, "Gameplay_Panel");
        ResultPanel = Find(canvasTransform, "Result_Panel");
        AchievementPanel = Find(canvasTransform, "Achievement_Panel");
        NodeSummaryPanel = Find(canvasTransform, "NodeSummary_Panel");
        BattlePanel = Find(canvasTransform, "Battle_Panel");
        EventWindow = Find(canvasTransform, "Event_Window");

        HUDLayer = Find(canvasTransform, "Layer_2_HUD");
        EndingLayer = Find(canvasTransform, "Layer_3_Ending");

        // --- 文本绑定 ---
        EventTitleText = FindText(canvasTransform, "Event_Title");
        ContextText = FindText(canvasTransform, "Event_Context");
        PlaceText = FindText(canvasTransform, "Place_Title_Text");

        ResultText = FindText(canvasTransform, "Result_Text");
        SummaryTitleText = FindText(canvasTransform, "Summary_Title");
        SummaryContentText = FindText(canvasTransform, "Summary_Content");
        ScrollingText = FindText(canvasTransform, "Scrolling_Poem");

        // --- 按钮绑定 ---
        ButtonA = FindButton(canvasTransform, "OptionA_Btn");
        ButtonB = FindButton(canvasTransform, "OptionB_Btn");
        ConfirmResultBtn = FindButton(canvasTransform, "Confirm_Result_Btn");
        ToBeContinueBtn = FindButton(canvasTransform, "ToBeContinue_Btn");
        GlobalQuitToTitleBtn = FindButton(canvasTransform, "QuitToTitle_Btn");

        // 🔥 新增：绑定主菜单的开始与退出按钮
        // 请确保 Unity 里按钮的名字叫 "Start_Btn" 和 "Quit_Btn"
        StartBtn = FindButton(canvasTransform, "Start_Btn"); 
        QuitBtn = FindButton(canvasTransform, "Quit_Btn");   
    }

    private void BindCommonButtons()
    {
        // ✅ 新系统按钮绑定（新系统ShowEventUI_v2和ShowEventResult_v2中已内置绑定）
        // 这里保持基础初始化，具体事件处理由各显示函数实现
        
        // 剧情面板按钮 - 由ShowStoryPanel()内置绑定
        if (ToBeContinueBtn)
        {
            ToBeContinueBtn.onClick.RemoveAllListeners();
            // 绑定由 ShowStoryPanel() 内部处理
            
            // 🔴 附加诊断器
            if (ToBeContinueBtn.GetComponent<ButtonClickDebugger>() == null)
            {
                ToBeContinueBtn.gameObject.AddComponent<ButtonClickDebugger>();
                Debug.Log("✅ 已为 ToBeContinueBtn 附加诊断器");
            }
        }
        
        // 事件UI按钮 - 由ShowEventUI_v2()内置绑定
        if (ButtonA) 
        {
            ButtonA.onClick.RemoveAllListeners();
            if (ButtonA.GetComponent<ButtonClickDebugger>() == null)
                ButtonA.gameObject.AddComponent<ButtonClickDebugger>();
        }
        if (ButtonB) 
        {
            ButtonB.onClick.RemoveAllListeners();
            if (ButtonB.GetComponent<ButtonClickDebugger>() == null)
                ButtonB.gameObject.AddComponent<ButtonClickDebugger>();
        }
        
        // 结果确认按钮 - 由ShowEventResult_v2()内置绑定
        if (ConfirmResultBtn) 
        {
            ConfirmResultBtn.onClick.RemoveAllListeners();
            if (ConfirmResultBtn.GetComponent<ButtonClickDebugger>() == null)
                ConfirmResultBtn.gameObject.AddComponent<ButtonClickDebugger>();
        }
        
        if (GlobalQuitToTitleBtn)
        {
            GlobalQuitToTitleBtn.onClick.RemoveAllListeners();
            GlobalQuitToTitleBtn.onClick.AddListener(() =>
            {
                ShowConfirmQuitDialog();
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
                // ❌ 旧系统已弃用：ShowNextEvent();
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
                    // ❌ 旧系统已弃用：ShowNextEvent();
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

    // =========================================================
    // 🔗 新增：v2事件系统UI方法（线性分支）
    // =========================================================

    /// <summary>
    /// 显示剧情面板
    /// </summary>
    public void ShowStoryPanel(DataManager.StoryPanelData panel)
    {
        if (panel == null)
        {
            Debug.LogError("❌ ShowStoryPanel: panel 为空");
            return;
        }

        Debug.Log($"📖 显示剧情面板: Node{panel.NodeID} - {panel.Title}");

        SwitchState(UIState.Gameplay);

        // 显示剧情面板
        if (MessagePanel) 
        {
            MessagePanel.SetActive(true);
            MessagePanel.transform.SetAsLastSibling();  // 确保显示在最上层
            Debug.Log("✅ MessagePanel 已激活并置于最上层");
        }
        else
        {
            Debug.LogError("❌ MessagePanel 为空");
            return;
        }

        // 设置文本内容
        if (MessageText)
        {
            MessageText.text = $"<b>{panel.Title}</b>\n\n{panel.Content}";
            Debug.Log($"✅ 已设置文本: {panel.Title}");
        }
        else
        {
            Debug.LogError("❌ MessageText 为空");
        }

        // 配置"继续"按钮
        if (ToBeContinueBtn)
        {
            Debug.Log("🔧 配置 ToBeContinueBtn 点击事件...");
            
            // 🔴 强制清除并重新配置
            ToBeContinueBtn.onClick.RemoveAllListeners();
            
            // 确保按钮可交互
            ToBeContinueBtn.interactable = true;
            Debug.Log($"✅ ToBeContinueBtn 设置为可交互");
            
            // 直接调用方法而不用Lambda (Lambda可能导致事件丢失)
            ToBeContinueBtn.onClick.AddListener(OnToBeContinueBtnClicked);
            
            Debug.Log("✅ ToBeContinueBtn 点击事件已绑定 (直接方法引用)");
            
            // 诊断信息
            Debug.Log($"📌 Button 组件状态: interactable={ToBeContinueBtn.interactable}, gameObject.active={ToBeContinueBtn.gameObject.activeInHierarchy}");
            if (ToBeContinueBtn.GetComponent<GraphicRaycaster>() == null && ToBeContinueBtn.GetComponentInParent<Canvas>() != null)
            {
                Debug.LogWarning("⚠️ 警告: ToBeContinueBtn 所在 Canvas 可能缺少 GraphicRaycaster 组件!");
            }
        }
        else
        {
            Debug.LogError("❌ ToBeContinueBtn 为空");
        }

        // 🔴 启用键盘输入备用方案
        isStoryPanelActive = true;
        Debug.Log("⌨️ 已启用故事面板键盘监听（按任意键继续）");

        Debug.Log($"📖 剧情面板显示完成");
    }

    /// <summary>
    /// ToBeContinueBtn 点击回调 (独立方法，避免Lambda问题)
    /// </summary>
    private void OnToBeContinueBtnClicked()
    {
        Debug.Log("👆 ============ ToBeContinueBtn 被点击！============");
        Debug.Log($"🕐 时间戳: {Time.time}");
        CloseStoryPanelAndStartEvents();
    }

    /// <summary>
    /// 关闭剧情面板并开始事件链
    /// </summary>
    public void CloseStoryPanelAndStartEvents()
    {
        Debug.Log("📖 关闭剧情面板，启动事件链...");
        
        if (MessagePanel) 
        {
            MessagePanel.SetActive(false);
            Debug.Log("✅ MessagePanel 已关闭");
        }

        // 通知GameManager启动事件链
        if (GameManager.Instance != null)
        {
            int currentNodeID = GameManager.Instance.CurrentNodeIndex;
            Debug.Log($"🎬 获取Node {currentNodeID} 的首个事件...");
            
            DataManager.StoryPanelData panel = DataManager.Instance.GetStoryPanelByNodeID(currentNodeID);
            if (panel != null)
            {
                Debug.Log($"✅ 获取到FirstEventID: {panel.FirstEventID}");
                Debug.Log($"📍 准备显示事件 ID {panel.FirstEventID}...");
                GameManager.Instance.StartNodeEventChain(panel.FirstEventID);
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到Node {currentNodeID} 的剧情面板");
            }
        }
        else
        {
            Debug.LogError("❌ GameManager.Instance 为空");
        }
    }

    /// <summary>
    /// 显示v2版本的事件UI
    /// </summary>
    public void ShowEventUI_v2(DataManager.EventData_v2 evt)
    {
        if (evt == null) 
        {
            Debug.LogError("❌ ShowEventUI_v2: evt 为空!");
            return;
        }

        Debug.Log($"🎬 ============ 显示事件 ID {evt.ID} ============");
        Debug.Log($"   事件: {evt.Title}");
        Debug.Log($"   内容: {evt.Context}");

        SwitchState(UIState.Gameplay);

        // 显示标题和内容
        if (EventTitleText) 
        {
            EventTitleText.text = evt.Title;
            Debug.Log($"✅ 已设置标题");
        }
        if (ContextText) 
        {
            ContextText.text = evt.Context;
            Debug.Log($"✅ 已设置内容");
        }

        // 在文本完全展开前，隐藏选项按钮
        if (ButtonA) ButtonA.gameObject.SetActive(false);
        if (ButtonB) ButtonB.gameObject.SetActive(false);

        // 启动事件文本的逐字展开；玩家点击一次可直接展开全部
        StartCoroutine(RevealEventContextAndEnableOptions(evt));

        // 配置选项A
        if (ButtonA)
        {
            ButtonA.interactable = true;
            var t = ButtonA.GetComponentInChildren<TMP_Text>();
            if (t) t.text = evt.OptA_Text;

            // 检查选项条件
            if (!string.IsNullOrEmpty(evt.Condition_A))
            {
                bool canChooseA = ConditionEvaluator.Evaluate(evt.Condition_A, ResourceManager.Instance);
                ButtonA.interactable = canChooseA;
                Debug.Log($"📌 选项A 条件检查: {evt.Condition_A} => {(canChooseA ? "✅ 符合" : "❌ 不符合")}");
                if (!canChooseA) t.text += " (条件不符)";
            }

            // 移除旧的监听
            ButtonA.onClick.RemoveAllListeners();
            // 添加新的监听
            ButtonA.onClick.AddListener(() => 
            {
                Debug.Log($"👆 选项A 被点击!");
                OnOptionSelected_v2(evt, true);
            });
            Debug.Log($"✅ 选项A 已绑定: {evt.OptA_Text}");
        }

        // 配置选项B
        if (ButtonB)
        {
            ButtonB.interactable = true;
            var t = ButtonB.GetComponentInChildren<TMP_Text>();
            if (t) t.text = evt.OptB_Text;

            // 检查选项条件
            if (!string.IsNullOrEmpty(evt.Condition_B))
            {
                bool canChooseB = ConditionEvaluator.Evaluate(evt.Condition_B, ResourceManager.Instance);
                ButtonB.interactable = canChooseB;
                Debug.Log($"📌 选项B 条件检查: {evt.Condition_B} => {(canChooseB ? "✅ 符合" : "❌ 不符合")}");
                if (!canChooseB) t.text += " (条件不符)";
            }

            // 移除旧的监听
            ButtonB.onClick.RemoveAllListeners();
            // 添加新的监听
            ButtonB.onClick.AddListener(() => 
            {
                Debug.Log($"👆 选项B 被点击!");
                OnOptionSelected_v2(evt, false);
            });
            Debug.Log($"✅ 选项B 已绑定: {evt.OptB_Text}");
        }

        // 🔴 启用事件UI键盘监听
        isEventUIActive = true;
        isStoryPanelActive = false;  // 关闭故事面板监听
        Debug.Log("⌨️ 已启用事件UI键盘监听（按 1 选项A，按 2 选项B）");

        Debug.Log($"✅✅ 事件 UI 显示完成");
    }

    IEnumerator RevealEventContextAndEnableOptions(DataManager.EventData_v2 evt)
    {
        if (ContextText == null) yield break;
        
        string full = evt.Context;
        ContextText.text = "";
        float delay = 0.02f;
        bool fullyRevealed = false;

        for (int i = 0; i < full.Length; i++)
        {
            // 检测鼠标点击或任意键按下，一键全文显示
            if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            {
                ContextText.text = full;
                fullyRevealed = true;
                Debug.Log("✅ 玩家点击/按键，事件文本一键展开");
                break;
            }

            ContextText.text = full.Substring(0, i + 1);
            yield return new WaitForSeconds(delay);
        }

        // 确保文本完全显示
        if (!fullyRevealed)
        {
            ContextText.text = full;
            Debug.Log("✅ 事件文本已完全渐进显示");
        }

        // 等待一帧，确保UI更新完成
        yield return null;

        // 文本已完全展开，显示并启用选项按钮
        if (ButtonA) 
        { 
            ButtonA.gameObject.SetActive(true);
            ButtonA.interactable = true;
            Debug.Log("✅ 选项 A 按钮已启用");
        }
        if (ButtonB) 
        { 
            ButtonB.gameObject.SetActive(true);
            ButtonB.interactable = true;
            Debug.Log("✅ 选项 B 按钮已启用");
        }
    }

    /// <summary>
    /// v2选项被点击
    /// </summary>
    private void OnOptionSelected_v2(DataManager.EventData_v2 evt, bool chooseA)
    {
        Debug.Log($"🔄 ============ 选项被选择 (EventID={evt.ID}) ============");
        Debug.Log($"   选择: {(chooseA ? "选项A" : "选项B")}");
        
        // 检查条件是否真的满足
        string condition = chooseA ? evt.Condition_A : evt.Condition_B;
        if (!string.IsNullOrEmpty(condition) && !ConditionEvaluator.Evaluate(condition, ResourceManager.Instance))
        {
            Debug.LogError($"❌ 条件检查失败: {condition}");
            return;
        }

        Debug.Log($"✅ 条件检查通过，准备禁用按钮...");
        // 禁用选项按钮
        if (ButtonA) 
        {
            ButtonA.interactable = false;
            Debug.Log($"✅ ButtonA 已禁用");
        }
        if (ButtonB) 
        {
            ButtonB.interactable = false;
            Debug.Log($"✅ ButtonB 已禁用");
        }

        Debug.Log($"📍 调用 GameManager.ResolveEventOption_v2()...");
        // 调用 GameManager 处理结果
        GameManager.Instance.ResolveEventOption_v2(evt, chooseA);
        Debug.Log($"📍 ResolveEventOption_v2() 调用完成");
    }

    /// <summary>
    /// 显示事件结果（v2）
    /// </summary>
    public void ShowEventResult_v2(string resultText)
    {
        Debug.Log($"📋 ============ 显示事件结果 ============");
        Debug.Log($"   内容: {resultText}");
        
        SwitchState(UIState.Result);

        if (ResultText) 
        {
            ResultText.text = resultText;
            Debug.Log($"✅ 结果文本已设置");
        }

        // 配置确认按钮
        if (ConfirmResultBtn)
        {
            ConfirmResultBtn.onClick.RemoveAllListeners();
            ConfirmResultBtn.onClick.AddListener(() =>
            {
                Debug.Log($"👆 确认按钮被点击!");
                Debug.Log($"📍 调用 GameManager.ConfirmEventResult_v2()...");
                // 继续到下一个事件或结算
                GameManager.Instance.ConfirmEventResult_v2();
            });
            Debug.Log($"✅ 确认按钮已绑定");
        }

        // 🔴 启用结果面板键盘监听
        isEventUIActive = false;  // 关闭事件UI监听
        isResultPanelActive = true;
        Debug.Log("⌨️ 已启用结果面板键盘监听（按任意键确认）");

        Debug.Log($"📋 事件结果显示完成");
    }

    // =========================================================
    // 🎯 新增：节点分页 UI 系统 v3 (ShowEventPageUI_v3)
    // =========================================================

    /// <summary>
    /// 显示事件分页 UI v3 版本（支持翻页、互斥选择、资源延迟结算）
    /// </summary>
    public void ShowEventPageUI_v3(NodeEventPoolManager eventPoolManager)
    {
        if (eventPoolManager == null)
        {
            Debug.LogError("❌ ShowEventPageUI_v3: eventPoolManager 为空!");
            return;
        }

        SwitchState(UIState.Gameplay);

        var evt = eventPoolManager.GetCurrentEvent();
        if (evt.EventData == null)
        {
            Debug.LogError("❌ 无法获取当前事件");
            return;
        }

        int currentPage = eventPoolManager.GetCurrentPageIndex() + 1;
        int totalPages = eventPoolManager.GetTotalEventCount();

        Debug.Log($"📄 显示事件页面 {currentPage}/{totalPages}: {evt.EventData.Title}");

        // 1. 显示标题和内容
        if (EventTitleText) EventTitleText.text = evt.EventData.Title;
        if (ContextText) ContextText.text = evt.EventData.Context;

        // 2. 更新进度条
        UpdateEventPageProgress(currentPage, totalPages);

        // 3. 配置翻页按钮
        ConfigureNavigationButtons(eventPoolManager);

        // 4. 配置选项
        ConfigureEventOptions_v3(eventPoolManager);

        // 5. 隐藏"确认按钮"，显示"完成事件按钮"（仅在全部完成时启用）
        UpdateCompletionButton(eventPoolManager);
    }

    private void UpdateEventPageProgress(int currentPage, int totalPages)
    {
        var progressText = FindText(canvasTransform, "ProgressText");
        if (progressText != null)
        {
            progressText.text = $"{currentPage}/{totalPages}";
            Debug.Log($"📊 进度条: {currentPage}/{totalPages}");
        }
    }

    private void ConfigureNavigationButtons(NodeEventPoolManager eventPoolManager)
    {
        int currentPage = eventPoolManager.GetCurrentPageIndex();
        int totalPages = eventPoolManager.GetTotalEventCount();

        var prevButton = FindButton(canvasTransform, "PrevButton");
        var nextButton = FindButton(canvasTransform, "NextButton");

        if (prevButton != null)
        {
            bool canGoPrev = currentPage > 0;
            prevButton.interactable = canGoPrev;
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(() => GameManager.Instance.OnEventPagePrevious());
        }

        if (nextButton != null)
        {
            bool canGoNext = currentPage < totalPages - 1;
            nextButton.interactable = canGoNext;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => GameManager.Instance.OnEventPageNext());
        }
    }

    private void ConfigureEventOptions_v3(NodeEventPoolManager eventPoolManager)
    {
        var evt = eventPoolManager.GetCurrentEvent();
        if (evt.EventData == null) return;

        bool isResolved = evt.IsResolved;
        bool chooseA = evt.ChooseA;

        if (ButtonA != null)
            ConfigureOptionButton(ButtonA, evt.EventData.OptA_Text, evt.EventData.OptA_Result_Data, true, isResolved, chooseA);

        if (ButtonB != null)
            ConfigureOptionButton(ButtonB, evt.EventData.OptB_Text, evt.EventData.OptB_Result_Data, false, isResolved, chooseA);
    }

    private void ConfigureOptionButton(Button button, string optText, string resultData, bool isOptionA, bool isResolved, bool currentChooseA)
    {
        var buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText == null) return;

        bool canAfford = CanAffordOption(resultData);
        bool isSelected = isOptionA == currentChooseA;

        // 构建文本（资源标红）
        string display = optText;
        if (!string.IsNullOrEmpty(resultData))
        {
            string resourceDisplay = FormatResourceDisplay(resultData);
            display += $"\n<color=red>{resourceDisplay}</color>";
        }
        buttonText.text = display;
        buttonText.richText = true;

        // 按钮状态
        if (isResolved || !canAfford)
        {
            button.interactable = false;
            button.image.color = Color.gray;
        }
        else
        {
            button.interactable = true;
            button.image.color = isSelected ? new Color(0.7f, 1f, 0.7f, 1f) : Color.white;
        }

        // 绑定事件
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            PlayOptionClickFeedback(button);
            GameManager.Instance.OnEventOptionSelected_v3(isOptionA);
            ShowEventPageUI_v3(NodeEventPoolManager.Instance);
        });
    }

    private bool CanAffordOption(string resultData)
    {
        if (string.IsNullOrEmpty(resultData)) return true;

        foreach (string item in resultData.Split(';'))
        {
            string[] kv = item.Split(':');
            if (kv.Length != 2) continue;

            string resourceName = kv[0].Trim();
            if (!int.TryParse(kv[1].Trim(), out int delta)) continue;

            int current = GetCurrentResourceAmount(resourceName);
            if (current + delta < 0)
                return false;
        }
        return true;
    }

    private int GetCurrentResourceAmount(string resourceName)
    {
        if (ResourceManager.Instance == null) return 0;
        return resourceName switch
        {
            "Food" => ResourceManager.Instance.Grain,
            "Armor" => ResourceManager.Instance.Armor,
            "Belief" => ResourceManager.Instance.Belief,
            _ => 0
        };
    }

    private string FormatResourceDisplay(string resultData)
    {
        if (string.IsNullOrEmpty(resultData)) return "";

        var parts = new System.Collections.Generic.List<string>();
        foreach (string item in resultData.Split(';'))
        {
            string[] kv = item.Split(':');
            if (kv.Length != 2) continue;

            string displayName = kv[0].Trim() switch
            {
                "Food" => "粮食",
                "Armor" => "铠甲",
                "Belief" => "信念",
                var x => x
            };

            parts.Add($"{displayName}:{kv[1].Trim()}");
        }
        return string.Join(" | ", parts);
    }

    private void PlayOptionClickFeedback(Button button)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("UI_Click");

        // 使用动效管理器播放按钮动效
        EventPageUIEffects.PlayButtonPunchEffect(button.GetComponent<RectTransform>());
    }

    private void UpdateCompletionButton(NodeEventPoolManager eventPoolManager)
    {
        bool allComplete = eventPoolManager.AreAllEventsResolved();
        var completeButton = FindButton(canvasTransform, "AllEventsCompleteButton");
        
        if (completeButton != null)
        {
            completeButton.interactable = allComplete;
            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(() => GameManager.Instance.OnAllEventsCompleted());
        }
    }

    public void OnEventOptionConfirmed_v3(NodeEventPoolManager eventPoolManager)
    {
        if (ButtonA != null) ButtonA.interactable = false;
        if (ButtonB != null) ButtonB.interactable = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("UI_Success");

        Debug.Log($"✅ 事件已确认");
    }

    public void ShowEventCompletionConfirmation()
    {
        Debug.Log("🎯 显示事件完成确认窗口");
        
        var confirmPanel = FindTransform(canvasTransform, "EventCompletionConfirmationPanel");
        if (confirmPanel != null)
        {
            confirmPanel.gameObject.SetActive(true);
            var confirmButton = confirmPanel.GetComponentInChildren<Button>();
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() =>
                {
                    confirmPanel.gameObject.SetActive(false);
                    GameManager.Instance.OnEventCompletionConfirmed();
                });
            }
        }
    }

    private Transform FindTransform(Transform parent, string name)
    {
        if (parent == null) return null;
        var result = parent.Find(name);
        if (result != null) return result;

        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindTransform(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private Button FindButton(Transform parent, string name)
    {
        var transform = FindTransform(parent, name);
        return transform?.GetComponent<Button>();
    }
}
