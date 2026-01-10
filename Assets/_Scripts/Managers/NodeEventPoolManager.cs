using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 节点事件池管理器 - 管理单个节点内的所有事件
/// 职责：
///   1. 初始化节点事件池（从事件链解析获得所有事件）
///   2. 管理翻页逻辑（Next/Previous）
///   3. 管理选项互斥选择（玩家可在确认前随意切换）
///   4. 追踪事件处理状态
/// </summary>
public class NodeEventPoolManager : MonoBehaviour
{
    // ========== 数据结构 ==========
    
    /// <summary>事件页面数据 - 包含事件本体和处理状态</summary>
    [System.Serializable]
    public struct EventPageData
    {
        public DataManager.EventData_v2 EventData;      // 事件本体
        public bool IsResolved;                         // 是否已处理
        public bool ChooseA;                            // 当前选择 (true=选A, false=选B)
    }

    // ========== 单例模式 ==========
    public static NodeEventPoolManager Instance { get; private set; }

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

    // ========== 状态 ==========
    
    /// 当前节点的所有事件
    private List<EventPageData> currentNodeEvents = new List<EventPageData>();
    
    /// 当前显示的事件页索引
    private int currentPageIndex = 0;

    // ========== 查询方法 ==========
    
    /// <summary>获取当前显示的事件</summary>
    public EventPageData GetCurrentEvent()
    {
        if (currentPageIndex >= 0 && currentPageIndex < currentNodeEvents.Count)
            return currentNodeEvents[currentPageIndex];
        return default;
    }

    /// <summary>获取指定索引的事件</summary>
    public EventPageData GetEventAt(int index)
    {
        if (index >= 0 && index < currentNodeEvents.Count)
            return currentNodeEvents[index];
        return default;
    }

    /// <summary>获取总事件数</summary>
    public int GetTotalEventCount() => currentNodeEvents.Count;

    /// <summary>获取当前页索引</summary>
    public int GetCurrentPageIndex() => currentPageIndex;

    /// <summary>获取已解决的事件数</summary>
    public int GetResolvedCount() => currentNodeEvents.Count(e => e.IsResolved);

    /// <summary>检查是否全部处理完毕</summary>
    public bool AreAllEventsResolved()
    {
        if (currentNodeEvents.Count == 0) return false;
        return GetResolvedCount() == currentNodeEvents.Count;
    }

    /// <summary>获取未解决的事件数</summary>
    public int GetUnresolvedCount() => currentNodeEvents.Count - GetResolvedCount();

    // ========== 导航方法 ==========

