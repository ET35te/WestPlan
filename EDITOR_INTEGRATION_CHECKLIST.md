# 🎮 战斗系统编辑器集成清单 - 实施指南

**目标**：在 Unity 编辑器中完成所有新战斗系统的配置和测试  
**预计时间**：15-30 分钟  
**难度**：⭐⭐ 中等

---

## ✅ 第 0 步：代码部署验证

确保所有代码文件已正确保存和编译：

```
Assets/_Scripts/
├── Managers/
│   └── BattleManager.cs           ✅ 已更新（FSM、粮草、非终结普攻）
├── Systems/
│   └── EnemyStateMachine.cs       ✅ 新建（敌人状态机）
└── ...

根目录/
├── ENEMY_FSM_DESIGN.md            ✅ 新建（设计文档）
├── BATTLE_SYSTEM_CONFIG_GUIDE.md  ✅ 新建（配置指南）
└── BATTLE_INTEGRATION_GUIDE.md    ✅ 新建（集成文档）
```

**验证步骤**：
1. 打开 Unity 编辑器
2. 检查 Console 中是否有编译错误
3. 确认 `BattleManager.cs` 和 `EnemyStateMachine.cs` 没有红色下划线

---

## ✅ 第 1 步：场景配置检查

### 1.1 定位 Battle 场景

- 打开 Scene：`Assets/Scenes/[YourBattleScene].unity`
  （通常名为 `SampleScene` 或 `Battle` 或类似）

### 1.2 检查 BattleManager GameObject

在 Hierarchy 中找到 `BattleManager` 对象：
- 选中它
- 在 Inspector 中查看 `BattleManager.cs` 脚本

### 1.3 验证 UI 引用

在 Inspector 中检查以下字段是否已绑定：

| 字段 | 类型 | 绑定状态 | 说明 |
|------|------|--------|------|
| `Text_Player_Food` | TMP_Text | ⏳ | 显示玩家战斗粮食 |
| `Text_Player_Armor` | TMP_Text | ⏳ | 显示玩家甲值 |
| `Text_Player_Unit` | TMP_Text | ⏳ | 显示玩家信念（HP） |
| `Text_Enemy_Unit` | TMP_Text | ⏳ | 显示敌人战力 |
| `Text_Enemy_Intent` | TMP_Text | ⏳ | **新** 显示敌人意图 |
| `BattleLogText` | TMP_Text | ⏳ | 显示战斗日志 |
| `AttackBtn` | Button | ⏳ | 攻击按钮 |
| `DefendBtn` | Button | ⏳ | 防御按钮 |
| `SkipBtn` | Button | ⏳ | 跳过/结束回合按钮 |
| `ConfirmPlayCardBtn` | Button | ⏳ | 出牌确认按钮 |
| `HandAreaTransform` | Transform | ⏳ | 手牌区域 |
| `CardPrefab` | GameObject | ⏳ | 卡牌预制体 |

**处理步骤**：
- 如果某个字段显示"None"，需要在 Canvas 或场景中找到对应的 UI 元素并拖拽赋值
- 如果看到警告"Missing (TMP_Text)"，表示引用已断开，需要重新绑定

---

## ✅ 第 2 步：新增 UI 元素（可选）

如果想为敌人意图添加专用面板：

### 2.1 创建 Enemy Intent Panel

1. 在 Canvas 下新建 Panel
   ```
   Canvas
   └── EnemyIntentPanel (新建)
       ├── Background (Image)
       └── IntentText (TextMeshPro - Text)
   ```

2. 设置 IntentText 属性
   - Alignment：上中
   - Font Size：24-32
   - Color：黄色或橙色

3. 在 BattleManager Inspector 中
   - 如果 `Text_Enemy_Intent` 未绑定，将 `IntentText` 拖拽到该字段

### 2.2 为 Battle Result Panel 添加专用设计（可选）

目前共用 ResultPanel，若需专用面板：

```
Canvas
├── BattleResultPanel (新建，可隐藏)
│   ├── Background (Image，半透明黑）
│   ├── TitleText (TextMeshPro - Text)："战斗结果"
│   ├── ResultText (TextMeshPro - Text)：战斗统计
│   └── ConfirmButton (Button)：返回按钮
│       └── Text (TextMeshPro - Text)："继续"
```

---

## ✅ 第 3 步：FSM 参数配置（编辑器调整）

### 3.1 编辑 EnemyStateMachine.cs 暴露参数

