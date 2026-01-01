# 🚀 WestPlan 元旦冲刺 - 完整集成指南

## 📦 已创建的文件清单

### 🔧 核心脚本（已创建，无需修改）

| 文件名 | 功能 | 位置 |
|--------|------|------|
| DebugManager.cs | 4个作弊功能 | `Assets/_Scripts/Managers/` |
| DebugPanelUI.cs | Debug面板UI | `Assets/_Scripts/Managers/` |
| DamagePopup.cs | 飘字特效脚本 | `Assets/_Scripts/Managers/` |
| DebugTools.cs | 快速测试方法 | `Assets/_Scripts/Managers/` |
| RuntimeDebugInput.cs | 快捷键调试 | `Assets/_Scripts/Managers/` |

### 📝 已修改的文件

| 文件名 | 改动 | 位置 |
|--------|------|------|
| BattleManager.cs | +飘字效果（3处） | `Assets/_Scripts/Managers/` |
| EnemyTable.csv | 完善2个敌人数据 | `Assets/Resources/Data/` |

---

## ⚙️ 配置步骤（Editor中完成）

### 第一步：配置 DamagePopup.prefab

1. 打开 `Assets/Resources/UI/DamagePopup.prefab`
2. **重要**：在 Inspector 中，找到 DamagePopup GameObject
3. 点击 "Add Component" → 搜索 "DamagePopup" → 添加 **DamagePopup.cs** 脚本
4. 检查TextMeshProUGUI组件的设置：
   - ✅ Rich Text: 打开
   - ✅ Font Size: 36（推荐）
   - ✅ Alignment: Center

### 第二步：创建 Debug UI（Canvas中）

#### A. 创建 OpenDebugBtn（右上角）

```
右键 Canvas → UI → Button - TextMeshPro
改名为 OpenDebugBtn
```

**Position/Size:**
- Anchor Preset: TopRight
- Pos X: -30, Pos Y: -30
- Width: 60, Height: 60
- Text: "🔧"

#### B. 创建 DebugPanel

```
右键 Canvas → UI → Panel
改名为 DebugPanel
挂上 DebugPanelUI.cs 脚本
```

**Position/Size:**
- Width: 400, Height: 500
- Anchor: Center
- 背景色: 深灰色（可选）

#### C. 在 DebugPanel 中创建按钮

创建5个按钮，命名如下：
- `Button_InfiniteResources` - 文字: "资源无限"
- `Button_OneHitEnemy` - 文字: "秒杀敌人"
- `Button_SelfDestruct` - 文字: "自杀测试"
- `Button_JumpToEvent` - 文字: "强制跳关"
- `Button_CloseDebug` - 文字: "关闭"

#### D. 创建 InputField（用于输入事件ID）

```
右键 DebugPanel → UI → InputField - TextMeshPro
改名为 EventIDInput
```

**设置:**
- Placeholder Text: "输入事件ID"
- Text Component → Font Size: 20

### 第三步：配置 DebugPanelUI 的 Inspector 引用

**选中 DebugPanel，在 DebugPanelUI 组件中拖拽以下引用：**

```
BtnInfiniteResources ← Button_InfiniteResources
BtnOneHitEnemy ← Button_OneHitEnemy
BtnSelfDestruct ← Button_SelfDestruct
BtnJumpToEvent ← Button_JumpToEvent
BtnCloseDebug ← Button_CloseDebug
EventIDInput ← EventIDInput
DebugPanel ← DebugPanel (自己)
OpenDebugBtn ← OpenDebugBtn
```

### 第四步：将 RuntimeDebugInput 挂到场景

```
在Gameplay Scene中右键 → Create Empty GameObject
改名为 DebugInputHandler
挂上 RuntimeDebugInput.cs 脚本
```

### 第五步：确认 BattleCardUI 的 Rich Text

1. 打开 `Assets/Resources/UI/BattleCard_Prefab.prefab`（如果存在）
2. 选中 CostText 物体
3. 在 TextMeshPro 组件中找到 "Extra Settings"
4. ✅ 勾选 "Rich Text"

### 第六步：配置 Tooltips（资源图标）

为游戏界面的三个资源图标分别：
1. 选中图标 GameObject
2. 添加 **ResourceItem.cs** 组件
3. 设置 ChineseName:
   - 粮草图标: "粮草（行动资源，用于出牌和攻击防守）"
   - 护甲图标: "护甲（防线库存，抵挡敌方伤害）"
   - 信念图标: "信念（心理健康值，耗尽则死亡）"
4. 拖拽 TooltipObj（那个关不掉的弹窗）

---

## 🎮 快捷键一览表

在 Play 模式下按以下快捷键：

