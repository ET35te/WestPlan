# 🔧 战斗系统集成文档 - 技术实现指南

**文档类型**：集成说明  
**目标读者**：开发者  
**完成时间**：2026-01-10

---

## 📌 集成概况

| 组件 | 文件 | 状态 | 说明 |
|------|------|------|------|
| 敌人状态机 | `EnemyStateMachine.cs` | ✅ 完成 | 独立状态管理类，无依赖 |
| 战斗管理器 | `BattleManager.cs` | ✅ 完成 | 集成 FSM + 粮草恢复 + 非终结普攻 |
| 全局粮草 | `ResourceManager.cs` | ⏳ 需验证 | 全局粮草回收已集成，待测试 |
| UI 面板 | `UIManager.cs` | ✅ 已有 | 战斗结果面板自动处理，无需改动 |

---

## 🔗 代码集成点详解

### 集成点 1：EnemyStateMachine 初始化

**位置**：`BattleManager.cs` - `StartBattle` 方法  
**代码**：
```csharp
// 4. 初始化敌人状态机
enemyFSM = new EnemyStateMachine(EnemyUnitCount);
lastTurnPlayerDamageToEnemy = 0;
totalFoodGainedThisBattle = 0;
```

**检查清单**：
- [ ] `EnemyStateMachine.cs` 文件存在
- [ ] 变量 `enemyFSM` 已声明为字段
- [ ] 敌人战力 `EnemyUnitCount` 已初始化
- [ ] 日志不显示 `NullReferenceException`

---

### 集成点 2：全局粮草恢复

**位置**：`BattleManager.cs` - `StartTurnRoutine` 方法  
**代码**：
```csharp
// 📊 全局粮草恢复：每回合 +2
PlayerFood += 2;
totalFoodGainedThisBattle += 2;
LogToScreen($"<color=yellow>补给线恢复：粮草 +2（本战斗累计：+{totalFoodGainedThisBattle}）</color>");
```

**检查清单**：
- [ ] 每玩家回合开始时，粮草增加 +2
- [ ] 累计粮草变量正确递增
- [ ] 日志在控制台显示正确消息
- [ ] UI 中 `Text_Player_Food` 更新反映新粮草值

**全球影响**：
- 粮草现在是战斗内的**局部资源**，每回合补充
- 不影响全局 `ResourceManager.Grain`（库存粮）
- 战斗结束时，战场获得的粮食加回库存（见下文）

---

### 集成点 3：敌人状态更新

**位置**：`BattleManager.cs` - `StartTurnRoutine` 方法  
**代码**：
```csharp
// 📊 更新敌人状态（传入敌人血量百分比与玩家上回合伤害）
float enemyHPPercent = (float)EnemyUnitCount / (EnemyUnitCount + 10); // 简化估值
enemyFSM?.UpdateState(enemyHPPercent, lastTurnPlayerDamageToEnemy);
lastTurnPlayerDamageToEnemy = 0; // 重置伤害计数
enemyFSM?.ResetConsecutiveCount(); // 重置反制计数
```

**说明**：
- 敌人血量百分比 = `EnemyUnitCount / (初始战力 + 10)`
- 每回合重置玩家伤害计数（用于蓄力打断判定）
- 重置连续出牌计数（下回合重新计数）

**检查清单**：
- [ ] `UpdateState` 被正确调用
- [ ] `ResetConsecutiveCount` 在回合开始时调用
- [ ] 敌人血量百分比计算合理（0 ~ 1）
- [ ] FSM 状态日志在控制台显示

---

### 集成点 4：玩家出牌追踪

**位置**：`BattleManager.cs` - `OnConfirmPlayCardClicked` 方法  
**代码**：
```csharp
// 📊 追踪玩家出牌，用于触发敌人反制
enemyFSM?.OnPlayerPlayCard();
```

**说明**：
- 每次玩家确认出牌，FSM 计数递增
- 当连续出牌 ≥2 张时，敌人进入反制状态
- 重置时机：玩家回合开始（见上文的 `ResetConsecutiveCount`）

**检查清单**：
- [ ] 每次出牌后调用 `OnPlayerPlayCard`
- [ ] Console 中能看到"敌军感知到威胁"日志
- [ ] 反制状态在下个敌人回合生效

---

### 集成点 5：敌人伤害计算

