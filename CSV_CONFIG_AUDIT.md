# 📋 CSV 配置检查报告

**生成日期**：2026-01-10  
**检查范围**：9 个数据表  
**状态**：🟡 需要修复

---

## ⚠️ 发现的问题

### 1. **GlobalConfig.csv** - 🔴 严重问题

**当前内容**：
```csv
Player_Start_Food,Player+Start_Armor,Enemy_Start_Food,Enemy_Start_Armor,Turn_Regen_Food,Turn_Regen_Armor,Defend_Mitigation,Attack_Base_Mult
1,1,0,0,1,1,5,1
```

**问题**：
- ❌ 列名 `Player+Start_Armor` 有 `+` 符号，应为 `Player_Start_Armor`
- ❌ 值与代码不匹配：
  - 代码中 `Turn_Regen_Food = +2`（每回合）
  - 表中为 `1`（错误）
  - 代码中 `Defend_Mitigation = 5` 但无对应列
- ❌ 缺少关键参数：
  - `FleeBeliefPenalty` (目前硬编码为 5)
  - `VictoryLootFood`
  - `VictoryLootArmor`

**修复建议**：
```csv
Player_Start_Food,Player_Start_Armor,Enemy_Start_Food,Enemy_Start_Armor,Turn_Regen_Food,Turn_Regen_Armor,Defend_Mitigation_Rate,Attack_Base_Damage,Flee_Belief_Penalty,Victory_Loot_Food,Victory_Loot_Armor
1,1,0,0,2,1,0.5,5,5,2,1
```

---

### 2. **EnemyTable.csv** - 🟡 需要扩展

**当前内容**：
```csv
EnemyID,Name,Power,Description,Intent_Pattern
2001,杂虏骑兵,15,高攻低血型敌人 善于突击,A,A,D,A,N,A,A
2002,匈奴重甲,20,高防低攻型敌人 防线坚固,D,D,A,D,D,D,A
```

**问题**：
- ⚠️ 只有 2 个敌人，游戏可能不够丰富
- ⚠️ `Intent_Pattern` 列格式混乱：应为单独列，不应在同一单元格内
- ❌ 缺少敌人状态机相关参数（虽然代码中已硬编码）

**修复建议**：
```csv
EnemyID,Name,Power,Armor,Description,Behavior_Type,Difficulty
2001,杂虏骑兵,15,2,高攻低血型敌人 善于突击,Aggressive,Normal
2002,匈奴重甲,20,5,高防低攻型敌人 防线坚固,Defensive,Normal
2003,狂战士,25,1,极高攻击的疯子,Berserk,Hard
2004,魔法师,10,3,远程魔法型敌人,Magical,Hard
```

---

### 3. **CardTable.csv** - 🟢 基本完整

**当前内容**：12 张卡牌定义完整

**评估**：✅ 格式正确，数据完整
- 单位卡（3 张）
- 策略卡（4 张）
- 干扰卡（3 张）
- 阵法卡（1 张）
- 不支持的 Effect_ID 需配对效果系统

**建议**：
- 添加更多卡牌丰富游戏性
- 验证 Effect_ID 是否已在代码中实现

---

### 4. **EventTable_v2.csv** - 🟢 重度使用中

**检查结果**：
- ✅ 233 行数据，覆盖完整故事线
- ✅ 字段齐全：ID, Title, Context, Options, Results, Conditions
- ✅ 结果指令支持：ADD_RES, SUB_RES, DAMAGE, GAME_OVER 等
- ✅ 分支条件正确（belief>150 等）

**问题**：
- ⚠️ 无法与新的 FSM 敌人状态关联
- 建议：添加 `TRIGGER_BATTLE` 触发机制到特定事件

---

### 5. **EndingTable.csv** - 🟢 基本完整

**检查结果**：✅ 结束分支表完整

---

## 📊 优先级修复清单

### 🔴 立即修复（游戏崩溃风险）

| # | 文件 | 问题 | 修复时间 |
|----|------|------|--------|
| 1 | GlobalConfig.csv | 列名错误 + 值不匹配 | 5 分钟 |
| 2 | CardTable.csv | 验证 Effect_ID 实现 | 10 分钟 |

### 🟡 短期修复（功能优化）

| # | 文件 | 问题 | 修复时间 |
|----|------|------|--------|
| 3 | EnemyTable.csv | 敌人数量过少 + 格式优化 | 15 分钟 |
| 4 | EventTable_v2.csv | 战斗触发关联 | 20 分钟 |

