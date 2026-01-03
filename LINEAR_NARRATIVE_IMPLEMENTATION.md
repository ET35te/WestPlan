# 线性分支事件系统 - 实现指南

## 概述

已完成从"随机池" → "线性叙事+分支跳转"的架构转换。本文档说明如何启用和测试新系统。

---

## 1. 核心组件清单

### 1.1 新增数据类（DataManager.cs）

```csharp
// v2事件数据结构（支持分支跳转）
public class EventData_v2
{
    public int ID;
    public bool IsPeaceful;
    public string Title;
    public string Context;
    
    // 选项A
    public string OptA_Text;
    public string OptA_Result_Txt;
    public string OptA_Result_Data;
    public int NextID_A;      // 关键：选A后跳转到哪个事件
    public string Condition_A; // 关键：选项条件
    
    // 选项B
    public string OptB_Text;
    public string OptB_Result_Txt;
    public string OptB_Result_Data;
    public int NextID_B;
    public string Condition_B;
    
    // 特效（兼容旧系统）
    public string Effect_Type;
}

// 节点开场剧情面板
public class StoryPanelData
{
    public int NodeID;
    public string Title;
    public string Content;      // 长篇背景文本
    public int FirstEventID;    // 该节点第一个事件
}

// 结局配置
public class EndingData
{
    public int EndingID;
    public string Title;
    public string Description;
    public string Condition;
}
```

### 1.2 新增系统类（ConditionEvaluator.cs）

支持条件判定：
- `BELIEF>50` - 信念大于50
- `GRAIN<20` - 粮食小于20
- `ARMOR==10` - 护甲等于10
- `BELIEF>30&GRAIN<50` - 且条件
- `BELIEF>30|GRAIN<50` - 或条件

### 1.3 新增CSV配置表

在 `Assets/Resources/Data/` 中：

1. **EventTable_v2.csv** - v2版本事件表
   - 支持 NextID_A/B 显式分支
   - 移除了概率字段（Res2_Rate）
   - 增加了条件字段（Condition_A/B）

2. **StoryPanelTable.csv** - 节点剧情面板
   - 每个节点一条记录
   - 包含开场标题和背景文本

3. **EndingTable.csv** - 结局配置
   - 配置游戏的多个结局

---

## 2. 流程对比

### 旧系统流程（随机）
```
ShowNextEvent() [随机]
  ↓
GetRandomEvent()
  ↓
显示随机事件 (IsPeaceful判断)
  ↓
玩家选择 → ResolveEventOption()
  ↓
概率判定 → Result1/Result2
  ↓
下一个随机事件
```

### 新系统流程（线性）
```
StartNodeStoryFlow()
  ↓
ShowStoryPanel() [剧情面板]
  ↓
玩家点击继续
  ↓
StartNodeEventChain(FirstEventID)
  ↓
ShowEventByID_v2(EventID)
  ↓
玩家选择 (带条件检查)
  ↓
ResolveEventOption_v2()
  ↓
检查NextID_A/B
  ↓
如果NextID==-1：结束节点，进入结算
否则：跳转到下一个事件
```

---

## 3. 启用新系统的步骤

### 步骤1：修改 GameManager.StartNewGame()

```csharp
public void StartNewGame()
{
    Debug.Log("🔄 开始新游戏：重置所有数据...");
    
    // ... 现有初始化代码 ...
    
    // 启用新系统而不是旧系统
    // UIManager.Instance.ShowNextEvent();  // 旧系统 - 注释掉
    
    GameManager.Instance.StartNodeStoryFlow();  // 新系统 - 启用
}
```

### 步骤2：测试事件链

运行游戏，预期流程：
1. 进入游戏
2. 显示节点0的剧情面板（"丝路使者"）
3. 点击继续
4. 显示事件1001（"遭遇匈奴驿卒"）
5. 选择选项A或B
6. 显示结果
7. 根据NextID跳转到事件1002或1003

### 步骤3：验证分支逻辑

在事件1001中：
- 选A → 跳转1002（战斗线）
- 选B → 跳转1003（贿赂线）

结果应该完全不同（显示不同的事件流）

---

## 4. 关键API 速查表

### GameManager

```csharp
// 启动节点剧情（调用一次per节点）
GameManager.Instance.StartNodeStoryFlow();

// 启动事件链（内部调用）
GameManager.Instance.StartNodeEventChain(firstEventID);

// 按ID显示事件
GameManager.Instance.ShowEventByID_v2(eventID);

// 处理选项（由UIManager调用）
GameManager.Instance.ResolveEventOption_v2(evt, chooseA);

// 确认结果后处理（由UIManager调用）
GameManager.Instance.ConfirmEventResult_v2();
```

### UIManager

```csharp
// 显示剧情面板
UIManager.Instance.ShowStoryPanel(panel);

// 显示v2事件UI
UIManager.Instance.ShowEventUI_v2(evt);

// 显示结果
UIManager.Instance.ShowEventResult_v2(resultText);

// 关闭剧情面板并启动事件
UIManager.Instance.CloseStoryPanelAndStartEvents();
```

### DataManager

```csharp
// 按ID获取v2事件
DataManager.Instance.GetEventByID_v2(eventID);

// 按节点ID获取剧情面板
DataManager.Instance.GetStoryPanelByNodeID(nodeID);
```

### ConditionEvaluator

```csharp
// 评估条件
bool result = ConditionEvaluator.Evaluate("BELIEF>50", ResourceManager.Instance);

// 调试输出
ConditionEvaluator.DebugEvaluate("BELIEF>50&GRAIN<20", ResourceManager.Instance);
```