**位置**：`BattleManager.cs` - `EnemyTurnRoutine` 方法  
**代码**：
```csharp
// 🎭 使用敌人状态机计算伤害
int dmg = enemyFSM?.CalculateDamage(PlayerArmor) ?? 0;

if (dmg > 0) 
{
    PlayerUnitCount -= dmg;
    LogToScreen($"受到 {dmg} 点伤害！当前敌人状态：{enemyFSM?.CurrentState}");
    // 飘字效果
    DamagePopup.SpawnPopup($"-{dmg}", Camera.main.transform.position + Vector3.left * 2, new Color(1, 0.5f, 0));
} 
else if (enemyFSM?.CurrentState == EnemyStateMachine.State.CHARGING)
{
    LogToScreen("敌军正在蓄力，本回合不攻击...");
}
```

**说明**：
- 敌人伤害完全由 FSM 计算，考虑状态倍数和玩家护甲
- CHARGING 状态下返回 0 伤害（提供了预告窗口）
- 日志显示当前敌人状态，便于调试

**检查清单**：
- [ ] 不同敌人状态下伤害值不同
- [ ] CHARGING 状态下敌人不攻击
- [ ] 伤害数值在日志中显示正确
- [ ] 飘字效果正确播放

---

### 集成点 6：非终结普攻

**位置**：`BattleManager.cs` - `OnAttackCmd` 和 `OnDefendCmd` 方法

**修改前**：
```csharp
void OnAttackCmd() { ... EndPlayerTurn(); } // ❌ 强制结束
```

**修改后**：
```csharp
void OnAttackCmd() { ...; UpdateUI(); CheckVictoryCondition(); } // ✅ 不结束
```

**说明**：
- 移除了 `EndPlayerTurn()` 调用
- 玩家可以继续出牌或防御
- 结束回合需要主动点击 [Skip] 按钮

**检查清单**：
- [ ] 攻击后可以继续出牌
- [ ] 防御后可以继续出牌
- [ ] Skip 按钮才能正式结束回合
- [ ] 回合逻辑（玩家→敌人→玩家）正确进行

---

## 📊 数据流图

```
战斗开始
    │
    ├─► StartBattle()
    │   ├─► 初始化 EnemyStateMachine(战力)
    │   ├─► 初始化粮草追踪变量
    │   └─► 开始 BattleIntroSequence
    │
    ├─► 玩家选择"战斗"
    │
    ├─► 先手判定
    │
    └─► 回合循环
        │
        ├─ 玩家回合开始
        │  ├─ +2粮草
        │  ├─ 更新敌人状态
        │  │  └─ enemyFSM.UpdateState(敌人HP%, 上回合伤害)
        │  ├─ 重置连续出牌计数
        │  ├─ 更新敌人意图显示
        │  └─ 玩家行动（出牌/攻击/防御）
        │     └─ enemyFSM.OnPlayerPlayCard() ◄─ 追踪出牌
        │
        ├─ 敌人回合开始
        │  ├─ 计算伤害：dmg = enemyFSM.CalculateDamage(玩家甲)
        │  ├─ 考虑敌人状态倍数
        │  └─ 造成伤害或蓄力/反制/虚弱等
        │
        └─► 回合结束判定
            ├─ 玩家信念 ≤ 0 → 失败
            ├─ 敌人战力 ≤ 0 → 胜利
            └─ 否则继续循环
```

---

## 🔄 状态机调用流程

```
玩家回合
  │
  ├─► enemyFSM.UpdateState(敌人HP%, 伤害)
  │   ├─ 检查虚弱计数
  │   ├─ 检查蓄力完成
  │   ├─ 检查蓄力被打断
  │   ├─ 检查绝望条件
  │   └─ 检查血量触发蓄力
  │
  ├─► enemyFSM.OnPlayerPlayCard()
  │   ├─ 连续出牌计数 ++
  │   └─ 如果 ≥2 张 → 进入反制
  │
  ├─► enemyFSM.ResetConsecutiveCount()
  │
  ├─► enemyFSM.GetIntentText(预计伤害)
  │   └─ 返回敌人意图文本显示在UI
  │
  └─► enemyFSM.CalculateDamage(玩家甲)
      └─ 根据当前状态返回伤害值

敌人回合
  │
  └─► 造成伤害或不攻击（基于 CalculateDamage 返回值）
```

---

## 🐛 常见问题排查

### Q1：敌人每回合都不攻击，一直显示"蓄力"

**原因**：敌人卡在 CHARGING 状态，没有转移到 POWER_STRIKE  
**排查**：
```csharp
// 在 EnemyStateMachine.cs UpdateState 方法中检查
if (_currentState == State.CHARGING)
{
    _chargeCounter++;
    Debug.Log($"蓄力计数: {_chargeCounter}"); // ◄ 检查这里
    if (_chargeCounter >= 1)
    {
        _currentState = State.POWER_STRIKE;
    }
}
```