| 快捷键 | 功能 | 说明 |
|--------|------|------|
| **Ctrl + D** | 打印资源状态 | 在Console看当前粮草/护甲/信念 |
| **Ctrl + B** | 打印战斗状态 | 查看战斗中的所有数据 |
| **Ctrl + I** | 资源无限 | 粮草999、护甲999、信念999 |
| **Ctrl + K** | 秒杀敌人 | 敌人生命变1 |
| **Ctrl + S** | 自杀测试 | 信念变1，触发死亡 |
| **Ctrl + J** | 跳转事件2005 | 快速跳到指定事件 |
| **Ctrl + W** | 快速胜利 | 敌人生命变1（仅锁定） |

---

## 🧪 测试场景

### 冒烟测试（Smoke Test）

1. **启动游戏**
   ```
   运行 MainMenu Scene
   点击 "开始游戏"
   检查：是否进入 Gameplay 场景
   ```

2. **进入第一个事件**
   ```
   查看：EventWindow 是否显示事件标题
   查看：选项按钮是否可点击
   ```

3. **触发战斗**
   ```
   选择导致战斗的选项
   检查：BattlePanel 是否弹出
   检查：卡牌是否显示
   检查：敌人数据是否加载
   ```

4. **测试飘字**
   ```
   点击 "全军突击"（玩家攻击）
   查看：敌方是否出现红色伤害数字
   
   等待敌方回合
   查看：玩家是否出现橙色伤害数字
   ```

5. **测试Debug功能**
   ```
   按 Ctrl+I：资源变999
   按 Ctrl+K：敌人生命变1
   按 "全军突击"：敌人应该立即死亡
   检查：胜利面板是否弹出
   ```

### 完整流程测试

```
1. 开始游戏
2. 完成一个事件（选择战斗）
3. 赢下战斗
4. 查看结算面板
5. 回到事件流程
6. 重复至少一个节点（共12个）
7. 触发终局
```

---

## 🎯 飘字效果颜色方案

目前在BattleManager中配置如下：

| 颜色 | 用途 | 代码 |
|------|------|------|
| 🔴 红色 | 玩家输出伤害 | `Color.red` |
| 🟠 橙色 | 受到敌方伤害 | `new Color(1, 0.5f, 0)` |
| 🟢 绿色 | 治疗/回血 | `Color.green` |
| 🔵 蓝色 | 防御成功 | `Color.cyan` |

可在DamagePopup.cs中的 `FloatDuration` 和 `FloatHeight` 调整动画参数。

---

## 🐛 常见问题 & 解决

### Q: DamagePopup 没显示？
```
A: 检查以下几点：
1. DamagePopup.cs 是否挂到了 prefab 上？
2. Resources/UI/DamagePopup.prefab 是否存在？
3. Canvas 是否在场景中？
4. Console 中是否有报错？
```

### Q: Debug 按钮点不了？
```
A: 检查以下几点：
1. 所有按钮的 OnClick 事件是否绑定了？
2. DebugPanelUI 脚本是否在 DebugPanel 上？
3. 所有 UI 引用是否都拖拽了？
4. 按钮是否被其他物体遮挡？
```

### Q: 飘字位置不对？
```
A: DamagePopup.cs 中的 Show() 方法使用 Camera.main
   如果有多个相机，请改为目标相机引用
   或者改为固定的 UI Canvas 坐标
```

### Q: 快捷键不生效？
```
A: 检查以下几点：
1. RuntimeDebugInput.cs 是否挂到了 GameObject 上？
2. 该 GameObject 是否 Active？
3. 是否在 Play 模式下？
4. Input Manager 中是否定义了按键？
```

---

## 📊 数据配置速查

### CardTable.csv 结构
```
ID, Name, Type, SubType, Cost_Food, Cost_Armor, Power, Effect_ID, Effect_Val, Description
```

**已配置卡牌：**
- 1001-1003: Unit 类型（攻击单位）
- 2001-2002: Strategy Tactic（战术）
- 3001-3004: Strategy Auxiliary（辅助）

### EnemyTable.csv 结构
```
EnemyID, Name, Power, Description, Intent_Pattern
```

**已配置敌人：**
- 2001: 杂虏骑兵（战力15）- 高攻低血
- 2002: 匈奴重甲（战力20）- 高防低攻

### EventTable.csv 结构
```
ID, IsPeaceful, Title, Context, Opt_A, ... Opt_B, ... Effect_Type, OptB_Condition
```

**已配置事件：7个**（包含战斗、资源、赌博类型）

---

## ✅ 周四冲刺完成标志

- [x] Debug 面板 UI 完成
- [x] 4个作弊功能脚本完成
- [x] 飘字特效脚本完成
- [x] BattleManager 集成飘字（3处）
- [x] EnemyTable.csv 完善
- [ ] 在 Editor 中完成上述配置
- [ ] 运行测试并截图验证

---

## 🎬 下一步任务（周五）

1. **上午**：测试存档系统（Ctrl+S 保存，重启加载）
2. **下午**：优化胜利界面（战利品一个个弹出）
3. **晚上**：快速通关测试（用Debug工具跑完12个节点）

---

**生成时间**: 2026年1月1日
**工程师**: GitHub Copilot
**状态**: 🟢 代码集成完成，等待 Editor 配置