---

## 🔧 详细修复方案

### A. GlobalConfig.csv 修复

**步骤 1**：打开文件
```
Assets/Resources/Data/GlobalConfig.csv
```

**步骤 2**：替换全部内容为
```csv
Player_Start_Food,Player_Start_Armor,Enemy_Start_Food,Enemy_Start_Armor,Turn_Regen_Food,Turn_Regen_Armor,Defend_Mitigation_Rate,Attack_Base_Damage,Flee_Belief_Penalty,Victory_Loot_Food,Victory_Loot_Armor
1,1,0,0,2,1,0.5,5,5,2,1
```

**对应代码值**：
- `Turn_Regen_Food = 2` ✅ 匹配 BattleManager StartTurnRoutine()
- `Flee_Belief_Penalty = 5` ✅ 匹配 BattleManager FleeBeliefPenalty
- `Attack_Base_Damage = 5` ✅ 匹配 OnAttackCmd()

---

### B. EnemyTable.csv 扩展

**步骤 1**：打开文件
```
Assets/Resources/Data/EnemyTable.csv
```

**步骤 2**：替换全部内容为
```csv
EnemyID,Name,Power,Armor,Description,Behavior_Type,Difficulty
2001,杂虏骑兵,15,2,高攻低血型敌人 善于突击,Aggressive,Normal
2002,匈奴重甲,20,5,高防低攻型敌人 防线坚固,Defensive,Normal
2003,狂战士,25,1,极高攻击的疯子,Berserk,Hard
2004,暗夜法师,10,3,远程控制型敌人,Magical,Hard
```

**对应 FSM 状态**：
- Aggressive → 易进入 POWER_STRIKE
- Defensive → 易进入 COUNTERATTACK
- Berserk → 易进入 DESPERATE
- Magical → 混合型

---

### C. CardTable.csv 效果验证

**当前支持的 Effect_ID**：
- ✅ ADD_RES (添加资源)
- ✅ SUB_RES (扣除资源)
- ✅ DRAW_SELF (抽卡)
- ✅ STEAL_RES (偷资源)
- ❓ DISABLE_ATK (需验证)
- ❓ AOE_EARTHQUAKE (需验证)
- ❓ FORM_NO_RETREAT (需验证)

**建议**：
在 DataManager 或 BattleManager 中验证这些 Effect 是否已实现。如未实现，需添加对应处理代码。

---

### D. EventTable_v2.csv 战斗触发

**当前问题**：事件和战斗是分离的，应关联起来

**建议**：在特定事件中添加战斗触发
```csv
ID,Title,Context,...,Trigger_Battle,Battle_Enemy_ID
1003,第一次接触,几骑匈奴斥候...,1,2001
```

代码中处理：
```csharp
if (eventData.Trigger_Battle)
{
    BattleManager.Instance.StartBattle(DataManager.Instance.GetEnemyData(eventData.Battle_Enemy_ID));
}
```

---

## ✅ 验证清单

完成修复后，运行以下测试：

- [ ] 游戏启动无错误
- [ ] 战斗面板在启动时不弹出
- [ ] 进入战斗 → BattleIntroPanel 正确显示
- [ ] 选择战斗 → 面板关闭，进入战斗
- [ ] 选择逃离 → 面板关闭，扣 5 信念，返回大地图
- [ ] 战斗结束 → BattleResultPanel 显示统计信息
- [ ] 点击确认 → 面板关闭，返回事件系统
- [ ] 粮草每回合增加 2 点 ✅
- [ ] 信念初始值正确 ✅
- [ ] 敌人选择多样 ✅

---

## 📞 快速参考

**CSV 文件位置**：
```
Assets/Resources/Data/
├── GlobalConfig.csv          ← 【优先修复】
├── EnemyTable.csv            ← 【优先修复】
├── CardTable.csv             ← 【验证】
├── EventTable_v2.csv         ← 【增强】
├── EventTable.csv
├── EndingTable.csv
├── CardBalanceData_v1.csv
├── LevelData_EnemyProgression.csv
└── StoryPanelTable.csv
```

**下一步**：
1. 修复 GlobalConfig.csv（5 分钟）
2. 扩展 EnemyTable.csv（15 分钟）
3. 运行游戏测试弹出逻辑
4. 验证所有面板关闭工作正常

