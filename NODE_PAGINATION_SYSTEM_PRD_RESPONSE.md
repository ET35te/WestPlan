# 📑 节点分页式事件系统 - 完整实现方案

**PRD 版本**：v1.0  
**实现优先级**：🔴 **核心改造**  
**工作量估算**：150-200 小时  
**时间线**：2 周（假设全职）

---

## 0. 快速概览

### 当前系统（待废弃）
```
垂直列表显示 → 滚动查看所有事件 → 随机选择事件处理
                                      ↓
                                   资源修改
                                      ↓
                                   下一事件
```

**问题**：
- ❌ 信息过载，不知道还有多少事件
- ❌ 无法预览其他事件就做决策
- ❌ 被动接受事件顺序

### 新系统（目标）
```
卡片容器（单事件）
    ├─ 左/右翻页（< >）  ← 主导航
    ├─ 进度条 (2/5)      ← 进度感知
    ├─ 标题 + 描述
    ├─ 选项 A & B（可见资源预告）
    └─ 状态标记（未处理/✅已完成）
    
底部：
    └─ [出发] 按钮（全部完成时亮起）
```

**优势**：
- ✅ 一次一个事件，聚焦不分散
- ✅ 翻页预览，自由决定处理顺序
- ✅ 实时进度条，清晰完成度
- ✅ 防呆设计，资源不足自动置灰

---

## 1. 核心数据结构设计

### 1.1 节点事件池管理器（新类）

**类名**：`NodeEventPoolManager`（或 `EventPageController`）

```csharp
public class NodeEventPoolManager : MonoBehaviour
{
    // ========== 数据 ==========
    [System.Serializable]
    public struct EventPageData
    {
        public DataManager.EventData_v2 EventData;    // 事件本体
        public bool IsResolved;                        // 是否已处理
        public bool ChooseA;                           // 选择结果（如果已处理）
    }

    // 当前节点的所有事件
    private List<EventPageData> currentNodeEvents = new List<EventPageData>();
    
    // 当前显示的事件页索引
    private int currentPageIndex = 0;
    
    // ========== 查询方法 ==========
    
    /// <summary>获取当前显示的事件</summary>
    public EventPageData GetCurrentEvent() => currentNodeEvents[currentPageIndex];
    
    /// <summary>获取指定索引的事件</summary>
    public EventPageData GetEventAt(int index) => currentNodeEvents[index];
    
    /// <summary>获取总事件数</summary>
    public int GetTotalEventCount() => currentNodeEvents.Count;
    
    /// <summary>获取已解决的事件数</summary>
    public int GetResolvedCount() => currentNodeEvents.Count(e => e.IsResolved);
    
    /// <summary>检查是否全部处理完毕</summary>
    public bool AreAllEventsResolved() => GetResolvedCount() == GetTotalEventCount();
    
    // ========== 导航方法 ==========
    
    /// <summary>跳转到指定页面</summary>
    public bool GoToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= currentNodeEvents.Count) return false;
        currentPageIndex = pageIndex;
        return true;
    }
    
    /// <summary>下一页（返回是否成功）</summary>
    public bool NextPage()
    {
        if (currentPageIndex + 1 < currentNodeEvents.Count)
        {
            currentPageIndex++;
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
                return true;
            }
        }
        return false;
    }
    
    // ========== 修改方法 ==========
    
    /// <summary>标记当前事件为已处理</summary>
    public void ResolveCurrentEvent(bool chooseA)
    {
        var evt = currentNodeEvents[currentPageIndex];
        evt.IsResolved = true;
        evt.ChooseA = chooseA;
        currentNodeEvents[currentPageIndex] = evt;
        
        Debug.Log($"✅ 事件 {currentPageIndex+1}/{currentNodeEvents.Count} 已处理");
    }
    
    /// <summary>初始化节点事件池（从事件链解析获得所有事件）</summary>
    public void InitializeNodeEvents(List<int> eventIDs)
    {
        currentNodeEvents.Clear();
        currentPageIndex = 0;
        
        foreach (int id in eventIDs)
        {
            var evt = DataManager.Instance.GetEventByID_v2(id);
            if (evt != null)
            {
                currentNodeEvents.Add(new EventPageData
                {
                    EventData = evt,
                    IsResolved = false,
                    ChooseA = false
                });
            }
        }
        
        Debug.Log($"📋 初始化节点事件池：{currentNodeEvents.Count} 个事件");
    }
}
```