为了在编辑器中调整 FSM 参数，编辑 `EnemyStateMachine.cs`：

打开文件，找到以下代码（约第 20-25 行）：

**原始代码**：
```csharp
private float CriticalHPThreshold = 0.3f;
private float DespairHPThreshold = 0.1f;
private int ConsecutiveCardThreshold = 2;
private int WeaknessDefaultDuration = 2;
private float ChargePowerMultiplier = 2f;
```

**改为公开属性**（选择一种方式）：

**方式 A：SerializeField（推荐）**
```csharp
[SerializeField] private float CriticalHPThreshold = 0.3f;
[SerializeField] private float DespairHPThreshold = 0.1f;
[SerializeField] private int ConsecutiveCardThreshold = 2;
[SerializeField] private int WeaknessDefaultDuration = 2;
[SerializeField] private float ChargePowerMultiplier = 2f;
```

**方式 B：Public Property**
```csharp
public float CriticalHPThreshold { get; set; } = 0.3f;
public float DespairHPThreshold { get; set; } = 0.1f;
// ... 等等
```

### 3.2 保存文件并刷新 Unity

- 保存 `EnemyStateMachine.cs`
- 返回 Unity，等待编译完成
- BattleManager Inspector 中不会直接看到这些参数
- 而是在运行时通过 BattleManager 的字段访问

如果希望在 BattleManager 编辑器中直接调整，需要添加包装字段：

```csharp
public class BattleManager : MonoBehaviour
{
    [Header("--- 敌人FSM参数 ---")]
    [SerializeField] private float fsmCriticalHPThreshold = 0.3f;
    [SerializeField] private float fsmDespairHPThreshold = 0.1f;
    [SerializeField] private int fsmConsecutiveCardThreshold = 2;
    [SerializeField] private float fsmChargePowerMultiplier = 2f;
    
    void StartBattle(DataManager.EnemyData enemyData)
    {
        // ... 初始化代码 ...
        enemyFSM = new EnemyStateMachine(EnemyUnitCount);
        // 可选：设置参数（如果 FSM 暴露了 setter）
        // enemyFSM.CriticalHPThreshold = fsmCriticalHPThreshold;
    }
}
```

---

## ✅ 第 4 步：测试战斗流程

### 4.1 启动游戏并进入战斗

1. 按 **Play** 按钮
2. 导航到战斗开始的地方（例如选择事件或敌人触发）
3. 进入 Battle 场景

### 4.2 验证战斗介绍流程

| 检查项 | 预期结果 | 状态 |
|--------|---------|------|
| 出现"战斗/撤退"选择 | 两个按钮可点击 | ⏳ |
| 点击"撤退" | 扣除 5 点信念，返回大地图 | ⏳ |
| 点击"战斗" | 显示先手判定，然后开始战斗 | ⏳ |

**日志检查**：
```
Console 中应该看到：
✅ UIManager: 收到战斗胜利消息
⚔️ 遭遇强敌！正在判定先手...
【我方先攻】 或 【敌方先攻】
```

### 4.3 验证玩家回合逻辑

| 检查项 | 预期结果 | 状态 |
|--------|---------|------|
| 回合开始 | 显示"+2粮草"提示 | ⏳ |
| 攻击不结束回合 | 可继续出牌或防御 | ⏳ |
| 防御不结束回合 | 可继续出牌或攻击 | ⏳ |
| Skip 结束回合 | 转为敌人回合 | ⏳ |

**关键日志**：
```
Console 中应该看到：
第1回合
补给线恢复：粮草 +2（本战斗累计：+2）
⚔️ 敌军意图: 普通攻击
```

### 4.4 验证敌人状态机

| 敌人状态 | 触发条件 | 预期显示 | 状态 |
|---------|---------|---------|------|
| NORMAL | 初始状态 | `⚔️ 敌军意图: 普通攻击` | ⏳ |
| CHARGING | 敌人血量 ≤ 30% | `⚠️ 敌军正在蓄力...` | ⏳ |
| POWER_STRIKE | 蓄力完成 | `💥 敌军发动强力一击！` | ⏳ |
| COUNTERATTACK | 玩家出 ≥2 张牌 | `🔄 敌军反制攻击！` | ⏳ |

**测试步骤**：
1. 出牌多次，让敌人血量降低
2. 观察控制台中状态切换的日志
3. 验证 UI 中的意图文本更新

---

