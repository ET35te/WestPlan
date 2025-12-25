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
    private int forcedNextEventID = 0;  // 下一个强制跳转的事件ID

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
            ResourceManager.Instance.OnResourceDepleted += HandleResourceDepletion;
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
        UIManager.Instance.ShowNextEvent();
    }

    // =========================================================
    // ⚔️ 核心逻辑：事件与战斗结算
    // =========================================================

    // 由 UIManager 在点击“结果确认”按钮后调用
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

    // 处理选项结果 (骰子判定 + 资源扣除)
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

    // =========================================================
    // 🗺️ 节点推进与结算
    // =========================================================

    public void TriggerSettlement()
    {
        if (CurrentMonth >= 12) { TriggerEnding("Victory_Time"); return; }

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

        // 4. 开始新一轮事件
        UIManager.Instance.ShowNextEvent();
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
        if (string.IsNullOrEmpty(dataStr) || dataStr == "0:0") return "";
        string logBuilder = "";
        string[] entries = dataStr.Split(';');
        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;
            string[] parts = entry.Split(':');
            if (parts.Length == 2)
            {
                int id = int.Parse(parts[0]);
                int val = int.Parse(parts[1]);
                if (val != 0)
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
        PlayerPrefs.SetInt("Save_Water", ResourceManager.Instance.Water);
        PlayerPrefs.SetInt("Save_Troops", ResourceManager.Instance.Troops);
        PlayerPrefs.SetInt("Save_Money", ResourceManager.Instance.Money);
        PlayerPrefs.SetInt("Save_Horses", ResourceManager.Instance.Horses);
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
        ResourceManager.Instance.Water = PlayerPrefs.GetInt("Save_Water");
        ResourceManager.Instance.Troops = PlayerPrefs.GetInt("Save_Troops");
        ResourceManager.Instance.Money = PlayerPrefs.GetInt("Save_Money");
        ResourceManager.Instance.Horses = PlayerPrefs.GetInt("Save_Horses");
        ResourceManager.Instance.Armor = PlayerPrefs.GetInt("Save_Armor");

        CurrentMonth = PlayerPrefs.GetInt("Save_Month");
        CurrentNodeIndex = PlayerPrefs.GetInt("Save_NodeIdx");
        IsFantasyLine = PlayerPrefs.GetInt("Save_IsFantasy") == 1;

        CurrentEventCount = 0;

        // 加载后立刻刷新场景
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.UpdateResourceDisplay();
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        UIManager.Instance.ShowNextEvent();
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

    public void TriggerEnding(string endingType)
    {
        string endText = endingType == "Victory_Time" ? "历经艰辛，终于抵达终点。" : "旅途终结。";
        if (endingType == "Death_Belief") endText = "信念崩塌，倒在黄沙之中。";
        if (endingType == "Bad_End_Event") endText = "做出错误的选择，大汉的旗帜倒下了。";

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
            ResourceManager.Instance.OnResourceDepleted -= HandleResourceDepletion;
        }
    }
}