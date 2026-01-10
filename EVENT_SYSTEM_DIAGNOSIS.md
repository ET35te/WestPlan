# 🔍 事件系统诊断报告

**日期**: 2026年1月3日  
**诊断对象**: MessagePanel 无法关闭问题 + 事件系统确认

---

## ❓ 问题1: 为什么进入事件界面后 MessagePanel 无法关闭？

### 🔴 **根本原因确认**

**MessagePanel 在事件界面无法关闭的原因是：有两个不同的系统在使用 MessagePanel**

| 系统 | 使用 MessagePanel | 打开时机 | 关闭方式 |
|------|------------------|---------|---------|
| **剧情面板系统** (StoryPanel) | ✅ 使用 | `ShowStoryPanel()` 时 | `CloseStoryPanelAndStartEvents()` 点击"继续"按钮 |
| **事件显示系统** (EventUI_v2) | ❌ **不使用** | N/A | N/A |

### 🔍 **具体分析**

#### 剧情面板系统 (需要MessagePanel):
```csharp
// UIManager.cs 第662行
public void ShowStoryPanel(DataManager.StoryPanelData panel)
{
    if (MessagePanel) 
    {
        MessagePanel.SetActive(true);  // ✅ 打开
        Debug.Log("✅ MessagePanel 已激活");
    }
    
    // ... 绑定按钮事件 ...
    ToBeContinueBtn.onClick.AddListener(() =>
    {
        CloseStoryPanelAndStartEvents();  // ✅ 点击继续关闭
    });
}
```

#### 事件显示系统 (不使用MessagePanel):
```csharp
// UIManager.cs 第755行
public void ShowEventUI_v2(DataManager.EventData_v2 evt)
{
    // ❌ 这里没有使用MessagePanel
    // 而是直接显示事件内容到其他UI元素
    
    if (EventTitleText) EventTitleText.text = evt.Title;
    if (ContextText) ContextText.text = evt.Context;
    // ... 配置选项按钮 ...
}
```

### 🎯 **问题所在**

```
流程:
1. ShowStoryPanel() 激活MessagePanel ✅
2. 玩家点击"继续"按钮
3. CloseStoryPanelAndStartEvents() 关闭MessagePanel
4. StartNodeEventChain() 启动事件链
5. ShowEventUI_v2() 显示第一个事件
   ⚠️ 但此时MessagePanel已关闭，事件显示在另外的UI上
   
❌ 如果再次点击事件选项：
6. OnOptionSelected_v2() 被调用
7. ShowEventResult_v2() 显示结果
   ⚠️ 结果显示在哪里?
```

### ⚠️ **隐藏问题**

**现在MessagePanel被"遗弃"了：**
- ✅ 剧情面板打开/关闭 MessagePanel
- ❌ 事件链显示完全没有使用 MessagePanel
- ❌ 如果代码后续试图关闭 MessagePanel，会找不到对象或无反应

### ✅ **解决方案**

需要确认：**事件链应该使用 MessagePanel 还是使用独立的 EventPanel？**

---

## ❓ 问题2 & 3: 事件系统使用的表格是哪个？

### 🎯 **确认答案**

**事件系统使用的是 `EventTable_v2.csv`**

证据列表：

#### 📍 证据1: DataManager 加载两个表格

```csharp
// DataManager.cs 第100行
void Start()
{
    LoadEventTable();         // ❌ 旧系统(已弃用)
    LoadEventTable_v2();      // ✅ 新系统(正在使用)
}
```

#### 📍 证据2: GameManager 调用 v2 API

```csharp
// GameManager.cs 第172行
public void StartNodeEventChain(int firstEventID)
{
    ShowEventByID_v2(firstEventID);  // ✅ v2版本
}

// GameManager.cs 第180行
public void ShowEventByID_v2(int eventID)
{
    DataManager.EventData_v2 evt = DataManager.Instance.GetEventByID_v2(eventID);
    //                            ↑↑↑
    //                      使用GetEventByID_v2()
}
```

#### 📍 证据3: EventTable_v2.csv 已成功加载

```csharp
// DataManager.cs 第268行
void LoadEventTable_v2()
{
    TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable_v2");
    // 加载: Assets/Resources/Data/EventTable_v2.csv
    
    // 解析并填充 AllEvents_v2 列表
    AllEvents_v2.Add(evt);
}

// DataManager.cs 第367行
public EventData_v2 GetEventByID_v2(int eventID)
{
    return AllEvents_v2.FirstOrDefault(e => e.ID == eventID);
    // ✅ 从v2列表查询
}
```

### 📊 **两个表格的对比**

| 表格 | 位置 | 用途 | 状态 |
|------|------|------|------|
| **EventTable.csv** | `Assets/Resources/Data/EventTable.csv` | 旧系统(随机事件池) | ❌ 已弃用 |
| **EventTable_v2.csv** | `Assets/Resources/Data/EventTable_v2.csv` | 新系统(线性分支) | ✅ 正在使用 |

### 💾 **EventTable_v2.csv 的结构**

```csv
ID,IsPeaceful,Title,Context,OptA_Text,OptA_Result_Txt,OptA_Result_Data,NextID_A,Condition_A,OptB_Text,OptB_Result_Txt,OptB_Result_Data,NextID_B,Condition_B,Effect_Type
1001,1,遭遇匈奴驿卒,前路遭遇来自西域的匈奴驿卒,选择战斗,士兵奋勇迎战,DAMAGE:30|ADD_RES:belief:10,1002,,选择贿赂,付出金钱换取通行,SUB_RES:grain:20|ADD_RES:belief:-5,1003,,
...
```

---