**解决**：确保 `_chargeCounter` 在 `StartTurnRoutine` 中被重置为 0

---

### Q2：玩家攻击后直接结束回合

**原因**：旧代码中残留了 `EndPlayerTurn()` 调用  
**排查**：
```csharp
void OnAttackCmd()
{
    // ... 伤害逻辑 ...
    
    // ❌ 检查这里是否有 EndPlayerTurn()
    // EndPlayerTurn();  // 应该被删除
    
    // ✅ 应该只有
    UpdateUI();
    CheckVictoryCondition();
}
```

**解决**：删除 `OnAttackCmd` 和 `OnDefendCmd` 中的 `EndPlayerTurn()` 调用

---

### Q3：粮草不恢复，一直是 0

**原因**：`StartTurnRoutine` 未正确调用，或粮草增量逻辑不执行  
**排查**：
```csharp
IEnumerator StartTurnRoutine()
{
    turnCount++;
    
    // ✅ 这行一定要存在
    PlayerFood += 2;
    totalFoodGainedThisBattle += 2;
    LogToScreen($"补给线恢复：粮草 +2");
    
    Debug.Log($"[DEBUG] 粮草: {PlayerFood}"); // ◄ 检查这里
    
    // ...
}
```

**解决**：
- 确认 `BattleStartSequence` 最后调用 `StartTurnRoutine`
- 确认玩家回合真的进入了这个协程（检查日志）
- 如果日志不显示，检查 `isPlayerTurn` 是否设置正确

---

### Q4：敌人一直处于反制状态

**原因**：`ResetConsecutiveCount` 未被正确调用  
**排查**：
```csharp
void StartTurnRoutine()
{
    // ...
    enemyFSM?.ResetConsecutiveCount(); // ✅ 一定要调用
    Debug.Log($"[DEBUG] 连续出牌计数已重置");
    // ...
}
```

**解决**：确保每个玩家回合开始时调用一次 `ResetConsecutiveCount`

---

## ✅ 验证清单

在编辑器中运行战斗，检查以下项目：

### 基础功能
- [ ] 战斗开始，显示"战斗/撤退"选择
- [ ] 选择"战斗"后进行先手判定
- [ ] 玩家回合开始显示"+2粮草"提示

### 粮草系统
- [ ] 每个玩家回合粮草 +2
- [ ] 粮草值在 UI 中正确显示
- [ ] 攻击/防御消耗粮草正确计算

### 普攻非终结
- [ ] 点击 [攻击] 后可以继续出牌
- [ ] 点击 [攻击] 后可以继续 [防御]
- [ ] 只有 [Skip] 才能结束玩家回合

### 敌人状态机
- [ ] 敌人血量 30% 时显示"蓄力"提示
- [ ] 蓄力状态下敌人不攻击（伤害为 0）
- [ ] 强力一击伤害明显高于普通攻击（约 2 倍）
- [ ] 玩家出 2+ 张牌时显示"反制"提示
- [ ] 反制伤害为 x1.5

### 意图提示
- [ ] 敌人每回合显示正确的意图文本
- [ ] 敌人状态改变时文本更新
- [ ] 预计伤害数值准确

### 战斗胜负
- [ ] 敌人血量归零，战斗胜利
- [ ] 玩家信念归零，战斗失败
- [ ] 撤退扣除 5 点信念，返回大地图
- [ ] 战斗结果面板显示统计信息

---

## 📈 性能监控

### 内存占用
- FSM 状态机：极小（仅几个 int/float 变量）
- 预期内存增长：< 1 MB

### CPU 占用
- 状态更新：每个玩家回合 O(1)
- 伤害计算：每个敌人回合 O(1)
- 预期性能影响：可忽略

### 建议优化
- 如果有大量敌人，考虑缓存 `CalculateDamage` 结果
- FSM 日志可在生产版本中禁用

---

## 🚀 后续扩展路线

| 优先级 | 功能 | 预计工作量 | 说明 |
|--------|------|----------|------|
| 高 | 虚弱卡实现 | 2小时 | 在 `ApplyCardEffect` 中添加逻辑 |
| 中 | 多敌人群战 | 4小时 | 为每个敌人创建独立 FSM |
| 中 | 玩家 BUFF 系统 | 3小时 | 类似 EnemyFSM 的玩家状态机 |
| 低 | 难度设置 | 1小时 | 在游戏开始时选择 FSM 参数预设 |
| 低 | 视觉特效 | 可变 | 敌人状态转移的闪烁/动画效果 |

---

**文档版本**：1.0  
**最后更新**：2026-01-10  
**审核状态**：待测试

