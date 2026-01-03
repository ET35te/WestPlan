# 🎮 游戏平衡系统 - 快速参考卡片

## 📐 核心公式速记

### 卡牌伤害 (Card Damage Formula)

| 情况 | 公式 | 例子 |
|------|------|------|
| **低费(1费)** | Cost × 5 | 1粮 = 5伤 |
| **高费(2+)** | Cost × 5 × (1 + (Cost-1) × 0.2) | 2粮 = 12伤 |
| **护甲消耗** | Cost × 8 × 1.6 | 2甲 = 25.6伤 |

### 敌人属性 (Enemy Stats)

| 类型 | 公式 | 例子 |
|------|------|------|
| **ATK** | 5 + NodeIdx × 2 | 第5关 = 15 |
| **HP** | 30 × (1.2 ^ NodeIdx) | 第5关 = 74 |
| **波动** | Rest 0.8倍 / Spike 1.2倍 | 最终HP = 74 × 波动系数 |

### 战利品 (Loot)

```
获得粮 = 基础30 + (上限100 - 当前) × 0.5

例: 当前粮50 → 30 + (100-50) × 0.5 = 55粮
```

---

## 🎯 立即可用的代码片段

### 计算卡牌伤害

```csharp
float damage = GameBalanceCalculator.CalculateCardDamage(
    costGrain: 2,
    costArmor: 0
);
// 返回 12.0
```

### 生成敌人等级表

```csharp
var progression = GameBalanceCalculator.GenerateEnemyProgression();
var level5 = progression[5];
Debug.Log($"ATK:{level5.FinalATK}, HP:{level5.FinalHP}");
// 输出: ATK:15, HP:89
```

### 计算动态战利品

```csharp
int loot = DynamicLootSystem.CalculateLootReward(
    baseLoot: 30,
    currentGrain: 50,
    grainCapacity: 100
);
// 返回 55
```

---

## 📊 数值表 (Quick Lookup)

### 费用 vs 伤害

| 粮 | 伤害 | 甲 | 伤害 |
|----|------|----|----|
| 1 | 5 | 1 | 12.8 |
| 2 | 12 | 2 | 25.6 |
| 3 | 21.6 | 3 | 38.4 |
| 4 | 33.6 |
| 5 | 48 |

### 关卡 vs 敌人属性

| 关 | ATK | HP Rest | HP Spike |
|----|-----|---------|----------|
| 0 | 5 | 24 | 36 |
| 5 | 15 | 59 | 89 |
| 10 | 25 | 148 | 222 |

### 粮草 vs 战利品

| 当前粮 | 获得粮 | 补偿 |
|--------|--------|------|
| 10 | 75 | +45 |
| 50 | 55 | +25 |
| 100 | 30 | +0 |

---

## ✅ 验证清单

运行这个命令来验证一切：

```csharp
// 在Unity编辑器中：
// 1. 添加 BalanceValidationTester 组件到场景
// 2. 右键 > "📊 生成所有验证数据"
// 3. 检查控制台输出
```

**应该看到**:
- ✅ 所有卡牌效率都在 4-15 范围内
- ✅ 曲线检查: 凸形(正常)
- ✅ 穿透伤害检查: 安全
- ✅ ATK线性，HP指数
- ✅ 波峰波谷交替

---

## 🔧 常见修改

### 想要修改基础伤害？

编辑 `GameBalanceCalculator.cs`:
```csharp
private const float GRAIN_BASE_VALUE = 5f;  // 改这个数字
```

### 想要修改敌人难度？

编辑 `GameBalanceCalculator.cs`:
```csharp
const int ATK_BASE = 5;           // 敌人基础ATK
const int ATK_GROWTH = 2;         // 每关增长
const float HP_BASE = 30f;        // 敌人基础HP
const float HP_MULTIPLIER = 1.2f; // 每关乘数
```

### 想要修改战利品补偿？

编辑 `DynamicLootSystem.cs`:
```csharp
float compensationFactor = deficit * 0.5f;  // 改这个系数(0.5)
```

---

## 📁 文件位置

```
Assets/
├── _Scripts/
│   └── Systems/
│       ├── GameBalanceCalculator.cs        ← 核心定价
│       ├── DynamicLootSystem.cs            ← 战利品
│       └── BalanceValidationTester.cs      ← 验证
└── Resources/
    └── Data/
        ├── CardBalanceData_v1.csv          ← 卡牌表
        └── LevelData_EnemyProgression.csv  ← 敌人表
```

---

## 📚 完整文档

- `BALANCE_SYSTEM_GUIDE.md` - 详细设计文档
- `BALANCE_CHECKLIST.md` - 集成清单
- 本文件 - 快速参考

---

**最后更新**: 2026年1月3日

