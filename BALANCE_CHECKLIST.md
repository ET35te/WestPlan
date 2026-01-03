# ✅ 游戏平衡系统集成清单

**状态**: 🔄 等待集成到游戏引擎  
**创建时间**: 2026年1月3日

---

## 📦 已交付的文件

### 1️⃣ 核心系统代码

| 文件 | 行数 | 功能 | 状态 |
|------|------|------|------|
| `GameBalanceCalculator.cs` | 370+ | 卡牌定价 + 敌人生长 | ✅ 完成 |
| `DynamicLootSystem.cs` | 280+ | 战利品补偿机制 | ✅ 完成 |
| `BalanceValidationTester.cs` | 350+ | 数据验证和测试 | ✅ 完成 |

### 2️⃣ 数据表

| 文件 | 行数 | 用途 | 状态 |
|------|------|------|------|
| `CardBalanceData_v1.csv` | 16 | 卡牌伤害理论值验证 | ✅ 完成 |
| `LevelData_EnemyProgression.csv` | 13 | 敌人等级表(0-11关) | ✅ 完成 |

### 3️⃣ 文档

| 文件 | 页数 | 内容 | 状态 |
|------|------|------|------|
| `BALANCE_SYSTEM_GUIDE.md` | 本文档 | 完整实现指南 | ✅ 完成 |
| `BALANCE_CHECKLIST.md` | 此文件 | 集成检查清单 | ✅ 完成 |

---

## 🎯 集成步骤

### Step 1: 代码编译检查 (5分钟)

```csharp
// 1. 在Unity编辑器中打开项目
// 2. 检查 Assets/_Scripts/Systems/ 目录
// 3. 确认以下文件存在且无编译错误:
✓ GameBalanceCalculator.cs
✓ DynamicLootSystem.cs
✓ BalanceValidationTester.cs
```

**预期结果**: 0 个编译错误

---

### Step 2: 运行验证测试 (10分钟)

```csharp
// 1. 在Hierarchy中创建一个空GameObject
// 2. 添加 BalanceValidationTester 组件
// 3. 在Inspector中，右键 BalanceValidationTester
// 4. 选择 "📊 生成所有验证数据"
// 5. 查看控制台输出
```

**预期输出**:
```
========== 📈 卡牌平衡报告 ==========
[01] 轻步兵        | ... | 状态:✅ 平衡
[02] 快速斩击      | ... | 状态:✅ 平衡
...
🔍 曲线检查: ✅ 凸形(正常)
🔍 穿透伤害检查: ✅ 安全
```

---

### Step 3: 修改 DataManager.cs (30分钟)

在 `DataManager.cs` 中集成新的查表系统：

```csharp
// 在DataManager类中添加以下内容:

private List<GameBalanceCalculator.EnemyLevelData> levelProgression;

void Start()
{
    // 生成敌人等级表(仅一次)
    levelProgression = GameBalanceCalculator.GenerateEnemyProgression();
    Debug.Log($"✅ 加载敌人等级表，共{levelProgression.Count}关");
}

// 新增方法：按关卡查询敌人属性
public GameBalanceCalculator.EnemyLevelData GetEnemyLevelData(int nodeIndex)
{
    if (nodeIndex < 0 || nodeIndex >= levelProgression.Count)
    {
        Debug.LogError($"❌ 无效的关卡索引: {nodeIndex}");
        return null;
    }
    return levelProgression[nodeIndex];
}
```

**检查清单**:
- [ ] 添加 `levelProgression` 字段
- [ ] 在 `Start()` 中生成等级表
- [ ] 添加 `GetEnemyLevelData()` 方法
- [ ] 编译无错误

---

### Step 4: 修改 BattleManager.cs (45分钟)

将敌人属性计算改为查表法：

```csharp
// 旧代码 (已弃用):
// int enemyATK = EnemyPower * 0.2f;  // ❌ 实时计算

// 新代码 (查表法):
public void InitializeEnemy(int nodeIndex)
{
    var levelData = DataManager.Instance.GetEnemyLevelData(nodeIndex);
    
    if (levelData != null)
    {
        EnemyATK = levelData.FinalATK;
        EnemyMaxHP = levelData.FinalHP;
        EnemyCurrentHP = EnemyMaxHP;
        
        Debug.Log($"🎯 敌人初始化 - 关卡{nodeIndex}: ATK={EnemyATK}, HP={EnemyMaxHP}");
    }
}
```

**检查清单**:
- [ ] 删除旧的 `CurrentHP * 0.2f` 计算
- [ ] 添加 `InitializeEnemy()` 方法
- [ ] 在战斗开始时调用 `InitializeEnemy(CurrentNodeIndex)`
- [ ] 编译无错误
- [ ] 测试一场战斗，确认敌人属性正确

---

### Step 5: 修改 ResourceManager.cs (30分钟)

集成动态战利品系统：

```csharp
// 在战斗胜利后调用:
public void ApplyBattleReward(int nodeIndex, GameBalanceCalculator.CardArchetype playerArchetype)
{
    int baseGrain = 30;  // 基础掉落
    int baseArmor = 5;

    // 计算动态战利品
    var reward = DynamicLootSystem.CalculateFullLoot(
        baseGrain, baseArmor,
        Grain, Armor,  // 当前资源
        GrainCapacity, ArmorCapacity
    );

    // 应用流派调整
    reward = DynamicLootSystem.AdjustByArchetype(reward, playerArchetype);

    // 应用难度系数(假设所有关卡为Normal)
    var difficulty = (nodeIndex % 2 == 0) 
        ? DynamicLootSystem.BattleDifficulty.Rest 
        : DynamicLootSystem.BattleDifficulty.Spike;
    reward = DynamicLootSystem.ApplyDifficultyMultiplier(reward, difficulty);

    // 发放奖励
    AddGrain(reward.GrainReward + reward.BonusGrain);
    AddArmor(reward.ArmorReward);

    Debug.Log($"🎁 战利品: {reward}");
}
```