### 1.2 DataManager 扩展

**现有问题**：事件系统是线性链（1→2→3），无法一次性获取节点所有事件

**解决方案**：添加方法遍历事件链并收集所有事件 ID

```csharp
// 在 DataManager 中添加
public List<int> GetNodeEventChain(int firstEventID)
{
    List<int> eventChain = new List<int>();
    int currentID = firstEventID;
    
    while (currentID > 0)
    {
        eventChain.Add(currentID);
        var evt = GetEventByID_v2(currentID);
        if (evt == null) break;
        
        // 检查是否是最后一个事件（NextID_A 或 NextID_B 为 -1）
        // 为了简单起见，假设所有选项都指向相同的下一个事件（线性）
        // 如果有分支，需要特殊处理
        
        int nextID = evt.NextID_A;  // 假设 A 路线为主路线
        if (nextID == evt.NextID_B)
        {
            currentID = nextID;
        }
        else
        {
            // 有分支，需要决策
            break;  // 暂时中止，等待玩家选择
        }
    }
    
    return eventChain;
}
```

---

## 2. UI 布局设计（Canvas 结构）

### 2.1 层级结构

```
Canvas
├─ EventPageContainer (主事件卡片容器)
│  ├─ Background (半透明背景)
│  ├─ ProgressBar (顶部进度条)
│  │  ├─ ProgressBar_Fill
│  │  └─ ProgressText (如 "2/5")
│  ├─ TitleText (事件标题)
│  ├─ DescriptionText (事件描述)
│  ├─ OptionsContainer
│  │  ├─ OptionA_Button
│  │  │  ├─ Text (选项文本)
│  │  │  └─ ResourceTag (如 "粮-5")
│  │  └─ OptionB_Button
│  │     ├─ Text
│  │     └─ ResourceTag
│  ├─ StatusBadge (✅ 已完成 标记，处理过的事件显示)
│  ├─ NavigationContainer
│  │  ├─ PrevButton (<)
│  │  └─ NextButton (>)
│
└─ BottomBar
   └─ PrepareButton ([整备完毕] 或 [出发])
       └─ Tooltip (鼠标悬停时提示"还有 2 个事件未处理")
```

### 2.2 按钮状态设计

| 按钮 | 初始状态 | 条件 | 外观 |
|------|---------|------|------|
| PrevButton | 禁用 | 当前页 > 0 时启用 | 绿色亮 / 灰色 |
| NextButton | 启用 | 当前页 < 总页数-1 时启用 | 绿色亮 / 灰色 |
| OptionA/B | 启用或置灰 | 资源足够时启用，不足时置灰 | 绿色 / 灰色 |
| PrepareButton | 禁用 | 所有事件解决时亮起 | 亮黄 / 灰 |

---

## 3. 交互流程详设

### 3.1 页面加载流程

```csharp
GameManager.StartNodeStoryFlow()
    ↓
获取节点首个事件ID（从 StoryPanel）
    ↓
NodeEventPoolManager.InitializeNodeEvents(eventIDs)
    ├─ 遍历事件链，收集所有事件 ID
    └─ 创建 EventPageData 列表
    
    ↓
UIManager.ShowEventPageUI()
    ├─ 渲染第 1 页事件
    ├─ 更新进度条 (1/N)
    ├─ 启用/禁用导航按钮
    └─ 绑定翻页和选项回调
```

### 3.2 翻页交互

