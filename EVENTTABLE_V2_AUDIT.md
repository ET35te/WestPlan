# EventTable_v2.csv 配置检查报告

**日期**：2026-01-10  
**文件**：Assets/Resources/Data/EventTable_v2.csv  
**状态**：✅ 基本完整 | ⚠️ 需要验证

---

## 📊 基本统计

| 项目 | 数值 | 说明 |
|------|------|------|
| 总行数 | 233 | 包括表头 |
| 事件数量 | 232 | 事件记录（ID 1001-14002） |
| 列数 | 15 | 完整字段 |
| 是否线性 | ✅ 是 | 所有事件都有 NextID 分支 |

---

## ✅ 字段完整性检查

### 必需列（15 列）

```
ID, IsPeaceful, Title, Context, OptA_Text, OptA_Result_Txt, OptA_Result_Data, 
NextID_A, Condition_A, OptB_Text, OptB_Result_Txt, OptB_Result_Data, 
NextID_B, Condition_B, Effect_Type
```

**验证结果**：✅ 全部存在

---

## 🎯 事件流程分析

### 起点事件
```
ID 1001: 铁流
├─ 和平事件（IsPeaceful=1）
├─ 选项 A → NextID 1002
└─ 选项 B → NextID 1002
```

**✅ 正确**：两条路都指向下一事件

### 故事分支结构

**主线路径**：1001 → 1002 → 1003 → 1004 → ... → 2000 → 2001 → ...

**关键分支点**：
- ID 2004：条件分支（belief>150 时进入 2005「夜袭」，否则 2007「固守」）
  ```csv
  2004,0,暴风雨之夜,夜深了...,整顿兵马...,进入夜袭线,ADD_RES:belief:0,2005,belief>150,严防死守...,进入固守线,ADD_RES:armor:20,2007,,
  ```
  **✅ 正确**：条件逻辑清晰

- ID 4010：路线分支（标记为「命运岔路口」）
  ```csv
  4010,1,命运的岔路口,...,路线A：死守疏勒,...,进入史实线,ADD_RES:belief:20,5000,,路线B：翻越天山,...,进入幻想线,ADD_RES:belief:20,1300,belief>200,SWITCH_ROUTE_FANTASY
  ```
  **✅ 正确**：两条路线清晰分离（信念>200 进幻想线）

- ID 5009：高难度条件
  ```csv
  5009,0,不可食用的援军,...,冒险出击：抢粮！,...,ADD_RES:grain:20,6000,belief>50,坚守不出：...,SUB_RES:belief:10,6000,,
  ```
  **✅ 正确**：条件为 belief>50

### 终点事件

**多个结局**：
- ID 12010：「血色修罞」（吃人路线）- GAME_OVER
- ID 12011：「千秋功业」（坚守路线）- GAME_OVER
- ID 13001+ ：「疯子的演讲」（幻想路线开始）

**✅ 正确**：结局设计完整

---

## 🔍 数据完整性检查

### 资源指令验证

**支持的指令类型**：

| 指令 | 用法 | 出现次数 | 状态 |
|------|------|---------|------|
| ADD_RES | `ADD_RES:belief:10` | ~100+ | ✅ 正确 |
| SUB_RES | `SUB_RES:belief:5` | ~80+ | ✅ 正确 |
| DAMAGE | `DAMAGE:100,-1` | ~5 | ✅ 正确 |
| GAME_OVER | 触发游戏结束 | 2 | ✅ 正确 |
| SWITCH_ROUTE_FANTASY | 路线切换 | 1 | ⚠️ 需验证代码实现 |

**发现问题**：
- `SWITCH_ROUTE_FANTASY` 在 ID 4010 中出现，但不确定代码是否已实现此指令

### 条件表达式检查

**示例条件**：
- `belief>150` → 检查信念大于 150
- `belief>50` → 检查信念大于 50
- `BELIEF>200` → 检查信念大于 200（混合大小写）
- `belief>30` → 检查信念大于 30
- `ARMOR>0` → 检查护甲大于 0
- `GRAIN>50` → 检查粮食大于 50

**⚠️ 问题识别**：
1. 条件表达式大小写不统一（`belief` vs `BELIEF`）
2. 复杂条件（如 AND/OR）未发现，只有简单比较

---

## 📋 关键事件检查

### 战斗触发事件

**ID 1003：第一次接触**
```csv
1003,0,第一次接触,几骑匈奴斥候出现...,下令齐射...,无损全歼,SUB_RES:grain:5,1004,,发起冲锋...,轻微战损,...,1004,,
```
**问题**：❌ 未触发战斗！只是文本事件
- 应该触发 BattleManager.StartBattle()
- 但代码中无 TRIGGER_BATTLE 指令或类似机制

