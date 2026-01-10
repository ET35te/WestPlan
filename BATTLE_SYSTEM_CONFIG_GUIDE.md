# 🎮 战斗系统升级指南 - 编辑器配置步骤

## 📋 概览

你已经完成了代码级别的系统升级：
- ✅ 敌人状态机（EnemyStateMachine.cs）
- ✅ 全局粮草恢复机制（每回合 +2）
- ✅ 攻击/防御非终结动作
- ✅ 敌人多形态战斗

现在需要在 **Unity 编辑器中进行 UI 配置和测试**。

---

## 🎛️ 编辑器配置步骤

### 第一步：打开 BattleManager 并配置 FSM 参数

1. **定位 BattleManager 脚本**
   - 路径：`Assets/_Scripts/Managers/BattleManager.cs`
   - 在 Scene 中找到 `BattleManager` GameObject（或 Prefab）

2. **在 Inspector 中查看新增的敌人 FSM 参数**
   - 目前这些是 **private** 字段，编辑器暂不可见
   - 如果需要在编辑器调整，编辑 `EnemyStateMachine.cs`，为以下字段添加 `[SerializeField]`：

   ```csharp
   [SerializeField] private float CriticalHPThreshold = 0.3f;    // 触发蓄力的血量百分比
   [SerializeField] private float DespairHPThreshold = 0.1f;     // 触发绝望的血量百分比
   [SerializeField] private int ConsecutiveCardThreshold = 2;    // 触发反制的连续出牌张数
   [SerializeField] private int WeaknessDefaultDuration = 2;     // 虚弱持续回合数
   [SerializeField] private float ChargePowerMultiplier = 2f;    // 蓄力后伤害倍数
   ```

3. **保存后刷新 Inspector**
   - 修改后，回到 Unity 编辑器
   - BattleManager Inspector 中应该能看到这些参数
   - 根据游戏平衡需求调整数值

### 第二步：添加虚弱卡牌

目前敌人状态机中有"虚弱"状态，但需要卡牌能触发它。

1. **打开 CardData 定义**（如果存在）
   - 路径：`Assets/_Scripts/Managers/DataManager.cs` 或 `CardData.cs`

2. **检查或新增虚弱效果**
   - 在 `ApplyCardEffect` 中添加处理：
   ```csharp
   case "APPLY_WEAKNESS":
       enemyFSM?.ApplyWeakness();
       LogToScreen($"发动虚弱！敌军陷入虚弱状态");
       DamagePopup.SpawnPopup("WEAK", Camera.main.transform.position + Vector3.right * 2, new Color(1, 0.5f, 1));
       break;
   ```

3. **在 EventTable_v2.csv 中创建虚弱卡**
   - 新增一行（示例）：
   ```
   CardID,Name,Type,Cost_Food,Cost_Armor,Effect_ID,Effect_Val,Power,Description
   1016,虚弱术,Spell,1,0,APPLY_WEAKNESS,0,0,使敌军陷入虚弱状态 (2回合)
   ```

### 第三步：Battle Result Panel 配置

为战斗结果添加专用面板（目前共用 ResultPanel）。

1. **如果需要专用战斗结果面板**，在 Canvas 中添加：
   - 新建 Panel → 命名为 `BattleResultPanel`
   - 添加子对象：
     - `ResultText` (TMP_Text) - 显示胜利/失败/撤退消息
     - `ConfirmButton` (Button) - 确认返回按钮

2. **在 UIManager 中绑定**（可选，当前流程已自动处理）
   - 在 `OnBattleVictory` 中可调用 `ShowBattleResultPanel()` 替代 `ShowResult()`

3. **示例面板结构**：
   ```
   Canvas
   └── BattleResultPanel (Panel)
       ├── Background (Image)
       ├── ResultText (TextMeshPro - Text)
       └── ConfirmButton (Button)
           └── Text (TextMeshPro - Text)
   ```

### 第四步：测试与调试

1. **启动游戏，进入战斗**
   - 选择 [战斗]
   - 观察日志输出（Console）

2. **验证关键流程**
   - ✅ 回合开始时粮草 +2
   - ✅ 攻击后**不强制结束回合**（可继续防御或出牌）
   - ✅ 敌人血量低时显示"蓄力"状态
   - ✅ 出牌 ≥2 张时敌人反制