```csharp
// 玩家点击 NextButton
PrevButton.onClick → OnPrevButtonClicked()
    ├─ manager.PreviousPage()
    └─ RefreshCurrentEventUI()

NextButton.onClick → OnNextButtonClicked()
    ├─ manager.NextPage()
    └─ RefreshCurrentEventUI()
    
RefreshCurrentEventUI()
    ├─ 获取当前事件 = manager.GetCurrentEvent()
    ├─ 更新标题/描述
    ├─ 更新进度条 (X/N)
    ├─ 如果已处理，显示 ✅ 并禁用选项
    ├─ 如果未处理，显示选项并启用
    └─ 检查选项资源是否足够（置灰不足的）
```

### 3.3 选项点击 → 处理 → 反馈

```
玩家点击 OptionA
    ↓ PlayAudio + Punch 缩放动效
    
UIManager.ShowEventResult_v2(resultText)
    ↓ 显示结果面板
    
玩家点击 [确认]
    ↓
GameManager.ConfirmEventResult_v2()
    ├─ 应用资源变化
    └─ manager.ResolveCurrentEvent(true)
    
    ↓
回到 EventPageUI
    ├─ 标记当前页为 ✅ 已完成
    ├─ 禁用选项按钮
    ├─ 自动跳转到下一个未处理的事件（可选）
    └─ 如果全部完成，高亮 [出发] 按钮
```

---

## 4. 防呆设计细节

### 4.1 资源不足处理

```csharp
// 在显示事件时检查资源
private void CheckAndDisableInsufficientOptions(EventPageData eventData)
{
    // 解析 OptA_Result_Data，检查是否足够
    bool canChooseA = CanAfforOption(eventData.EventData, true);
    bool canChooseB = CanAfforOption(eventData.EventData, false);
    
    OptionA_Button.interactable = canChooseA;
    OptionB_Button.interactable = canChooseB;
    
    if (!canChooseA) OptionA_Button.color = Color.gray;
    if (!canChooseB) OptionB_Button.color = Color.gray;
}

private bool CanAffordOption(DataManager.EventData_v2 evt, bool chooseA)
{
    string dataStr = chooseA ? evt.OptA_Result_Data : evt.OptB_Result_Data;
    // 解析 dataStr，逐项检查资源是否足够
    // 如果所有必要资源都足够，返回 true
    // 伪代码：
    // foreach resource in Parse(dataStr)
    //     if CurrentResource[resource.id] + resource.delta < 0
    //         return false
    // return true
}
```

### 4.2 未完成时点击 [出发] 按钮

```csharp
PrepareButton.onClick += () =>
{
    if (!manager.AreAllEventsResolved())
    {
        // 拒绝反馈：震动
        PlayErrorShake(PrepareButton);
        
        // 提示
        int unresolvedCount = manager.GetTotalEventCount() - manager.GetResolvedCount();
        ShowToast($"❌ 还有 {unresolvedCount} 个事务未处理");
        
        return;
    }
    
    // 正常出发
    GameManager.Instance.GoToNextNode();
};
```

### 4.3 资源不足的选项被点击

```csharp
OptionA_Button.onClick += () =>
{
    if (!OptionA_Button.interactable)
    {
        PlayErrorShake(OptionA_Button);
        ShowToast("❌ 资源不足，无法选择此选项");
        return;
    }
    
    // 正常处理
    OnOptionSelected(true);
};
```

---

## 5. 动效与 UI Juice

### 5.1 翻页动效

```csharp
// 使用 DOTween（需要安装）
private IEnumerator TransitionToPage(int newPageIndex)
{
    // 淡出
    eventCardContainer.DOFade(0.3f, 0.2f);
    
    // 侧滑（可选）
    eventCardContainer.transform.DOLocalMoveX(-100, 0.2f);
    
    yield return new WaitForSeconds(0.2f);
    
    // 更新内容
    currentPageIndex = newPageIndex;
    RefreshCurrentEventUI();
    
    // 淡入
    eventCardContainer.DOFade(1f, 0.2f);
    eventCardContainer.transform.DOLocalMoveX(0, 0.2f);
}
```

### 5.2 选项点击反馈（Punch Scale）