---

## 5. CSV 表格格式详解

### EventTable_v2.csv

| 字段 | 类型 | 说明 | 例子 |
|------|------|------|------|
| ID | int | 事件唯一ID | 1001 |
| IsPeaceful | bool | 是否为非战斗事件 | 1（true） |
| Title | string | 事件标题 | 遭遇匈奴驿卒 |
| Context | string | 事件背景描述 | 前路遭遇来自西域的... |
| OptA_Text | string | 选项A的按钮文本 | 选择战斗 |
| OptA_Result_Txt | string | 选项A的结果文本 | 士兵奋勇迎战 |
| OptA_Result_Data | string | 选项A的资源数据 | DAMAGE:30\|ADD_RES:belief:10 |
| NextID_A | int | 选A后跳转的事件 | 1002 |
| Condition_A | string | 选项A的条件 | BELIEF>20 |
| OptB_Text | string | 选项B的按钮文本 | 选择贿赂 |
| ... | ... | 选项B的其他字段 | ... |
| Effect_Type | string | 特殊效果（保留兼容） | BATTLE:104 |

**资源数据格式示例**：
- `DAMAGE:30` - 造成30点伤害
- `ADD_RES:belief:20` - 增加20点信念
- `SUB_RES:grain:10` - 减少10点粮食
- `DAMAGE:30|ADD_RES:belief:10` - 组合效果（用|分隔）

**特殊的NextID值**：
- `-1` - 表示该选项导致"节点结束"，进入结算

**条件格式示例**：
- `BELIEF>50` - 信念>50
- `GRAIN<20` - 粮食<20
- `ARMOR==10` - 护甲==10
- `BELIEF>30&GRAIN<50` - 信念>30 且 粮食<50
- `BELIEF>30|GRAIN<50` - 信念>30 或 粮食<50

### StoryPanelTable.csv

| 字段 | 类型 | 说明 |
|------|------|------|
| NodeID | int | 节点索引（0-11对应12个地点） |
| Title | string | 章节标题 |
| Content | string | 长篇背景文本 |
| FirstEventID | int | 该节点首个互动事件ID |

### EndingTable.csv

| 字段 | 类型 | 说明 |
|------|------|------|
| EndingID | int | 结局ID |
| Title | string | 结局标题 |
| Description | string | 结局描述 |
| Condition | string | 触发条件 |

---

## 6. 现有测试数据

### EventTable_v2.csv 已包含的事件

- **1001**: 遭遇匈奴驿卒
  - 选A → 1002（战斗）
  - 选B → 1003（贿赂）

- **1002**: 激烈的战斗
  - 选A → 1004（继续战斗）
  - 选B → 1005（撤退逃离）

- **1003**: 权衡之后
  - 选A → 1004（继续前进）
  - 选B → 9999（放弃西行 - 失败）

- **1004**: 抵达楼兰城（节点结束）
  - 选A/B → -1（结束节点）

- **1005**: 全身而退
  - 选A → -1（继续西行结束）
  - 选B → 9999（失败结局）

- **2001**: 楼兰城的秘密
  - 选A → 2002
  - 选B → 2002

- **2002**: 秘密泄露（节点结束）
  - 选A/B → -1

### StoryPanelTable.csv 已包含的节点

- Node 0: 丝路使者 → FirstEventID: 1001
- Node 1: 楼兰重镇 → FirstEventID: 2001
- Node 2: 西海之滨 → FirstEventID: 3001
- Node 3: 匈奴汗帐 → FirstEventID: 4001

---

## 7. 验收检查清单

- [ ] DataManager 成功加载 EventTable_v2.csv
- [ ] DataManager 成功加载 StoryPanelTable.csv
- [ ] DataManager 成功加载 EndingTable.csv
- [ ] ConditionEvaluator 正常评估条件
- [ ] GameManager.StartNodeStoryFlow() 显示剧情面板
- [ ] 点击继续后出现首个事件
- [ ] 选项A和选项B导向不同的下一个事件
- [ ] 资源变化正确应用
- [ ] 条件不符时选项按钮置灰
- [ ] NextID=-1 时正确进入节点结算
- [ ] 多次重玩同一节点，同样的选择导致同样的事件流

---

## 8. 常见问题排查

### 问题：找不到CSV表

**解决**：确保CSV文件位置正确
- EventTable_v2.csv → `Assets/Resources/Data/EventTable_v2.csv`
- StoryPanelTable.csv → `Assets/Resources/Data/StoryPanelTable.csv`
- EndingTable.csv → `Assets/Resources/Data/EndingTable.csv`

### 问题：事件ID不存在

**解决**：检查CSV中的ID是否与代码中引用的ID一致

### 问题：条件判定失败

**解决**：检查条件格式，确保资源名称大小写正确（BELIEF/GRAIN/ARMOR）

### 问题：选项按钮不响应

**解决**：确保按钮条件判定通过（Condition字段为空或条件评估为true）

---

## 9. 后续扩展建议

### 优先级1（必需）
- 填充更多事件数据（12节点×3+事件/节点）
- 设计完整的12节点剧情线

### 优先级2（推荐）
- 实现战斗胜利后的自动跳转
- 添加"事件标志"系统（标记已触发的特殊事件）
- 实现复杂的条件判定（如"曾经选择过XXX"）

### 优先级3（优化）
- 编写事件编辑器UI（无需手动改CSV）
- 可视化事件流图
- 性能优化（事件预加载等）

---

**实现状态**：✅ **核心架构完成，ready for content填充**

