using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- 游戏进度 ---")]
    public string[] Nodes_Historical = { "玉门关", "白龙堆", "楼兰", "龟兹", "疏勒", "天山", "车师", "高昌", "敦煌", "长安", "洛阳", "终焉" };
    public string[] Nodes_Fantasy = { "玉门关", "若羌", "且末", "于阗", "莎车", "葱岭", "大宛", "康居", "北匈奴", "单于庭", "封狼居胥", "终焉" };

    public int CurrentNodeIndex = 0;
    public int CurrentMonth = 1;
    public bool IsFantasyLine = false;

    [Header("--- 流程控制 ---")]
    public int RandomEventsPerNode = 3; // 每个节点先过3个随机事件，再过剧情
    public int CurrentEventCount = 0;   // 当前节点已过的事件数
    public int forcedNextEventID = 0;  // 下一个强制跳转的事件ID
    // 缓存：选项产生的资源变化，等待玩家在结果确认时应用
    private string pendingResourceData = null;

    // 全局库存 (主要用于存档中转)
    public int GlobalFoodStock = 10;
    public int GlobalArmorStock = 5;

    private void Awake()
    {
        // 单例模式 + 场景切换不销毁
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 监听资源耗尽导致的游戏结束
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnGameEndingTriggered += HandleResourceDepletion;
        }
    }
    public void StartNewGame()
    {
        Debug.Log("🔄 开始新游戏：重置所有数据...");

        // 1. 重置 GM 自己的数据
        CurrentNodeIndex = 0;
        CurrentMonth = 1;
        CurrentEventCount = 0;
        IsFantasyLine = false;
        GlobalFoodStock = 10;
        GlobalArmorStock = 5;

        // 2. 重置 资源管理器
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResetResources();

        // 3. 重置 战斗管理器 (防止上一局的残余)
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.PlayerUnitCount = 5;
            BattleManager.Instance.EnemyUnitCount = 5;
            // 清空手牌UI等
        }

        // 4. 更新 UI 文本 (地点、资源)
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        if (ResourceManager.Instance != null) ResourceManager.Instance.ForceUpdateUI();

        // 5. 启动新系统：线性剧情流程
        Debug.Log("🎬 启动线性叙事系统...");
        StartNodeStoryFlow();
    }
    // =========================================================
    // 👑 核心架构：初始化流程 (解决白屏死锁)
    // =========================================================

    // 由 UIManager 在 OnSceneLoaded 时调用
    public void OnUIReady()
    {
        StartCoroutine(InitGameFlow());
    }

    IEnumerator InitGameFlow()
    {
        Debug.Log("🚀 GM: 开始初始化流程...");

        // 1. 等待 DataManager 加载完毕 (最多等 3秒，防止死循环)
        float timeout = 3.0f;
        while (DataManager.Instance == null || !DataManager.Instance.IsReady)
        {
            timeout -= Time.deltaTime;
            if (timeout <= 0)
            {
                Debug.LogError("❌ 初始化超时！强制启动（可能缺少 CSV 文件）。");
                break;
            }
            yield return null;
        }
        Debug.Log("✅ GM: 所有系统就绪！");
        // 2. 确保 ResourceManager 就绪
        while (ResourceManager.Instance == null) yield return null;

        Debug.Log("✅ GM: 所有系统就绪！");

        // 3. 初始同步资源
        // 如果是新游戏，把 GM 的低保同步给 RM；如果是读档，RM 的数据会覆盖这里
        if (PlayerPrefs.GetInt("HasSave", 0) == 0)
        {
            ResourceManager.Instance.Grain = GlobalFoodStock;
            ResourceManager.Instance.Armor = GlobalArmorStock;
            ResourceManager.Instance.ForceUpdateUI();
        }

        // 4. 刷新 UI 显示
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.UpdateResourceDisplay();
        
        // 5. 判断是读档还是新游戏流程
        // 如果当前没有任何事件在运行，就开始抽取第一个
        //UIManager.Instance.ShowNextEvent();
        yield return null;
    }

    // =========================================================    // 🔗 新增：线性分支事件系统 + 节点分页系统
    // =========================================================

    private DataManager.EventData_v2 currentEvent_v2 = null;
    private int currentNodeEventChainID = -1;  // 当前节点的事件链起点
    
    // 🎯 新增：节点事件池管理器（支持翻页和互斥选择）
    private NodeEventPoolManager eventPoolManager = null;
    
    // 📦 新增：缓存所有节点事件的选择结果（用于最终结算）
    private List<(int EventID, bool ChooseA, string ResultData)> allResolvedChoices = new List<(int, bool, string)>();

    /// <summary>
    /// 启动节点剧情流程（新系统）
    /// 顺序：ShowStoryPanel → 初始化事件池 → 显示第一个事件 → 翻页/选择 → 全部完成确认 → NodeEnd
    /// </summary>
    public void StartNodeStoryFlow()
    {
        Debug.Log($"🎬 ============ 启动节点剧情流程: Node {CurrentNodeIndex} ============");
        Debug.Log($"🕐 时间戳: {Time.time}");

        // 1. 获取该节点的剧情面板
        DataManager.StoryPanelData panel = DataManager.Instance.GetStoryPanelByNodeID(CurrentNodeIndex);
        if (panel == null)
        {
            Debug.LogWarning($"⚠️ 没有找到节点{CurrentNodeIndex}的剧情面板，跳过");
            StartNodeEventChain(-1);
            return;
        }

        Debug.Log($"✅ 获取到剧情面板: {panel.Title}, FirstEventID={panel.FirstEventID}");

        // 2. 显示剧情面板
        Debug.Log("📍 调用 UIManager.ShowStoryPanel()...");
        UIManager.Instance.ShowStoryPanel(panel);
        Debug.Log("📍 ShowStoryPanel() 调用完成");

        // 3. 记录该节点的首个事件ID
        currentNodeEventChainID = panel.FirstEventID;
        Debug.Log($"📍 已记录 currentNodeEventChainID = {currentNodeEventChainID}");
        
        // 3. 初始化事件池管理器
        InitializeNodeEventPool(panel.FirstEventID);
    }

    /// <summary>
    /// 初始化节点事件池 - 从首个事件ID出发，遍历事件链获取所有事件
    /// </summary>
    private void InitializeNodeEventPool(int firstEventID)
    {
        // 1. 创建事件池管理器（如果还没有）
        if (eventPoolManager == null)
        {
            // 从场景或创建
            eventPoolManager = FindObjectOfType<NodeEventPoolManager>();
            if (eventPoolManager == null)
            {
                GameObject poolObj = new GameObject("NodeEventPoolManager");
                eventPoolManager = poolObj.AddComponent<NodeEventPoolManager>();
            }
        }

        // 2. 获取事件链
        List<int> eventIDs = DataManager.Instance.GetNodeEventChain(firstEventID);

        // 3. 初始化事件池
        eventPoolManager.InitializeNodeEvents(eventIDs);

        // 4. 清空旧的选择记录
        allResolvedChoices.Clear();

        // 5. 显示第一个事件
        ShowEventPageUI();
    }

    /// <summary>
    /// 显示当前事件页面 UI
    /// </summary>
    private void ShowEventPageUI()
    {
        if (eventPoolManager == null)
        {
            Debug.LogError("❌ eventPoolManager 未初始化");
            return;
        }

        var evt = eventPoolManager.GetCurrentEvent();
        if (evt.EventData == null)
        {
            Debug.LogError("❌ 无法获取当前事件");
            return;
        }

        // 由 UIManager 显示当前事件页
        UIManager.Instance.ShowEventPageUI_v3(eventPoolManager);
    }

    /// <summary>
    /// 启动节点事件链（旧系统，保留兼容性）
    /// </summary>
    public void StartNodeEventChain(int firstEventID)
    {
        if (firstEventID <= 0)
        {
            Debug.LogWarning("⚠️ 无效的事件ID，直接进入节点结算");
            TriggerSettlement();
            return;
        }

        ShowEventByID_v2(firstEventID);
    }

    /// <summary>
    /// 按ID显示v2版本的事件
    /// </summary>
    public void ShowEventByID_v2(int eventID)
    {
        DataManager.EventData_v2 evt = DataManager.Instance.GetEventByID_v2(eventID);
        if (evt == null)
        {
            Debug.LogError($"❌ 找不到事件ID: {eventID}");
            return;
        }

        currentEvent_v2 = evt;
        UIManager.Instance.ShowEventUI_v2(evt);
    }

    /// <summary>
    /// 处理v2事件选项的点击（线性分支）
    /// </summary>
    public void ResolveEventOption_v2(DataManager.EventData_v2 evt, bool chooseA)
    {
        // 1. 确定选择
        string resultText = chooseA ? evt.OptA_Result_Txt : evt.OptB_Result_Txt;
        string resultData = chooseA ? evt.OptA_Result_Data : evt.OptB_Result_Data;
        int nextEventID = chooseA ? evt.NextID_A : evt.NextID_B;
        string effectType = evt.Effect_Type;

        // 2. 准备资源变化（延迟应用，等待玩家确认）
        pendingResourceData = string.IsNullOrEmpty(resultData) ? null : resultData;
        if (!string.IsNullOrEmpty(pendingResourceData))
        {
            string previewLog = BuildResourceChangePreview(pendingResourceData);
            resultText = resultText + "\n" + previewLog;
            Debug.Log($"📌 资源变化已缓存，等待确认: {pendingResourceData}");
        }

        // 3. 显示结果（玩家确认后再真正应用资源）
        UIManager.Instance.ShowEventResult_v2(resultText);

        // 4. 存储下一个事件ID供结果确认后使用
        forcedNextEventID = nextEventID;

        // 5. 处理特效
        if (!string.IsNullOrEmpty(effectType))
        {
            HandleEventEffect(effectType);
        }
    }

    /// <summary>
    /// 确认事件结果后的处理（线性分支）
    /// </summary>
    public void ConfirmEventResult_v2()
    {
        Debug.Log("📍 玩家确认事件结果，开始处理后续逻辑...");

        // 0. 如果有缓存的资源变化，先应用
        if (!string.IsNullOrEmpty(pendingResourceData))
        {
            Debug.Log($"📥 应用缓存的资源变化: {pendingResourceData}");
            string appliedLog = ApplyMultiResources(pendingResourceData);
            pendingResourceData = null;
            // 刷新 UI（如果需要）
            if (UIManager.Instance != null) UIManager.Instance.UpdateResourceDisplay();
            Debug.Log($"✅ 资源变化已应用: {appliedLog}");
        }

        // 1. 检查下一个事件ID
        if (forcedNextEventID == -1)
        {
            // -1 表示该节点事件链结束
            Debug.Log("📍 节点事件链结束");
            forcedNextEventID = 0;
            TriggerSettlement();
            return;
        }

        if (forcedNextEventID > 0)
        {
            int nextID = forcedNextEventID;
            forcedNextEventID = 0;
            ShowEventByID_v2(nextID);
            return;
        }

        Debug.LogWarning("⚠️ 未指定下一个事件");
    }

    // =========================================================
    // 🎯 新增：节点分页系统 - 事件页面交互
    // =========================================================

    /// <summary>翻到下一个事件页</summary>
    public void OnEventPageNext()
    {
        if (eventPoolManager == null) return;
        if (eventPoolManager.NextPage())
            ShowEventPageUI();
        else
            Debug.Log("📄 已经是最后一个事件");
    }

    /// <summary>翻到上一个事件页</summary>
    public void OnEventPagePrevious()
    {
        if (eventPoolManager == null) return;
        if (eventPoolManager.PreviousPage())
            ShowEventPageUI();
        else
            Debug.Log("📄 已经是第一个事件");
    }

    /// <summary>玩家选择事件选项（支持切换）</summary>
    public void OnEventOptionSelected_v3(bool chooseA)
    {
        if (eventPoolManager == null) return;
        eventPoolManager.SetCurrentChoice(chooseA);
        Debug.Log($"🎯 玩家选择已更新: {(chooseA ? \"选项A\" : \"选项B\")}");
    }

    /// <summary>玩家点击确认按钮，锁定当前事件为已处理</summary>
    public void OnEventOptionConfirmed()
    {
        if (eventPoolManager == null) return;
        var evt = eventPoolManager.GetCurrentEvent();
        if (evt.EventData == null) return;
        eventPoolManager.ResolveCurrentEvent();
        UIManager.Instance.OnEventOptionConfirmed_v3(eventPoolManager);
    }

    /// <summary>玩家完成所有事件处理后，弹出确认窗口</summary>
    public void OnAllEventsCompleted()
    {
        if (eventPoolManager == null) return;
        if (!eventPoolManager.AreAllEventsResolved())
        {
            int unresolvedCount = eventPoolManager.GetUnresolvedCount();
            Debug.LogWarning($"⚠️ 还有 {unresolvedCount} 个事件未处理");
            return;
        }
        allResolvedChoices = eventPoolManager.GetAllResolvedChoices();
        UIManager.Instance.ShowEventCompletionConfirmation();
    }

    /// <summary>确认窗口中玩家点击了确认，执行资源结算</summary>
    public void OnEventCompletionConfirmed()
    {
        ApplyAllEventResults();
        if (eventPoolManager != null)
            eventPoolManager.Clear();
        TriggerSettlement();
    }

    /// <summary>应用所有事件的资源结算</summary>
    private void ApplyAllEventResults()
    {
        Debug.Log("💰 开始结算所有事件的资源变化...");
        foreach (var (eventID, chooseA, resultData) in allResolvedChoices)
        {
            if (string.IsNullOrEmpty(resultData))
            {
                Debug.Log($"📌 事件 {eventID} 无资源变化");
                continue;
            }
            Debug.Log($"📥 应用事件 {eventID} 的资源变化: {resultData}");
            string appliedLog = ApplyMultiResources(resultData);
            Debug.Log($"✅ {appliedLog}");
        }
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateResourceDisplay();
        Debug.Log("✅ 所有事件资源结算完成");
    }

    // =========================================================    // ⚔️ 核心逻辑：事件与战斗结算
    // =========================================================

    // 由 UIManager 在点击“结果确认”按钮后调用
    // ❌ 旧系统已弃用 - 不再调用此方法
    /*
    public void CheckGameStateAfterResult()
    {
        // A. 如果有强制跳转 (通过 Effect_Type 设置了 JUMP:ID)
        if (forcedNextEventID != 0)
        {
            int nextID = forcedNextEventID;
            forcedNextEventID = 0; // 重置
            UIManager.Instance.ShowSpecificEvent(nextID);
            return;
        }

        // B. 计数增加
        CurrentEventCount++;

        // C. 判断是否该进入节点结算
        if (CurrentEventCount >= RandomEventsPerNode)
        {
            TriggerSettlement();
        }
        else
        {
            // 继续下一个随机事件
            UIManager.Instance.ShowNextEvent();
        }
    }
    */

    // ❌ 旧系统已弃用 - 使用新的 ResolveEventOption_v2() 替代
    /*
    public string ResolveEventOption(DataManager.EventData evt, bool chooseA)
    {
        string baseResultText = "";
        string dataString = "";

        int rate2 = chooseA ? evt.OptA_Res2_Rate : evt.OptB_Res2_Rate;
        int roll = Random.Range(0, 100);
        bool triggerResult2 = roll < rate2;

        if (triggerResult2)
        {
            baseResultText = chooseA ? evt.OptA_Res2_Txt : evt.OptB_Res2_Txt;
            dataString = chooseA ? evt.OptA_Res2_Data : evt.OptB_Res2_Data;
        }
        else
        {
            baseResultText = chooseA ? evt.OptA_Res1_Txt : evt.OptB_Res1_Txt;
            dataString = chooseA ? evt.OptA_Res1_Data : evt.OptB_Res1_Data;
        }

        string changeLog = ApplyMultiResources(dataString);
        HandleEventEffect(evt.Effect_Type); // 处理特效/跳转

        return baseResultText + changeLog;
    }
    */

    // =========================================================
    // 🗺️ 节点推进与结算
    // =========================================================

    public void TriggerSettlement()
    {
        // 📊 检查游戏是否应该结束
        if (CurrentMonth >= 12)
        {
            Debug.Log("🏁 游戏时间已满12个月，触发结局判定...");
            EvaluateAndTriggerEnding();
            return;
        }

        string summaryTitle = $"大汉建初元年 - {CurrentMonth}月";
        string place = GetCurrentNodeName();
        string summaryContent = $"全军抵达{place}。\n整备物资，等待下一次行动。";

        UIManager.Instance.ShowNodeSummary(summaryTitle, summaryContent);
    }

    public void GoToNextNode()
    {
        // 1. 数据更新
        CurrentMonth++;
        CurrentEventCount = 0; // 重置事件计数
        forcedNextEventID = 0;

        if (CurrentNodeIndex < Nodes_Historical.Length - 1) CurrentNodeIndex++;

        // 2. 存档
        SaveGame();

        // 3. UI 更新
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);

        // 4. ✅ 改为新系统(v2)：启动线性剧情流程
        // ❌ 旧代码已注释：UIManager.Instance.ShowNextEvent();
        StartNodeStoryFlow();
    }

    public string GetCurrentNodeName()
    {
        string[] targetNodes = IsFantasyLine ? Nodes_Fantasy : Nodes_Historical;
        if (targetNodes == null || targetNodes.Length == 0) return "未知区域";
        return targetNodes[Mathf.Clamp(CurrentNodeIndex, 0, targetNodes.Length - 1)];
    }

    // =========================================================
    // 🛠️ 资源与效果处理
    // =========================================================

    // 战斗结束后同步资源 (解决资源分散问题)
    public void UpdateGlobalStock(int food, int armor)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Grain = food;
            ResourceManager.Instance.Armor = armor;
            ResourceManager.Instance.ForceUpdateUI();
        }
        GlobalFoodStock = food;
        GlobalArmorStock = armor;
    }
    private string ApplyMultiResources(string dataStr)
    {
        // 防空检查
        if (string.IsNullOrEmpty(dataStr) || dataStr == "0:0") return "";
        
        string logBuilder = "";
        string[] entries = dataStr.Split(';'); // 分割不同资源组

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] parts = entry.Split(':'); // 分割 ID 和 数值

            // --- 🔥 修复开始：安全校验 ---
            if (parts.Length != 2)
            {
                Debug.LogError($"❌ 表格格式错误，跳过解析: '{entry}' (完整数据: {dataStr})");
                continue;
            }

            // 尝试解析 ID
            if (!int.TryParse(parts[0], out int id))
            {
                Debug.LogError($"❌ 资源ID无法解析为数字: '{parts[0]}' (完整数据: {entry})");
                continue;
            }

            // 尝试解析 数值
            if (!int.TryParse(parts[1], out int val))
            {
                Debug.LogError($"❌ 资源数值无法解析为数字: '{parts[1]}' (完整数据: {entry})");
                continue;
            }
            // --- 🔥 修复结束 ---

            // 如果解析成功，继续执行原来的逻辑
            if (val != 0)
            {
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.ChangeResource(id, val);
                    string resName = ResourceManager.Instance.GetResName(id);
                    string sign = val > 0 ? "+" : "";
                    string colorHex = val > 0 ? "#00FF00" : "#FF4500";
                    logBuilder += $"\n<color={colorHex}>({resName} {sign}{val})</color>";
                }
            }
        }
        return logBuilder;
    }

    /// <summary>
    /// 生成资源变化预览文本，但不实际修改资源（用于在结果面板显示预览，玩家确认后再应用）
    /// </summary>
    private string BuildResourceChangePreview(string dataStr)
    {
        if (string.IsNullOrEmpty(dataStr) || dataStr == "0:0") return "";

        string logBuilder = "";
        string[] entries = dataStr.Split(';');
        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;
            string[] parts = entry.Split(':');
            if (parts.Length != 2) continue;
            if (!int.TryParse(parts[0], out int id)) continue;
            if (!int.TryParse(parts[1], out int val)) continue;

            string resName = ResourceManager.Instance != null ? ResourceManager.Instance.GetResName(id) : $"Res{id}";
            string sign = val > 0 ? "+" : "";
            string colorHex = val > 0 ? "#00FF00" : "#FF4500";
            logBuilder += $"\n<color={colorHex}>({resName} {sign}{val})</color>";
        }

        return logBuilder;
    }

    public void HandleEventEffect(string effectType)
    {
        if (string.IsNullOrEmpty(effectType)) return;
        Debug.Log($"触发特效: {effectType}");

        if (effectType.StartsWith("JUMP:"))
        {
            int jumpID = int.Parse(effectType.Split(':')[1]);
            forcedNextEventID = jumpID;
            return;
        }

        switch (effectType)
        {
            case "SWITCH_ROUTE_FANTASY": IsFantasyLine = true; break;
            case "GAME_OVER": TriggerEnding("Bad_End_Event"); break;
            case "VICTORY": TriggerEnding("Victory_Event"); break;
            case "NODE_END": CurrentEventCount = 999; break; // 强制立刻结算
        }
    }

    // =========================================================
    // 💾 存档与重置系统
    // =========================================================

    public void SaveGame()
    {
        if (ResourceManager.Instance == null) return;

        PlayerPrefs.SetInt("Save_Belief", ResourceManager.Instance.Belief);
        PlayerPrefs.SetInt("Save_Grain", ResourceManager.Instance.Grain);
        PlayerPrefs.SetInt("Save_Armor", ResourceManager.Instance.Armor);

        PlayerPrefs.SetInt("Save_Month", CurrentMonth);
        PlayerPrefs.SetInt("Save_NodeIdx", CurrentNodeIndex);
        PlayerPrefs.SetInt("Save_IsFantasy", IsFantasyLine ? 1 : 0);

        PlayerPrefs.SetInt("HasSave", 1);
        PlayerPrefs.Save();
        Debug.Log("进度已保存");
    }

    public void LoadGame()
    {
        if (PlayerPrefs.GetInt("HasSave", 0) == 0) return;

        ResourceManager.Instance.Belief = PlayerPrefs.GetInt("Save_Belief");
        ResourceManager.Instance.Grain = PlayerPrefs.GetInt("Save_Grain");
        ResourceManager.Instance.Armor = PlayerPrefs.GetInt("Save_Armor");

        CurrentMonth = PlayerPrefs.GetInt("Save_Month");
        CurrentNodeIndex = PlayerPrefs.GetInt("Save_NodeIdx");
        IsFantasyLine = PlayerPrefs.GetInt("Save_IsFantasy") == 1;
        CurrentEventCount = 0;

        // 加载后立刻刷新场景
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.UpdateResourceDisplay();
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        
        // ✅ 新系统：启动线性剧情流程而非随机事件
        StartNodeStoryFlow();
    }

    public void ResetDataOnly()
    {
        PlayerPrefs.DeleteAll();
        if (ResourceManager.Instance != null) ResourceManager.Instance.ResetResources();

        CurrentMonth = 1;
        CurrentNodeIndex = 0;
        IsFantasyLine = false;
        CurrentEventCount = 0;
        GlobalFoodStock = 10;
        GlobalArmorStock = 5;

        Debug.Log("GM: 数据已重置 (New Game)");
    }

    /// <summary>
    /// 📊 根据游戏状态自动判定结局类型
    /// </summary>
    public void EvaluateAndTriggerEnding()
    {
        string endingType = EvaluateEndingCondition();
        Debug.Log($"🏁 游戏结局判定: {endingType}");
        TriggerEnding(endingType);
    }

    /// <summary>
    /// 🔍 根据游戏状态评估结局条件
    /// 返回: "Victory_Ending", "Failure_Ending", "Death_Ending", "Peaceful_Ending"
    /// </summary>
    private string EvaluateEndingCondition()
    {
        // 优先级1: 检查是否已经达到终点月份
        if (CurrentMonth >= 12)
        {
            Debug.Log("✅ 游戏进度: 已完成12个月的旅程");
            
            // 根据路线和资源判定具体结局
            if (IsFantasyLine)
            {
                return "Victory_Fantasy";  // 幻想线胜利结局
            }
            else
            {
                // 检查是否是和平结局（没有大的损失）
                if (ResourceManager.Instance.Belief > 50 && ResourceManager.Instance.Grain > 30)
                    return "Victory_Ending";
                else if (ResourceManager.Instance.Belief < 20)
                    return "Failure_Ending";
                else
                    return "Peaceful_Ending";
            }
        }

        // 优先级2: 检查资源是否耗尽（游戏失败条件由ResourceManager触发）
        // 这里只作为备用判定

        // 默认返回失败结局
        return "Failure_Ending";
    }

    public void TriggerEnding(string endingType)
    {
        string endText = endingType == "Victory_Time" ? "历经艰辛，终于抵达终点。" : "旅途终结。";
        if (endingType == "Death_Belief") endText = "信念崩塌，倒在黄沙之中。";
        if (endingType == "Bad_End_Event") endText = "做出错误的选择，大汉的旗帜倒下了。";
        if (endingType == "Victory_Ending") endText = "班超成功建立了与西域诸国的联系。您的名字将被刻在历史的丰碑上！";
        if (endingType == "Victory_Fantasy") endText = "您掌握了古老的魔法力量，成为了一位传奇人物。幻想的世界因您而改变！";
        if (endingType == "Peaceful_Ending") endText = "您通过智慧和外交，在不流血的情况下赢得了最大的胜利。";
        if (endingType == "Failure_Ending") endText = "您黯然返回长安，多年的热血换来了无尽的遗憾。";

        PlayerPrefs.DeleteKey("HasSave");
        UIManager.Instance.ShowEnding(endText);
    }

    private void HandleResourceDepletion(string reason)
    {
        TriggerEnding(reason);
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnGameEndingTriggered -= HandleResourceDepletion;
        }
    }
}