# 🎁 胜利界面战利品逐个弹出 - 设置指南

## 📋 概述

胜利界面已改进，现在战利品图标会**逐个弹出**，间隔 0.3 秒，每个图标有缩放动画效果。

---

## 🔧 需要在 Editor 中设置

### 步骤 1: 打开 Result_Panel

1. 打开 MainMenu 场景或游戏场景
2. Hierarchy 中找到 `Canvas`
3. 展开找到 `Result_Panel`

### 步骤 2: 在 Result_Panel 下添加 3 个战利品图标

在 `Result_Panel` 内创建以下 GameObject 和 Image 组件：

```
Result_Panel
├── Result_Text (已存在)
├── ConfirmResultBtn (已存在)
├── Loot_Food (新增)
│   └── Image 组件 (显示粮食图标)
├── Loot_Armor (新增)
│   └── Image 组件 (显示盾甲图标)
└── Loot_XP (新增)
    └── Image 组件 (显示经验图标)
```

### 步骤 3: 配置每个战利品图标

对于 **Loot_Food**、**Loot_Armor**、**Loot_XP** 各执行一次：

1. **选中该 GameObject**
2. **添加 Image 组件**（Add Component → Rendering → Image）
3. **设置 Image Source**：
   - 如果有 Sprites 文件夹中的图标，拖入即可
   - 否则可以使用默认方块（white square）
4. **RectTransform 配置**：
   - 建议放在屏幕下方或侧边
   - 大小：建议 100x100 像素
   - 位置示例：
     - Loot_Food: (-150, -100)
     - Loot_Armor: (0, -100)
     - Loot_XP: (150, -100)

### 步骤 4: 命名很重要！

**一定要确保名称正确**，否则代码无法找到：
- ✅ `Loot_Food`
- ✅ `Loot_Armor`
- ✅ `Loot_XP`

---

## ✅ 测试效果

1. Play 模式进入游戏
2. 参加战斗并赢得胜利
3. 观察结果面板：
   - 第 1 个图标出现（0.5s 后）
   - 第 2 个图标出现（+0.3s）
   - 第 3 个图标出现（+0.3s）
   - 每个图标都有 **缩放动画**（从小到大）

**Console 输出示例**：
```
🎁 显示战利品 0: Loot_Food
🎁 显示战利品 1: Loot_Armor
🎁 显示战利品 2: Loot_XP
✅ 所有战利品已显示
```

---

## 🎨 动画参数调整

如果要改变动画效果，编辑 `ShowLootSequence()` 中的参数：

```csharp
float duration = 0.3f;  // 单个图标弹出时长（秒）
yield return new WaitForSeconds(0.3f);  // 图标间隔（秒）
```

---

## 🔔 常见问题

### Q: 图标没有显示？
A: 检查：
1. GameObject 名称是否正确（Loot_Food / Loot_Armor / Loot_XP）
2. 是否添加了 Image 组件
3. Console 中是否有错误信息

### Q: 动画太快/太慢？
A: 修改 `duration` 和 `WaitForSeconds` 的值：
- 快速：改为 0.2f
- 缓慢：改为 0.5f

### Q: 想要改变位置？
A: 在 Editor 中直接调整 RectTransform 的 Position 值

---

## 代码位置

- 文件: `Assets/_Scripts/Managers/UIManager.cs`
- 方法: `ShowLootSequence()` (第 324 行后)

---

**完成后，战利品弹出效果就会自动工作了！** 🎉