## ✅ 第 5 步：虚弱卡配置

### 5.1 添加虚弱卡到 CSV

编辑 `Assets/Resources/Data/EventTable_v2.csv`（或相应的卡牌表）：

添加一行虚弱卡：
```csv
CardID,Name,Type,Cost_Food,Cost_Armor,Effect_ID,Effect_Val,Power,Description
1016,虚弱术,Spell,1,0,APPLY_WEAKNESS,0,0,使敌军陷入虚弱状态 (2回合)
```

### 5.2 测试虚弱卡

1. 调整卡牌概率或直接加入初始手牌
2. 在战斗中使用虚弱卡
3. 观察：
   - Console 中是否显示"发动虚弱术！"
   - 敌人伤害是否降至 0（虚弱状态下 x0.5 倍数）

---

## 🔍 调试技巧

### 技巧 1：启用详细日志

在 `BattleManager.cs` 中搜索 `Debug.Log`，如果没有看到日志，检查：
1. Console 窗口是否打开（Window → General → Console）
2. Log 过滤器是否被设置为只显示错误
3. 是否在运行时没有实际进入战斗

### 技巧 2：暂停游戏检查状态

1. 在 `EnemyStateMachine.cs` 的 `UpdateState` 中添加断点
2. 或者在 `CalculateDamage` 中添加日志
3. 按 Play，进入战斗，观察断点触发情况

### 技巧 3：检查 FSM 状态

添加临时代码到 `UpdateUI` 中查看当前敌人状态：
```csharp
void UpdateUI()
{
    // ... 现有代码 ...
    if (enemyFSM != null)
    {
        enemyFSM.DebugPrintState(); // 输出当前状态
    }
}
```

---

## ⚠️ 常见配置错误

| 错误 | 症状 | 解决方案 |
|------|------|---------|
| 敌人意图不更新 | UI 显示固定文本 | 检查 `Text_Enemy_Intent` 是否绑定 |
| 粮草显示为 0 | 始终无粮食 | 检查 `PlayerFood += 2` 是否在 StartTurnRoutine 中执行 |
| 攻击仍强制结束 | 无法继续出牌 | 删除 `OnAttackCmd` 中的 `EndPlayerTurn()` |
| FSM 状态不切换 | 敌人一直 NORMAL | 检查 `UpdateState` 是否在回合开始时调用 |
| 虚弱卡不生效 | 使用虚弱卡无反应 | 检查 CSV 中 `Effect_ID` 是否为 `APPLY_WEAKNESS` |

---

## ✅ 最终验收清单

运行一场完整的战斗，检查以下全部项目：

### 战斗流程
- [ ] 显示战斗/撤退选择
- [ ] 撤退扣除信念，返回大地图
- [ ] 进行先手判定（50% 玩家先/敌方先）

### 玩家回合
- [ ] 显示"+2粮草"提示
- [ ] 攻击按钮可用，不强制结束
- [ ] 防御按钮可用，不强制结束
- [ ] 可多次攻击/防御/出牌
- [ ] Skip 按钮结束回合

### 敌人行为
- [ ] 敌人血量 30% 时进入蓄力
- [ ] 蓄力状态下不攻击（伤害 0）
- [ ] 蓄力后发动强力一击（伤害 x2）
- [ ] 玩家出 2+ 张牌时敌人反制（伤害 x1.5）
- [ ] 意图文本正确更新

### UI 显示
- [ ] 粮食、护甲、信念值正确更新
- [ ] 敌人意图文本清晰显示
- [ ] 战斗日志不会爆屏
- [ ] 飘字效果显示伤害数值

### 战斗结束
- [ ] 敌人血量 ≤ 0 → 胜利
- [ ] 玩家信念 ≤ 0 → 失败
- [ ] 结果面板显示统计信息
- [ ] 3 秒后自动返回大地图

---

## 📞 如需进一步帮助

**常见问题**：
1. 如果 UI 元素显示为"Missing"，检查是否被删除或重命名
2. 如果敌人状态机日志不显示，检查 Console 过滤器
3. 如果粮草数值异常，检查 `StartTurnRoutine` 是否真的被调用

**可能的后续优化**：
1. 为 FSM 参数添加难度预设
2. 为敌人状态转移添加视觉效果
3. 为虚弱状态添加敌人图像灰度化效果

---

**版本**：1.0  
**最后更新**：2026-01-10  
**状态**：✅ 完成并待测试