    /// <summary>跳转到指定页面</summary>
    public bool GoToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= currentNodeEvents.Count)
            return false;
        
        currentPageIndex = pageIndex;
        Debug.Log($"📄 跳转到页面 {currentPageIndex + 1}/{currentNodeEvents.Count}");
        return true;
    }

    /// <summary>下一页（返回是否成功）</summary>
    public bool NextPage()
    {
        if (currentPageIndex + 1 < currentNodeEvents.Count)
        {
            currentPageIndex++;
            Debug.Log($"📄 翻到下一页: {currentPageIndex + 1}/{currentNodeEvents.Count}");
            return true;
        }
        return false;
    }

    /// <summary>上一页</summary>
    public bool PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            Debug.Log($"📄 翻到上一页: {currentPageIndex + 1}/{currentNodeEvents.Count}");
            return true;
        }
        return false;
    }

    /// <summary>自动跳转到下一个未处理事件</summary>
    public bool JumpToNextUnresolved()
    {
        for (int i = currentPageIndex + 1; i < currentNodeEvents.Count; i++)
        {
            if (!currentNodeEvents[i].IsResolved)
            {
                currentPageIndex = i;
                Debug.Log($"📄 自动跳转到未处理事件: {currentPageIndex + 1}/{currentNodeEvents.Count}");
                return true;
            }
        }
        return false;
    }

    // ========== 选项管理 ==========

    /// <summary>
    /// 设置当前事件的选择（支持切换）
    /// 注意：只有当玩家点击"确认"时才会真正标记为已处理
    /// </summary>
    public void SetCurrentChoice(bool chooseA)
    {
        if (currentPageIndex < 0 || currentPageIndex >= currentNodeEvents.Count)
            return;

        var evt = currentNodeEvents[currentPageIndex];
        evt.ChooseA = chooseA;
        currentNodeEvents[currentPageIndex] = evt;

        Debug.Log($"🎯 事件 {currentPageIndex + 1} 选择已更新: {(chooseA ? "选项A" : "选项B")}");
    }

    /// <summary>
    /// 获取当前事件的选择
    /// </summary>
    public bool GetCurrentChoice()
    {
        if (currentPageIndex < 0 || currentPageIndex >= currentNodeEvents.Count)
            return true;  // 默认选A

        return currentNodeEvents[currentPageIndex].ChooseA;
    }

    /// <summary>
    /// 获取当前选择的选项数据
    /// </summary>
    public (string ResultText, string ResultData, int NextID) GetCurrentChoiceResult()
    {
        var evt = GetCurrentEvent();
        if (evt.EventData == null)
            return ("", "", -1);

        bool chooseA = evt.ChooseA;
        return (
            chooseA ? evt.EventData.OptA_Result_Txt : evt.EventData.OptB_Result_Txt,
            chooseA ? evt.EventData.OptA_Result_Data : evt.EventData.OptB_Result_Data,
            chooseA ? evt.EventData.NextID_A : evt.EventData.NextID_B
        );
    }

    // ========== 状态更新 ==========

    /// <summary>标记当前事件为已处理</summary>
    public void ResolveCurrentEvent()
    {
        if (currentPageIndex < 0 || currentPageIndex >= currentNodeEvents.Count)
            return;

        var evt = currentNodeEvents[currentPageIndex];
        evt.IsResolved = true;
        currentNodeEvents[currentPageIndex] = evt;

        int resolved = GetResolvedCount();
        int total = GetTotalEventCount();
        Debug.Log($"✅ 事件 {currentPageIndex + 1} 已处理 ({resolved}/{total})");
    }

    /// <summary>获取所有选择的结果数据（用于最终结算）</summary>
    public List<(int EventID, bool ChooseA, string ResultData)> GetAllResolvedChoices()
    {
        var result = new List<(int, bool, string)>();

        foreach (var evt in currentNodeEvents)
        {
            if (evt.IsResolved && evt.EventData != null)
            {
                string resultData = evt.ChooseA ? evt.EventData.OptA_Result_Data : evt.EventData.OptB_Result_Data;
                result.Add((evt.EventData.ID, evt.ChooseA, resultData));
            }
        }

        return result;
    }

    // ========== 初始化 ==========

    /// <summary>
    /// 初始化节点事件池（从事件链遍历获得所有事件）
    /// </summary>
    public void InitializeNodeEvents(List<int> eventIDs)
    {
        currentNodeEvents.Clear();
        currentPageIndex = 0;

        if (eventIDs == null || eventIDs.Count == 0)
        {
            Debug.LogWarning("⚠️ 事件列表为空，无法初始化节点事件池");
            return;
        }

        foreach (int id in eventIDs)
        {
            var evt = DataManager.Instance.GetEventByID_v2(id);
            if (evt != null)
            {
                currentNodeEvents.Add(new EventPageData
                {
                    EventData = evt,
                    IsResolved = false,
                    ChooseA = true  // 默认选A
                });
            }
        }

        Debug.Log($"📋 ✅ 初始化节点事件池：{currentNodeEvents.Count} 个事件");
        if (currentNodeEvents.Count > 0)
        {
            Debug.Log($"   首个事件: {currentNodeEvents[0].EventData.Title}");
            Debug.Log($"   末尾事件: {currentNodeEvents[currentNodeEvents.Count - 1].EventData.Title}");
        }
    }

    /// <summary>清空事件池</summary>
    public void Clear()
    {
        currentNodeEvents.Clear();
        currentPageIndex = 0;
        Debug.Log("🗑️ 事件池已清空");
    }

    // ========== Debug Helper ==========

    /// <summary>打印当前状态（Debug用）</summary>
    public void PrintDebugInfo()
    {
        Debug.Log("\n========== 📋 NodeEventPoolManager 状态 ==========");
        Debug.Log($"总事件数: {GetTotalEventCount()}");
        Debug.Log($"当前页: {currentPageIndex + 1}/{GetTotalEventCount()}");
        Debug.Log($"已处理: {GetResolvedCount()}/{GetTotalEventCount()}");
        Debug.Log($"未处理: {GetUnresolvedCount()}");
        Debug.Log($"全部完成: {(AreAllEventsResolved() ? "✅ 是" : "❌ 否")}");

        if (currentPageIndex >= 0 && currentPageIndex < currentNodeEvents.Count)
        {
            var current = currentNodeEvents[currentPageIndex];
            Debug.Log($"\n当前事件:");
            Debug.Log($"  标题: {current.EventData.Title}");
            Debug.Log($"  处理状态: {(current.IsResolved ? "✅ 已处理" : "⏳ 未处理")}");
            Debug.Log($"  当前选择: {(current.ChooseA ? "选项A" : "选项B")}");
        }
        Debug.Log("===============================================\n");
    }
}