3. **性能检查**
   - Console 中应该看到详细的 FSM 日志
   - 如果日志过多，可在 `EnemyStateMachine.cs` 中注释掉 `Debug.Log`

---

## 🔧 配置参数推荐值

### 基础平衡

| 参数 | 当前值 | 建议范围 | 说明 |
|------|--------|---------|------|
| 每回合粮草恢复 | +2 | +1 ~ +3 | 越高越简单 |
| 蓄力伤害倍数 | 2x | 1.5x ~ 2.5x | 控制敌人威胁度 |
| 蓄力触发血量 | 30% | 20% ~ 40% | 提前预警敌人 |
| 反制阈值 | 2张牌 | 2 ~ 3张 | 越低越容易反制 |
| 虚弱持续 | 2回合 | 1 ~ 3回合 | 控制虚弱有效时间 |

### 难度调整指南

**如果太简单**：
- 降低粮草恢复（+1 而非 +2）
- 提高蓄力伤害倍数（2.5x）
- 降低蓄力触发血量阈值（20%）

**如果太难**：
- 提高粮草恢复（+3）
- 降低蓄力伤害倍数（1.5x）
- 提高蓄力触发血量阈值（40%）
- 降低反制阈值（3张牌而非2张）

---

## 📊 预期行为流程

### 典型战斗演示

```
【玩家回合 1】
> 补给线恢复：粮草 +2（本战斗累计：+2）
> 第1回合
  玩家出牌 1 张（粮 - 1）
  玩家攻击（粮 - 1）  ← 攻击不结束，可继续出牌或防御
  玩家防御（粮 - 1）

【敌方回合】
> 敌军进行普通攻击

【玩家回合 2】
> 补给线恢复：粮草 +2
> 玩家出牌 2 张 → 敌人反制计数 ++
> 敌人感知威胁，准备反制！
> 玩家结束回合

【敌方回合】
> 🔄 敌军反制攻击！（伤害 x1.5）

【敌方血量 30%】
> ⚠️ 敌军血量下降，准备蓄力反击...

【玩家回合 N】
> 敌方蓄力 1 回合完毕...

【敌方回合】
> 💥 敌军发动强力一击！（伤害 x2）

【敌方血量 10%】
> 🔥 敌军陷入绝望，发起疯狂进攻！
> （伤害 x1.3，防御力降低）
```

---

## 🐛 常见问题 & 排查

| 问题 | 可能原因 | 解决方案 |
|------|--------|---------|
| 攻击仍然结束回合 | 代码更新未应用 | 重新保存 BattleManager.cs，确保 `OnAttackCmd` 中没有 `EndPlayerTurn()` |
| 敌人状态没有切换 | FSM 未初始化 | 检查 `StartBattle` 中是否调用 `enemyFSM = new EnemyStateMachine(...)` |
| 粮草每回合不恢复 | 代码问题 | 检查 `StartTurnRoutine` 中粮草恢复逻辑是否存在 |
| 虚弱卡不生效 | 效果未实现 | 在 `ApplyCardEffect` 中添加虚弱处理逻辑 |

---

## ✅ 验收清单

配置完成后，检查以下项目：

- [ ] FSM 参数在 Inspector 中可见（如添加了 SerializeField）
- [ ] 游戏日志显示粮草恢复与状态转移
- [ ] 攻击不再强制结束回合
- [ ] 敌人在血量 30% 时进入蓄力状态
- [ ] 玩家出 2+ 张牌时敌人进入反制状态
- [ ] 战斗胜利/失败/撤退正确流转回大地图
- [ ] 虚弱卡可成功使敌人进入虚弱状态
- [ ] 战斗结果面板正确显示统计信息

---

## 📞 如需进一步优化

可考虑的后续改进：

1. **卡牌多样化**：添加更多效果类型（净化虚弱、加倍伤害、抽牌等）
2. **敌人多样化**：为不同敌人配置不同的 FSM 参数
3. **BUFF 系统**：敌人和玩家都可获得临时增益/减益
4. **视觉反馈**：添加状态指示器动画、闪烁效果等
5. **难度曲线**：随着游戏进度提升敌人 FSM 参数