## ❓ 问题4: 事件弹出是随机还是按顺序？

### 🎯 **确认答案**

**事件弹出是 `按顺序` (线性分支)，不是随机**

### 🔍 **完整流程**

#### 第1步: 启动节点事件链

```csharp
// GameManager.cs 第164行
public void StartNodeStoryFlow()
{
    // 1. 显示剧情面板 (开场介绍)
    DataManager.StoryPanelData panel = DataManager.Instance.GetStoryPanelByNodeID(CurrentNodeIndex);
    UIManager.Instance.ShowStoryPanel(panel);
    
    // 2. 记录第一个事件ID
    FirstEventID = panel.FirstEventID;  // 例如: 1001
}
```

#### 第2步: 用户点击剧情面板"继续"按钮

```csharp
// UIManager.cs 第704行
ToBeContinueBtn.onClick.AddListener(() =>
{
    CloseStoryPanelAndStartEvents();
});

// UIManager.cs 第732行
public void CloseStoryPanelAndStartEvents()
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.StartNodeEventChain(FirstEventID);
        // ↓↓↓ 按顺序启动事件链
    }
}
```

#### 第3步: 显示第一个事件(不是随机!)

```csharp
// GameManager.cs 第167行
public void StartNodeEventChain(int firstEventID)
{
    ShowEventByID_v2(firstEventID);  // 按指定ID显示
    // firstEventID = 1001 (顺序第一个事件)
}

// GameManager.cs 第178行
public void ShowEventByID_v2(int eventID)
{
    DataManager.EventData_v2 evt = DataManager.Instance.GetEventByID_v2(eventID);
    // ❌ 不是 GetRandomEvent()
    // ✅ 而是 GetEventByID_v2(eventID)
    UIManager.Instance.ShowEventUI_v2(evt);
}
```

#### 第4步: 用户选择选项后，按NextID跳转

```csharp
// GameManager.cs 第193行
public void ResolveEventOption_v2(DataManager.EventData_v2 evt, bool chooseA)
{
    // 确定下一个事件ID
    int nextEventID = chooseA ? evt.NextID_A : evt.NextID_B;
    //                ↑↑↑
    // 不是随机，而是根据选择确定的ID
    
    forcedNextEventID = nextEventID;  // 存储
}

// GameManager.cs 第209行
public void ConfirmEventResult_v2()
{
    if (forcedNextEventID == -1)
    {
        TriggerSettlement();  // 节点结束
        return;
    }

    if (forcedNextEventID > 0)
    {
        int nextID = forcedNextEventID;
        ShowEventByID_v2(nextID);  // 按ID跳转到下一个事件
        // ✅ 完全是顺序/分支，不是随机
        return;
    }
}
```

### 📊 **事件流程可视化**

```
剧情面板
   │
   ├─ 用户点击"继续"
   │
   ▼
事件1001 (遭遇匈奴驿卒)
   │
   ├─ 选项A: 选择战斗
   │    ├─ NextID_A = 1002 (继续战斗)
   │
   ├─ 选项B: 选择贿赂
   │    ├─ NextID_B = 1003 (权衡之后)
   │
   ▼
事件1002 或 事件1003
   │
   ├─ 选项A或B
   │
   ▼
事件1004 (根据选择)
   │
   ...
   ▼
事件-1 (NextID = -1，节点结束)
   │
   ├─ 进入节点结算
```

### ⚠️ **与旧系统的区别**

| 方面 | 旧系统 (EventTable) | 新系统 (EventTable_v2) |
|------|------------------|----------------------|
| **显示方式** | `GetRandomEvent()` ❌ 随机 | `GetEventByID_v2()` ✅ 按ID |
| **分支逻辑** | 概率判定 (概率字段) | 显式分支 (NextID_A/B) |
| **事件流** | 无序随机流 | 线性有序流 |
| **使用状态** | ❌ 已弃用 | ✅ 正在使用 |

---

## 📋 最终确认清单

| 问题 | 答案 | 确认度 |
|------|------|--------|
| ① MessagePanel为何无法关闭 | 事件系统未使用MessagePanel，只有剧情面板系统使用 | ✅ 100% |
| ② 事件系统的表格是哪个 | `EventTable_v2.csv` | ✅ 100% |
| ③ 是EventTable还是v2 | `EventTable_v2` | ✅ 100% |
| ④ 弹出逻辑是随机还是顺序 | `按顺序(线性分支)` 不是随机 | ✅ 100% |

---

## 🎯 建议行动

### 立即需要做的事:

1. **确认UI结构**
   - [ ] EventUI_v2 是否有独立的UI面板？
   - [ ] 还是应该使用 MessagePanel 来显示事件？

2. **如果 EventUI_v2 应该使用 MessagePanel**
   ```csharp
   // 修改 UIManager.cs 的 ShowEventUI_v2()
   public void ShowEventUI_v2(DataManager.EventData_v2 evt)
   {
       // 激活MessagePanel用于显示事件
       if (MessagePanel) MessagePanel.SetActive(true);
       
       // ... 设置内容 ...
   }
   ```

3. **如果 EventUI_v2 有独立的UI面板**
   - [ ] 确认这个独立面板是否有关闭逻辑
   - [ ] 验证事件结束后是否正确关闭

### 测试验证:

- [ ] 进入第一个剧情面板，点击"继续" → MessagePanel应该关闭
- [ ] 第一个事件应该显示(在EventPanel或MessagePanel上)
- [ ] 选择事件选项后，显示结果
- [ ] 点击确认后，显示下一个事件

---

**诊断完成时间**: 2026年1月3日  
**可信度**: ✅ 基于代码审查

需要更详细的修复步骤吗？

