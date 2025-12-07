using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- 游戏进度配置 ---")]
    // 史实路线节点名称
    public string[] Nodes_Historical = { "玉门关", "白龙堆", "楼兰", "龟兹", "疏勒", "天山", "车师", "高昌", "敦煌", "长安", "洛阳", "终焉" }; 
    // 幻想路线节点名称
    public string[] Nodes_Fantasy = { "玉门关", "若羌", "且末", "于阗", "莎车", "葱岭", "大宛", "康居", "北匈奴", "单于庭", "封狼居胥", "终焉" };
    
    public int CurrentNodeIndex = 0; // 当前第几站
    public int CurrentMonth = 1;     // 当前月份
    public bool IsFantasyLine = false; // 是否进入幻想线

    [Header("--- 时间控制 ---")]
    public float TimeLimitPerNode = 60f; // 每个节点(月)现实时间 60秒
    public float CurrentTimer;
    public bool IsTimerRunning = false;

private void Awake()
    {
        // --- 👇 追踪代码 开始 👇 ---
        Debug.Log($"【侦探】GameManager 启动了！" +
                  $"名字: {gameObject.name} | " +
                  $"ID: {gameObject.GetInstanceID()} | " +
                  $"场景: {gameObject.scene.name}");
        // --- 👆 追踪代码 结束 👆 ---

        if (Instance != null && Instance != this) 
        {
            Debug.LogError($"【侦探】发现重复！我是冒牌货 ({gameObject.name})，我要自杀，原来的老大ID是: {Instance.gameObject.GetInstanceID()}");
            Destroy(this.gameObject);
        }
        else 
        { 
            Instance = this; 
            // DontDestroyOnLoad(this.gameObject); // 暂时注释
        }
    }
    void Start()
    {
        // 游戏开始时的初始化
        CurrentTimer = TimeLimitPerNode;
        IsTimerRunning = true;
    }

    void Update()
    {
        // 倒计时逻辑：只有在 Gameplay 状态下才倒计时
        if (IsTimerRunning)
        {
            CurrentTimer -= Time.deltaTime;
            
            // TODO: 这里可以调用 UI 更新倒计时显示文本
            // UIManager.Instance.UpdateTimerText(CurrentTimer);

            if (CurrentTimer <= 0)
            {
                TriggerSettlement(); // 时间到，强制结算
            }
        }
    }

    // --- 核心流程 1: 触发节点结算 ---
    public void TriggerSettlement()
    {
        IsTimerRunning = false; // 暂停时间
        
        // 1. 判断是否大结局 (第12月)
        if (CurrentMonth >= 12)
        {
            UIManager.Instance.SwitchState(UIManager.UIState.Ending);
            return;
        }

        // 2. 正常结算
        // 计算本月总结文案 (这里先写死，后续可根据表现生成)
        string summaryTitle = $"大汉建初元年 - {CurrentMonth}月";
        string summaryContent = $"本月行军至{GetCurrentNodeName()}。\n粮草消耗正常，士气尚可。\n(此处可扩展更多随机总结)";

        UIManager.Instance.ShowNodeSummary(summaryTitle, summaryContent);
        
        // 这里可以执行自动存档 SaveGame();
    }

    // --- 核心流程 2: 前往下一站 ---
    // 由 NodeSummary 面板的“继续”按钮调用
    public void GoToNextNode()
    {
        CurrentMonth++;
        
        // 移动到下一个地名，防止数组越界
        if (CurrentNodeIndex < Nodes_Historical.Length - 1) 
            CurrentNodeIndex++;

        // 重置时间
        CurrentTimer = TimeLimitPerNode;
        IsTimerRunning = true;

        // 更新 UI 上的地名显示
        UIManager.Instance.UpdatePlaceName(GetCurrentNodeName());

        // 回到游玩界面
        UIManager.Instance.SwitchState(UIManager.UIState.Gameplay);
        
        // 立即触发一个新事件
        UIManager.Instance.ShowNextEvent();
    }

    // 获取当前地名
    public string GetCurrentNodeName()
    {
        if (IsFantasyLine)
            return Nodes_Fantasy[Mathf.Clamp(CurrentNodeIndex, 0, Nodes_Fantasy.Length-1)];
        else
            return Nodes_Historical[Mathf.Clamp(CurrentNodeIndex, 0, Nodes_Historical.Length-1)];
    }

    // --- 事件判定逻辑 (保留之前的) ---
    public string ResolveEventOption(DataManager.EventData evt, bool chooseA)
    {
        // ... (保留你之前的 ResolveEventOption 代码，这里为了篇幅省略，请把之前的逻辑贴回来) ...
        // 如果找不到之前的代码，告诉我，我再发一遍完整版
        return "事件结果..."; // 占位
    }
    
    // --- 辅助：获取资源名 (保留之前的) ---
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