**⚠️ 建议**：
添加战斗触发列，如 `Trigger_Battle_ID`：
```csv
ID,...,Trigger_Battle_ID
1003,...,2001
```

### 结局分支事件

**ID 12009：审判日（Karma）**
```csv
12009,1,审判日（Karma）,...,辩解：为了活下去,判定信念,ADD_RES:belief:0,12010,BELIEF<50,沉默：功过任人评说,判定威望,ADD_RES:belief:0,12011,BELIEF>50,
```

**验证结果**：✅ 正确
- 信念<50 → 血色修罞（ID 12010）
- 信念>50 → 千秋功业（ID 12011）
- 完美的条件分支

---

## 🐛 已发现的问题

### 问题 1：战斗触发机制缺失（🔴 严重）

**现象**：
- EventTable_v2.csv 中有多个战斗情景（ID 1003、2001 等）
- 但这些事件都是纯文本，未触发 BattleManager

**根本原因**：
- CSV 表中无 `Trigger_Battle` 或 `Battle_Enemy_ID` 列
- 代码中无解析战斗触发的逻辑

**修复建议**：
```
方案 A：添加新列到 CSV
- 添加列：`Trigger_Battle`, `Battle_Enemy_ID`
- 例：ID 1003 → Trigger_Battle=1, Battle_Enemy_ID=2001

方案 B：通过硬编码在代码中
- 在 GameManager.cs 中添加：
  if (eventID == 1003) BattleManager.StartBattle(DataManager.GetEnemyData(2001));
```

### 问题 2：SWITCH_ROUTE_FANTASY 指令未实现（🟡 中等）

**现象**：
- ID 4010 事件中出现 `SWITCH_ROUTE_FANTASY` 指令
- 不确定 DataManager 是否处理此指令

**修复建议**：
在 DataManager.cs 中查找并确保处理：
```csharp
case "SWITCH_ROUTE_FANTASY":
    // 切换到幻想路线（ID 1300 开始）
    break;
```

### 问题 3：条件表达式大小写不统一（🟡 中等）

**现象**：
- 部分条件使用 `belief>150`（小写）
- 部分条件使用 `BELIEF>200`（大小写混合）

**可能影响**：
- 条件解析失败
- 某些分支无法正确跳转

**修复建议**：
统一使用小写：`belief>150`, `armor>0`, `grain>50`

---

## ✅ 验证清单

完成下列检查以确保 EventTable_v2.csv 完全可用：

- [ ] 在游戏中运行事件系统
- [ ] 验证所有 NextID 正确跳转
- [ ] 验证所有条件表达式正常工作
- [ ] 验证资源增减生效
- [ ] 验证战斗触发机制（需要代码修改）
- [ ] 验证结局分支正确执行
- [ ] 搜索并替换所有大小写混合的条件表达式

---

## 📌 后续任务

### 立即处理

1. **确认战斗触发机制**
   - 在 GameManager.cs 中查找 `OnEventResult()` 方法
   - 检查是否已实现战斗触发逻辑
   - 如未实现，需添加

2. **统一条件表达式**
   - 搜索：`BELIEF>`, `ARMOR>`, `GRAIN>`
   - 替换为：`belief>`, `armor>`, `grain>`

### 本周处理

1. **添加战斗触发列（可选优化）**
   - 在 CSV 中添加 `Trigger_Battle`, `Battle_Enemy_ID` 列
   - 更新 DataManager 解析逻辑
   - 优点：解耦合，配置更灵活

2. **验证 SWITCH_ROUTE_FANTASY**
   - 确认代码中是否处理此指令
   - 添加测试路线切换

---

## 📄 总结

| 方面 | 状态 | 说明 |
|------|------|------|
| 表头完整性 | ✅ 完整 | 15 个字段齐全 |
| 事件数量 | ✅ 完整 | 232 个事件（起点→多结局） |
| 线性分支 | ✅ 完整 | 所有事件都有分支 |
| 资源指令 | ✅ 完整 | ADD_RES, SUB_RES 等 |
| 条件分支 | ✅ 基本完整 | 但大小写不统一 |
| 战斗触发 | ❌ 缺失 | 无触发战斗机制 |
| 代码实现 | ⚠️ 待验证 | 需确认 SWITCH_ROUTE_FANTASY |

**整体评分**：8/10 - 结构完整，但需要代码层面的完善