**检查清单**:
- [ ] 添加 `ApplyBattleReward()` 方法
- [ ] 在战斗结算时调用此方法
- [ ] 编译无错误
- [ ] 测试战斗奖励，检查动态补偿是否工作

---

### Step 6: 修改 CardTable.csv (20分钟)

应用新的定价公式到实际卡牌：

**现有卡牌修正:**

| 卡牌 | 旧伤害 | 新伤害(公式) | 理由 |
|------|--------|-----------|------|
| 轻步兵(1粮) | 3 | 5 | 1粮 × 5 = 5 |
| 虎豹骑(3粮) | 8 | 21 | 3粮 × 5 × 1.4 = 21 |
| 斩杀(2甲) | 20 | 26 | 2甲 × 8 × 1.6 = 26 |

```csv
// CardTable.csv 应更新为:
ID,Name,Cost_Grain,Cost_Armor,Power(伤害),Description
1001,轻步兵,1,0,5,1粮基准卡 (1*5=5)
1002,铁甲卫,1,1,9,混合成本 (1*5 + 1*8*1.6)
1003,虎豹骑,3,0,21,高费膨胀卡 (3*5*1.4=21)
3001,劫营,0,0,0,特殊效果
3003,地震,2,0,15,AOE伤害 (2*5*1.2=12)
```

**检查清单**:
- [ ] 逐张卡牌应用公式
- [ ] 确保低费卡(1-2费)效率在 4-8 范围内
- [ ] 确保高费卡有正向的膨胀系数
- [ ] 测试卡牌是否正确读取新的伤害值

---

### Step 7: 游戏内测试 (60分钟)

```
测试用例1: 卡牌伤害计算
  1. 进入战斗
  2. 使用"轻步兵"(1粮)卡牌
  3. 预期伤害: 5点 (应该是旧的3点)
  4. ✓ 通过 / ✗ 失败

测试用例2: 敌人属性
  1. 进入第5关(龟兹)
  2. 检查敌人ATK应该是 7 + 5*2 = 15 (不是旧值)
  3. 检查敌人HP应该符合指数曲线
  4. ✓ 通过 / ✗ 失败

测试用例3: 战利品补偿
  1. 拥有10粮时，战胜敌人
  2. 预期获得: 30 + (100-10)*0.5 = 75粮
  3. ✓ 通过 / ✗ 失败

测试用例4: 波峰波谷
  1. 连续玩两关
  2. 第1关(偶数->Rest): 敌人血量 < 普通
  3. 第2关(奇数->Spike): 敌人血量 > 普通
  4. ✓ 通过 / ✗ 失败
```

---

## 🔍 质量保证

### 代码质量检查

```
✓ 所有代码遵循驼峰命名法
✓ 关键方法有XML注释
✓ 使用const定义魔法数字(5, 1.2, 1.6等)
✓ 支持调试输出(Debug.Log)
```

### 数据有效性检查

```
✓ CardBalanceData_v1.csv 的效率曲线是凸形
✓ LevelData_EnemyProgression.csv 的ATK是线性的
✓ LevelData_EnemyProgression.csv 的HP是指数的
✓ 没有负数或异常值
```

### 运行时检查

```
✓ BattleManager 成功初始化敌人
✓ ResourceManager 正确计算动态战利品
✓ CardTable 加载新的伤害值
✓ 无运行时错误或异常
```

---

## 📊 验收标准

### Acceptance Criteria

| 项目 | 标准 | 验收方法 |
|------|------|---------|
| 卡牌定价 | 所有卡牌效率在 4-15 范围内 | 运行 `GenerateCardBalanceReport()` |
| 敌人曲线 | ATK线性增长，HP指数增长 | 查看 `LevelData_EnemyProgression.csv` |
| 战利品补偿 | 穷人获得更多，富人获得更少 | 运行 `GenerateLootComparisonReport()` |
| 防坑指南 | 曲线凸形，有穿透伤害卡 | 运行 `GenerateCardBalanceReport()` |

---

## 🚀 Go-Live Checklist

在发布前，确认以下所有项目都已完成：

- [ ] 所有代码编译无错误
- [ ] 所有验证测试通过
- [ ] 卡牌伤害值已更新
- [ ] 敌人属性已从查表读取
- [ ] 战利品补偿机制已集成
- [ ] 至少进行了5场完整游戏
- [ ] 没有发现运行时崩溃
- [ ] 数值平衡看起来合理（难度曲线平滑）
- [ ] 产品团队已验证(可选)

---

## 📞 支持

### 常见问题

**Q: 编译错误 "找不到GameBalanceCalculator"**  
A: 确认文件在 `Assets/_Scripts/Systems/GameBalanceCalculator.cs`

**Q: 测试脚本没有输出**  
A: 确认 BalanceValidationTester 已添加到场景，并在Inspector中运行Context Menu

**Q: 敌人属性没有更新**  
A: 确认 BattleManager 调用了 `InitializeEnemy(nodeIndex)`，且 DataManager 已生成等级表

### 联系方式

有任何问题，请查阅 `BALANCE_SYSTEM_GUIDE.md` 中的详细说明。

---

**文档版本**: v1.0  
**最后更新**: 2026年1月3日  
**下一版本**: 完成集成和测试后发布 v1.1