```csharp
private void OnOptionClicked(Button button)
{
    // 点击瞬间缩放
    button.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f);
    
    // 播放音效
    AudioManager.Play("UI_Click");
    
    // 禁用按钮防止重复点击
    button.interactable = false;
    
    // 延迟执行逻辑
    StartCoroutine(DelayedOptionResolution(button, true));
}

private IEnumerator DelayedOptionResolution(Button button, bool isOptionA)
{
    yield return new WaitForSeconds(0.2f);
    GameManager.Instance.ResolveEventOption_v2(manager.GetCurrentEvent().EventData, isOptionA);
}
```

### 5.3 已完成反馈（Seal Stamp）

```csharp
private void OnEventResolved()
{
    // 卡片震动（Thud 感）
    eventCardContainer.DOShakePosition(0.3f, new Vector3(0, 5, 0), 10, 90);
    
    // 播放盖章动画（序列帧或简单 Sprite 闪烁）
    PlayStampAnimation();
    
    // 显示 ✅ 标记
    statusBadge.SetActive(true);
    statusBadge.transform.DOScale(1f, 0.2f).From(Vector3.zero);
    
    // 禁用选项按钮
    OptionA_Button.interactable = false;
    OptionB_Button.interactable = false;
}

private void PlayStampAnimation()
{
    // 可以使用 DOTween 的 DORotate 模拟旋转
    stampImage.transform.DORotate(new Vector3(0, 0, -15), 0.1f)
        .SetLoops(2, LoopType.Yoyo);
}
```

### 5.4 拒绝反馈（Error Shake）

```csharp
private void PlayErrorShake(RectTransform target)
{
    target.DOShakePosition(0.3f, new Vector3(10, 0, 0), 5, 90);
    AudioManager.Play("UI_Error");
}
```

---

## 6. 从线性事件链到事件池的转换

### 当前问题

目前 EventTable_v2.csv 中的事件是线性链形式：
```
1003 → 1004 → 1005 → 1006 → ...（带分支）
```

### 解决方案

**方案 A：CSVv2.5（推荐）**

在 CSV 中添加新列 `NodeID`，标记哪些事件属于同一节点：

```csv
ID,NodeID,IsPeaceful,Title,Context,OptA_Text,...,NextID_A,NextID_B,Effect_Type
1003,1,0,第一次接触,...,下令齐射,...,1004,1004,
1004,1,1,尸体与口袋,...,允许掠夺,...,1005,1005,
1005,1,0,伊吾的城墙,...,正面强攻,...,1006,1006,
1006,1,1,庆功宴,...,加入狂欢,...,1007,1007,
2000,2,0,数学的恐怖,...,发表演讲,...,2001,2001,
2001,2,0,沸腾的金属,...,下令涂抹,...,2002,2002,
...
```

然后在 DataManager 中添加：
```csharp
public List<int> GetNodeEventIDs(int nodeID)
{
    return AllEvents_v2
        .Where(e => e.NodeID == nodeID)
        .Select(e => e.ID)
        .OrderBy(id => id)  // 按 ID 排序，保持顺序
        .ToList();
}
```

**方案 B：硬编码映射表（快速但不优雅）**

在 GameManager 中添加：
```csharp
private Dictionary<int, List<int>> NodeEventMap = new Dictionary<int, List<int>>
{
    { 0, new List<int> { 1003, 1004, 1005, 1006 } },      // 玉门关
    { 1, new List<int> { 2000, 2001, 2002, ... } },       // 白龙堆
    // ... 其他节点
};
```

### 推荐流程

```csharp
// 在 GameManager.StartNodeStoryFlow() 中改为
public void StartNodeStoryFlow()
{
    // 获取该节点的所有事件 ID
    List<int> nodeEventIDs = DataManager.Instance.GetNodeEventIDs(CurrentNodeIndex);
    
    if (nodeEventIDs.Count == 0)
    {
        Debug.LogWarning("该节点无事件，直接结算");
        TriggerSettlement();
        return;
    }
    
    // 初始化事件页面管理器
    eventPoolManager.InitializeNodeEvents(nodeEventIDs);
    
    // 显示第一个事件
    UIManager.Instance.ShowEventPageUI(eventPoolManager);
}
```

---

## 7. 实现的工作拆分表

