# 🎯 快速参考卡 (Quick Reference)

## 🔧 脚本速查表

### 作弊功能调用

```csharp
// 资源无限
DebugManager.Instance.CheatInfiniteResources();
// Result: Belief=999, Grain=999, Armor=999

// 秒杀敌人
DebugManager.Instance.CheatOneHitEnemy();
// Result: EnemyUnitCount=1

// 自杀测试
DebugManager.Instance.CheatSelfDestruct();
// Result: Belief=1 → 触发死亡事件

// 强制跳关
DebugManager.Instance.CheatJumpToEvent(2005);
// Result: 跳转到 EventID=2005 的事件
```

### 飘字调用

```csharp
// 简单调用
DamagePopup.SpawnPopup($"-{damage}", worldPos, Color.red);

// 可用颜色
Color.red              // 🔴 输出伤害
Color.green            // 🟢 治疗回血
Color.cyan             // 🔵 防御成功
new Color(1, 0.5f, 0) // 🟠 受伤害
```

### 快捷键表

| 快捷键 | 快捷键 | 效果 |
|--------|--------|------|
| Ctrl+D | Debug | 打印资源状态 |
| Ctrl+B | Battle | 打印战斗状态 |
| Ctrl+I | Infinite | 资源无限 |
| Ctrl+K | Kill | 秒杀敌人 |
| Ctrl+S | Self-destruct | 自杀测试 |
| Ctrl+J | Jump | 跳转事件2005 |
| Ctrl+W | Win | 快速胜利 |

---

## 📂 文件结构

```
Assets/
├── _Scripts/Managers/
│   ├── DebugManager.cs ⭐
│   ├── DebugPanelUI.cs ⭐
│   ├── DamagePopup.cs ⭐
│   ├── DebugTools.cs ⭐
│   ├── RuntimeDebugInput.cs ⭐
│   ├── BattleManager.cs (修改)
│   └── ... (其他脚本)
│
├── Resources/Data/
│   ├── CardTable.csv ✅
│   ├── EnemyTable.csv (修改) ✅
│   └── EventTable.csv ✅
│
└── Resources/UI/
    └── DamagePopup.prefab (需配置)
```

---

## 🎮 快速测试流程

### 5分钟快速验证

```
1. Play 进入游戏 (Main Menu)
2. 点击 开始游戏
3. 看到事件对话框
4. 选择导致战斗的选项
5. 进入 Battle Panel
6. 按 Ctrl+I (资源无限)
7. 按 Ctrl+K (秒杀敌人)
8. 点击 "全军突击" 按钮
   → 看到红色 "-5" 飘字
9. 敌人死亡
   → 看到胜利弹窗
10. 点击 确认
    → 回到事件流程
```

### 完整流程测试

```
1. 新游戏 → 完成1个事件 → 触发战斗
2. 战斗中测试：
   - 玩家攻击 (看红色伤害)
   - 敌方攻击 (看橙色伤害)
   - 出牌伤害 (看红色伤害)
3. 胜利结算 → 获得战利品
4. 回到事件 → 下一个节点
5. 重复至少12个节点
```

---

## 🔑 UI 引用映射

```
Canvas
├── OpenDebugBtn (右上角)
│   └── 点击 → 打开 DebugPanel
│
└── DebugPanel
    ├── Button_InfiniteResources → CheatInfiniteResources()
    ├── Button_OneHitEnemy → CheatOneHitEnemy()
    ├── Button_SelfDestruct → CheatSelfDestruct()
    ├── Button_JumpToEvent → CheatJumpToEvent(inputID)
    ├── Button_CloseDebug → 关闭面板
    └── EventIDInput → 事件ID输入框
```

---

## 💾 数据值速查

### 资源范围
```
Belief (信念): 0-999 (HP)
Grain (粮草): 0-999 (行动值)
Armor (护甲): 0-999 (库存)
```

### 战斗数值
```
玩家伤害: 5 (基础攻击)
敌方伤害: EnemyPower * 0.2 - PlayerArmor
防御加甲: +5 (正常) 或 +2 (断粮)
```

### 敌人数据
```
2001 杂虏骑兵: Power=15 (高攻低血)
2002 匈奴重甲: Power=20 (高防低攻)
```

### 卡牌效果代码
```
"ADD_RES"      → 获得粮草
"ADD_ARMOR"    → 获得护甲
"DRAW_SELF"    → 抽取卡牌
"DMG_ENEMY"    → 伤害敌人
```

---

## 🐛 快速排障

| 症状 | 原因 | 解决 |
|------|------|------|
| 飘字不显示 | prefab 没挂脚本 | 添加 DamagePopup.cs |
| Debug 面板打不开 | Button 事件未绑定 | 检查 OnClick 绑定 |
| 快捷键无效 | RuntimeDebugInput 未 Active | 确认脚本挂载 |
| 伤害数据错误 | CSV 编码错误 | 使用 UTF-8 编码 |
| 事件跳转失败 | EventID 不存在 | 检查 EventTable.csv |

---

## 📊 进度跟踪

### 周四
- ✅ 上午：Debug 系统 + UI
- ✅ 下午：飘字系统 + BattleManager 集成
- ⏳ 晚上：Editor 配置 (待执行)

### 周五
- ⏳ 上午：存档测试
- ⏳ 下午：胜利界面优化
- ⏳ 晚上：快速通关验证

### 周末
- ⏳ 试玩与完整流程
- ⏳ Build 打包
- ⏳ 发布

---

## 💬 常用日志格式

在 BattleManager 中的日志：

```
LogToScreen("全军突击！造成 5 点伤害");
LogToScreen("<color=red>断粮强攻！信念-10，造成 2 点伤害</color>");
LogToScreen("<color=green>🏆 胜利！</color>");
LogToScreen("<color=red>受到 3 点伤害！</color>");
LogToScreen("完美防御！");
```

---

## 🚀 单行快速开启

```csharp
// 在任何 MonoBehaviour 的 Start() 中
if (DebugManager.Instance == null) 
{
    var debugObj = new GameObject("DebugManager");
    debugObj.AddComponent<DebugManager>();
}
```

---

**最后更新**: 2026年1月1日  
**版本**: v1.0  
**状态**: ✅ 可用
