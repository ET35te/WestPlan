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
    public bool IsFantasyLine = false; 

    [Header("--- 流程控制 ---")]
    public int RandomEventsPerNode = 3; // 每个节点先过3个随机事件，再过剧情
    public int CurrentEventCount = 0;   // 当前节点已过的事件数
    private int forcedNextEventID = 0;  // 下一个强制跳转的事件ID

    private void Awake()
    {
        // 单例模式 + 场景切换不销毁
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject); // 如果已经有一个Manager了，销毁新的
        }
        else 
        {
            Instance = this; 
            DontDestroyOnLoad(gameObject); // 👈 关键：切换场景时，我不要死！
        }
    }

    void Start()
    {
        // 游戏启动等待UI调用 StartNewGame 或 LoadGame
    }

    // --- 核心流程 1: 处理事件结果后的跳转 ---
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
        // 逻辑：随机事件跑够了 -> 触发该节点的“最终剧情” -> 最终剧情结束后 -> 触发结算
        // 这里简化：假设跑完 RandomEventsPerNode 次后，直接进入结算
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
    // 把原来的 StartNewGame 拆分一下，去掉 UI 操作
    public void ResetDataOnly()
    {
        // 只重置数据，不调用 UI
        PlayerPrefs.DeleteAll(); 
        if(ResourceManager.Instance != null) ResourceManager.Instance.ResetResources();
        
        CurrentMonth = 1;
        CurrentNodeIndex = 0;
        IsFantasyLine = false;
        CurrentEventCount = 0;
        
        Debug.Log("GM: 数据已重置，准备进入新游戏...");
    }
    // --- 核心流程 2: 触发节点结算界面 ---
    public void TriggerSettlement()
    {
        if (CurrentMonth >= 12) { TriggerEnding("Victory_Time"); return; }

        string summaryTitle = $"大汉建初元年 - {CurrentMonth}月";
        string place = GetCurrentNodeName();
        string summaryContent = $"全军抵达{place}。\n整备物资，等待下一次行动。";

        UIManager.Instance.ShowNodeSummary(summaryTitle, summaryContent);
        
        // 注意：现在结算时不存档，点击“继续”进入下一关时才存档，或者在这里存也可以
        // 建议：点击“继续”进入下一关的一瞬间存档，保证玩家是在新状态下开始
    }

    // --- 核心流程 3: 前往下一站 (由结算界面的继续按钮调用) ---
    public void GoToNextNode()
    {
        // 1. 数据更新
        CurrentMonth++;
        CurrentEventCount = 0; // 重置事件计数
        forcedNextEventID = 0;
        
        if (CurrentNodeIndex < Nodes_Historical.Length - 1) CurrentNodeIndex++;

        // 2. 存档 (确保下次进来是新的一关)
        SaveGame();

        // 3. UI 更新
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        
        // 4. 开始新一轮事件
        UIManager.Instance.ShowNextEvent();
    }

    // --- 特殊效果处理 (新增 JUMP) ---
    public void HandleEventEffect(string effectType)
    {
        if (string.IsNullOrEmpty(effectType)) return;
        Debug.Log($"触发特效: {effectType}");

        // 解析 JUMP:1005 格式
        if (effectType.StartsWith("JUMP:"))
        {
            int jumpID = int.Parse(effectType.Split(':')[1]);
            forcedNextEventID = jumpID;
            Debug.Log($">>> 准备跳转至事件 {jumpID}");
            return;
        }

        switch (effectType)
        {
            case "SWITCH_ROUTE_FANTASY":
                IsFantasyLine = true;
                break;
            case "GAME_OVER":
                TriggerEnding("Bad_End_Event");
                break;
            case "VICTORY":
                TriggerEnding("Victory_Event");
                break;
            case "NODE_END": // 强制立刻结算
                CurrentEventCount = 999; 
                break;
        }
    }

    // --- 存档系统 ---
    public void SaveGame()
    {
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

        CurrentEventCount = 0; // 读档后重置当前进度计数

        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.UpdateResourceDisplay();
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        UIManager.Instance.ShowNextEvent();
    }

    public void StartNewGame()
    {
        PlayerPrefs.DeleteAll(); 
        ResourceManager.Instance.ResetResources(); // 需确保 ResourceManager 有此方法
        
        CurrentMonth = 1;
        CurrentNodeIndex = 0;
        IsFantasyLine = false;
        CurrentEventCount = 0;
        
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        UIManager.Instance.ShowNextEvent();
    }

    public string GetCurrentNodeName()
    {
        if (IsFantasyLine) return Nodes_Fantasy[Mathf.Clamp(CurrentNodeIndex, 0, Nodes_Fantasy.Length-1)];
        else return Nodes_Historical[Mathf.Clamp(CurrentNodeIndex, 0, Nodes_Historical.Length-1)];
    }

    // --- 事件判定 (保持不变) ---
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
                    string resName = ResourceManager.Instance.GetResName(id); // 需确保 ResourceManager 此方法 Public
                    string sign = val > 0 ? "+" : "";
                    string colorHex = val > 0 ? "#00FF00" : "#FF4500"; 
                    logBuilder += $"\n<color={colorHex}>({resName} {sign}{val})</color>";
                }
            }
        }
        return logBuilder;
    }

    public void TriggerEnding(string endingType)
    {
        string endText = endingType == "Victory_Time" ? "历经艰辛，终于抵达终点。" : "旅途终结。";
        if (endingType == "Death_Belief") endText = "信念崩塌，倒在黄沙之中。";
        
        PlayerPrefs.DeleteKey("HasSave"); 
        UIManager.Instance.ShowEnding(endText);
    }
}