| 优先级 | 任务 | 时间 | 依赖 | 状态 |
|--------|------|------|------|------|
| P0 | 创建 `NodeEventPoolManager` 类 | 4h | 无 | ⏳ |
| P0 | 修改 DataManager，支持节点事件查询 | 2h | P0 |  ⏳ |
| P0 | 改造 EventPageUI（Canvas 结构） | 8h | 无 | ⏳ |
| P0 | 翻页逻辑 + 刷新 UI | 4h | P0, P0 | ⏳ |
| P1 | 防呆设计（资源检查、置灰） | 6h | P0 | ⏳ |
| P1 | 动效实现（翻页、选项、完成） | 8h | P0 | ⏳ |
| P1 | 从线性链到事件池的转换 | 4h | P0 | ⏳ |
| P2 | 单元测试 | 6h | 所有 | ⏳ |
| P2 | 微调 + 性能优化 | 4h | 所有 | ⏳ |

**总工作量**：46+ 小时

---

## 8. 快速实现检查清单

### Phase 1：核心框架（1 周）
- [ ] 创建 `NodeEventPoolManager.cs`
- [ ] 在 DataManager 中添加节点查询方法
- [ ] 改造 GameManager 的 `StartNodeStoryFlow()`
- [ ] 创建/修改 EventPageUI Canvas 结构

### Phase 2：交互逻辑（3-4 天）
- [ ] 实现翻页逻辑（NextPage/PreviousPage）
- [ ] 实现选项处理与状态同步
- [ ] 实现进度条更新
- [ ] 实现防呆设计（资源检查）

### Phase 3：动效与精打磨（3-4 天）
- [ ] 实现翻页动效（淡入淡出/侧滑）
- [ ] 实现选项点击反馈（Punch Scale）
- [ ] 实现已完成反馈（Thud + 盖章）
- [ ] 实现拒绝反馈（Shake）

### Phase 4：测试与集成（2-3 天）
- [ ] 单元测试
- [ ] 集成测试（与 GameManager、ResourceManager 协作）
- [ ] UI 微调
- [ ] 性能优化

---

## 9. 技术栈需求

### 必需
- ✅ Unity 2020 LTS 或以上
- ✅ C# 7.3+
- ✅ TextMeshPro（用于文本显示）

### 推荐
- ⭐⭐⭐ **DOTween Pro**（用于动效）
- ⭐⭐ **EventSystem**（Unity 原生）
- ⭐ **AudioManager**（用于 UI 音效）

### 如无 DOTween，可用替代方案
```csharp
// 简单的 Punch Scale 协程实现
public IEnumerator PunchScale(Transform target, Vector3 punch, float duration)
{
    Vector3 originalScale = target.localScale;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        // 使用 AnimationCurve 模拟缓动
        float scale = Mathf.Lerp(1f, 1f + punch.x, Mathf.Sin(t * Mathf.PI));
        target.localScale = originalScale * scale;
        
        yield return null;
    }
    
    target.localScale = originalScale;
}
```

---

## 10. 常见问题 & 解决方案

### Q: 如何处理分支选项（同一事件有两个不同的 NextID）？
A: 暂时跳过分支，只收集线性路径上的事件。分支由玩家选择时决定。

### Q: 如果玩家在翻页时改变主意，后续想改变之前的选择怎么办？
A: 一旦事件被标记为 `Resolved`，就不允许再修改。如果需要支持"撤销"，需要额外的 UI 和逻辑。

### Q: 进度条应该只显示本节点事件，还是全局进度？
A: 建议只显示本节点（如 2/5），更清晰地反映当前目标。

### Q: 动效会不会导致性能问题？
A: 不会。简单的淡入淡出和缩放对现代设备无压力。可以通过 `DOTween` 的 `pooling` 功能进一步优化。

---

## 下一步行动

1. **确认需求**：与 PM 确认上述设计是否符合预期
2. **选择技术方案**：决定是用 DOTween 还是协程
3. **设计 UI 原型**：在 Figma/画图板上快速画出 UI 布局
4. **启动 Phase 1**：创建核心类和 Canvas 结构

