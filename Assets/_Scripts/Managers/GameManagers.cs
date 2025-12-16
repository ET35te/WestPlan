using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- 游戏进度 ---")]
    public string[] Nodes_Historical = { "玉门关", "白龙堆", "楼兰", "龟兹", "疏勒", "天山", "车师", "高昌", "敦煌", "长安", "洛阳", "终焉" }; 
    public string[] Nodes_Fantasy = { "玉门关", "若羌", "且末", "于阗", "莎车", "葱岭", "大宛", "康居", "北匈奴", "单于庭", "封狼居胥", "终焉" };
    
    public int CurrentNodeIndex = 0; 
    public int CurrentMonth = 1;     
    public bool IsFantasyLine = false; // 路线标记

    [Header("--- 时间控制 ---")]
    public float TimeLimitPerNode = 60f; 
    public float CurrentTimer;
    public bool IsTimerRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else { Instance = this; DontDestroyOnLoad(this.gameObject); }
    }

    void Start()
    {
        // 游戏启动时不自动开始，等待点击“开始”或“继续”
        IsTimerRunning = false;
    }

    void Update()
    {
        if (IsTimerRunning)
        {
            CurrentTimer -= Time.deltaTime;
            if (CurrentTimer <= 0) TriggerSettlement(); 
        }
    }

    // --- 核心流程：节点结算与存档 ---
    public void TriggerSettlement()
    {
        IsTimerRunning = false; 
        
        if (CurrentMonth >= 12) { TriggerEnding("Victory_Time"); return; }

        string summaryTitle = $"大汉建初元年 - {CurrentMonth}月";
        string place = GetCurrentNodeName();
        string summaryContent = $"全军抵达{place}。\n整备物资，等待下一次行动。";

        UIManager.Instance.ShowNodeSummary(summaryTitle, summaryContent);
        
        // --- 💾 周日思考：节点自动存档 ---
        SaveGame();
    }

    public void GoToNextNode()
    {
        CurrentMonth++;
        if (CurrentNodeIndex < Nodes_Historical.Length - 1) CurrentNodeIndex++;

        CurrentTimer = TimeLimitPerNode;
        IsTimerRunning = true;

        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        UIManager.Instance.ShowNextEvent();
    }

    // --- 💀 周日思考：结局/死亡 ---
    public void TriggerEnding(string endingType)
    {
        IsTimerRunning = false;
        string endText = "";
        
        // 根据结局类型显示不同文案 (后续可扩展)
        if (endingType == "Death_Belief") endText = "军心涣散，再无力西进。\n你倒在了黄沙之中...";
        else if (endingType == "Victory_Time") endText = "历经十二载，终于完成了使命！";
        else endText = "旅途终结。";

        // 清空存档，防止死档循环
        PlayerPrefs.DeleteKey("HasSave"); 
        
        UIManager.Instance.ShowEnding(endText);
    }

    // --- 📝 周二任务：特殊效果处理 ---
    // 在 UIManager 点击选项后调用这个，处理 Effect_Type
    public void HandleEventEffect(string effectType)
    {
        if (string.IsNullOrEmpty(effectType)) return;

        Debug.Log($"触发特殊效果: {effectType}");

        switch (effectType)
        {
            case "SWITCH_ROUTE_FANTASY":
                IsFantasyLine = true;
                Debug.Log(">>> 进入幻想线！ <<<");
                break;
            case "GAME_OVER":
                TriggerEnding("Bad_End_Event");
                break;
            case "VICTORY":
                TriggerEnding("Victory_Event");
                break;
            //此处可以拓展事件    
        }
    }

    // --- 事件判定逻辑 (保留原有，不重复发) ---
    public string ResolveEventOption(DataManager.EventData evt, bool chooseA)
    {
        string baseResultText = "";
        string dataString = ""; // 存那个 "101:10;102:-5" 字符串

        // 1. 判定概率
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

        // 2. 解析字符串并应用资源 + 生成变动文本
        string changeLog = ApplyMultiResources(dataString);

        // 3. 处理特殊效果
        HandleEventEffect(evt.Effect_Type);

        // 4. 返回最终文本
        return baseResultText + changeLog;
    }

    // --- 核心：多资源解析器 ---
    // 输入: "101:10;102:-50"
    // 输出: "\n(信念 +10)\n(粮食 -50)"
    private string ApplyMultiResources(string dataStr)
    {
        if (string.IsNullOrEmpty(dataStr) || dataStr == "0:0") return "";

        string logBuilder = "";
        
        // A. 按分号拆分多组数据: ["101:10", "102:-50"]
        string[] entries = dataStr.Split(';');

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            // B. 按冒号拆分 ID 和 数值
            string[] parts = entry.Split(':');
            if (parts.Length == 2)
            {
                int id = int.Parse(parts[0]);
                int val = int.Parse(parts[1]);

                if (val != 0)
                {
                    // 执行变动
                    ResourceManager.Instance.ChangeResource(id, val);

                    // 拼接显示文本
                    string resName = GetResName(id);
                    string sign = val > 0 ? "+" : "";
                    string colorHex = val > 0 ? "#00FF00" : "#FF4500"; // 绿涨红跌
                    
                    logBuilder += $"\n<color={colorHex}>({resName} {sign}{val})</color>";
                }
            }
        }

        return logBuilder;
    }

    // --- 💾 存档系统实现 ---
    public void SaveGame()
    {
        PlayerPrefs.SetInt("Saved_Belief", ResourceManager.Instance.Belief);
        PlayerPrefs.SetInt("Saved_Grain", ResourceManager.Instance.Grain);
        // ... 保存其他资源 (Water, Troops, Money, Horses, Armor)
        PlayerPrefs.SetInt("Saved_Water", ResourceManager.Instance.Water);
        PlayerPrefs.SetInt("Saved_Troops", ResourceManager.Instance.Troops);
        PlayerPrefs.SetInt("Saved_Money", ResourceManager.Instance.Money);
        PlayerPrefs.SetInt("Saved_Horses", ResourceManager.Instance.Horses);
        PlayerPrefs.SetInt("Saved_Armor", ResourceManager.Instance.Armor);

        PlayerPrefs.SetInt("Saved_Month", CurrentMonth);
        PlayerPrefs.SetInt("Saved_NodeIndex", CurrentNodeIndex);
        PlayerPrefs.SetInt("Saved_IsFantasy", IsFantasyLine ? 1 : 0);
        
        PlayerPrefs.SetInt("HasSave", 1); // 标记有存档
        PlayerPrefs.Save();
        Debug.Log("【系统】游戏已保存");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("HasSave")) return;

        ResourceManager.Instance.Belief = PlayerPrefs.GetInt("Saved_Belief");
        ResourceManager.Instance.Grain = PlayerPrefs.GetInt("Saved_Grain");
        // ... 读取其他资源
        ResourceManager.Instance.Water = PlayerPrefs.GetInt("Saved_Water");
        ResourceManager.Instance.Troops = PlayerPrefs.GetInt("Saved_Troops");
        ResourceManager.Instance.Money = PlayerPrefs.GetInt("Saved_Money");
        ResourceManager.Instance.Horses = PlayerPrefs.GetInt("Saved_Horses");
        ResourceManager.Instance.Armor = PlayerPrefs.GetInt("Saved_Armor");

        CurrentMonth = PlayerPrefs.GetInt("Saved_Month");
        CurrentNodeIndex = PlayerPrefs.GetInt("Saved_NodeIndex");
        IsFantasyLine = PlayerPrefs.GetInt("Saved_IsFantasy") == 1;

        // 恢复状态
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        CurrentTimer = TimeLimitPerNode;
        IsTimerRunning = true;
        
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        UIManager.Instance.UpdateResourceDisplay();
        Debug.Log("【系统】存档已读取");
    }

    public void StartNewGame()
    {
        PlayerPrefs.DeleteAll(); // 清除旧存档
        // 重置资源 (这里简单写，你可以封装 Reset 方法)
        ResourceManager.Instance.Belief = 80;
        ResourceManager.Instance.Grain = 100;
        // ...
        
        CurrentMonth = 1;
        CurrentNodeIndex = 0;
        IsFantasyLine = false;
        
        CurrentTimer = TimeLimitPerNode;
        IsTimerRunning = true;
        
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        UIManager.Instance.ShowNextEvent();
    }

    public string GetCurrentNodeName()
    {
        if (IsFantasyLine) return Nodes_Fantasy[Mathf.Clamp(CurrentNodeIndex, 0, Nodes_Fantasy.Length-1)];
        else return Nodes_Historical[Mathf.Clamp(CurrentNodeIndex, 0, Nodes_Historical.Length-1)];
    }

    public string GetResName(int id)
    {
        switch (id) {
            case 101: return "信念"; case 102: return "粮食";
            case 103: return "储水"; case 104: return "兵力";
            case 105: return "财货"; case 106: return "马匹";
            case 107: return "披甲"; default: return "资源";
        }
    }
}