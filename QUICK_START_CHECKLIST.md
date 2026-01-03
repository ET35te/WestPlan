# 🚀 线性事件系统 - 快速启动清单

## ✅ 前置检查（2分钟）

### 1. 编译验证
```
✅ 确认0个编译错误
  • 打开 Assets/_Scripts/Managers/GameManager.cs
  • 打开 Assets/_Scripts/Managers/UIManager.cs  
  • 打开 Assets/_Scripts/Systems/ConditionEvaluator.cs
  • 都应该没有红色波浪线
```

### 2. CSV表验证
```
✅ 确认3个CSV表存在：
  • Assets/Resources/Data/EventTable_v2.csv
  • Assets/Resources/Data/StoryPanelTable.csv
  • Assets/Resources/Data/EndingTable.csv
```

### 3. UI元素验证
```
✅ MessagePanel 已在编辑器创建：
  • MessagePanel (容器，黑色半透明)
    ├── Title_Text (标题)
    ├── Content_Text (内容)
    └── ToBeContinue_Btn (继续按钮)

✅ UIManager已关联这些元素：
  • MessagePanel → 
  • MessageText → Content_Text
  • ToBeContinueBtn → ToBeContinue_Btn
```

---

## 🎮 运行测试（3步）

### 步骤1：点击Play
```
1. 打开游戏主菜单场景
2. 点击 Play 按钮
3. 点击"新游戏"按钮
```

### 步骤2：观察剧情面板
```
预期看到：
  ✅ MessagePanel 弹出
  ✅ 标题：「丝路使者」
  ✅ 内容：「大汉建初元年秋。班超奉皇帝之命出使西域...」
  ✅ 按钮："继 续"
```

### 步骤3：继续到事件
```
4. 点击"继 续"按钮
5. 预期看到：
  ✅ 剧情面板关闭
  ✅ 事件1001 出现
    标题：「遭遇匈奴驿卒」
    内容：「前路遭遇来自西域的匈奴驿卒」
    按钮A：「选择战斗」→ NextID: 1002
    按钮B：「选择贿赂」→ NextID: 1003
```

---

## 🔑 快捷键速查

| 快捷键 | 功能 | 用途 |
|--------|------|------|
| **Shift+T** | 重新启动线性事件系统 | 测试剧情面板 |
| **Shift+Q** | 跳过剧情面板 | 快速进入事件 |
| **Shift+D** | 打印资源状态 | 查看Belief/Grain/Armor |
| **Shift+I** | 无限资源 | 测试条件判定 |
| **Shift+K** | 秒杀敌人 | 战斗测试 |

**在 Play 模式下按这些按钮即可**

---

## ✨ 测试验收清单

运行以下测试并检查所有✅项：

### Test 1: 基础流程
```
□ 新游戏 → 看到剧情面板
□ 点继续 → 看到事件1001
□ 看到两个选项按钮
□ 无崩溃无错误
```

### Test 2: 分支有效性
```
□ 选选项A "选择战斗"
  → 结果文本: "士兵奋勇迎战"
  → 资源变化: 伤害30 信念+10
  → 点确认后跳到事件1002 "激烈的战斗"

□ 重新开局，选选项B "选择贿赂"
  → 结果文本: "付出金钱换取通行"
  → 资源变化: 粮食-20 信念-5
  → 点确认后跳到事件1003 "权衡之后"
```

### Test 3: 无随机性
```
□ 新游戏 → 选A → 事件1002（记住结果）
□ 新游戏 → 选A → 事件1002（结果应完全相同）
□ 新游戏 → 选A → 事件1002（第3次结果也应相同）

三次流程100%相同？✅ PASS / ❌ FAIL
```

### Test 4: 事件链跳转
```
从事件1002开始：
□ 选A → 事件1004 "抵达楼兰城"
□ 选B → 事件1005 "全身而退"

从事件1003开始：
□ 选A → 事件1004 "抵达楼兰城"  
□ 选B → 事件9999 "放弃西行（失败）"
```

### Test 5: 节点结束
```
到达事件1004：
□ 两个选项的NextID都是 -1
□ 选任意选项 → 显示结果
□ 点确认 → 进入 NodeSummaryPanel（节点结算）

控制台应输出：📍 节点事件链结束
```

---

## 🐛 快速排查

如果有问题，按这个顺序排查：

```
问题：剧情面板不出现
  → 检查 MessagePanel 在 Inspector 中关联了吗？
  → 检查 MessageText 在 Inspector 中关联了吗？
  → 查看 Console 中有没有红色错误

问题：事件不显示
  → 检查 EventTable_v2.csv 中是否有ID=1001的事件
  → 检查 StoryPanelTable.csv 中是否有NodeID=0的记录
  → 查看 Console 是否有 "❌ 找不到事件ID" 的错误

问题：按钮无反应
  → 检查按钮是否被置灰（interactable=false）
  → 检查 ButtonA 和 ButtonB 在 Inspector 中关联了吗？
  → 试试快捷键 Shift+Q 跳过面板看是否能进入事件

问题：资源不变化
  → 检查 Console 是否有资源变化的日志输出
  → 手动检查 PlayerPrefs 中是否有保存的数据
  → 试试快捷键 Shift+D 查看当前资源状态
```

---

## 📋 详细文档速查

| 文档 | 用途 | 何时查阅 |
|------|------|---------|
| [LINEAR_NARRATIVE_IMPLEMENTATION.md](LINEAR_NARRATIVE_IMPLEMENTATION.md) | 完整API文档 | 需要改代码 |
| [QUICK_TEST_GUIDE.md](QUICK_TEST_GUIDE.md) | 详细测试场景 | 测试中有问题 |
| [EVENT_SYSTEM_REFACTOR_COMPLETION.md](EVENT_SYSTEM_REFACTOR_COMPLETION.md) | 架构说明 | 需要理解设计 |

---

## 🎯 成功标志

**当你看到这些，说明系统正常工作：**

✅ 游戏启动后自动显示剧情面板（不是直接进入事件）
✅ 点继续后显示事件1001（"遭遇匈奴驿卒"）
✅ 选不同选项导向不同事件（1002 vs 1003）
✅ 同样的选择重复3次结果完全相同
✅ 资源数字正确增减
✅ 无崩溃，无红色错误

**所有项都✅？恭喜！线性事件系统已成功启用！🎉**

---

## 📞 如有问题

参考对应的详细文档：
- **架构问题** → EVENT_SYSTEM_REFACTOR_COMPLETION.md
- **API问题** → LINEAR_NARRATIVE_IMPLEMENTATION.md
- **测试问题** → QUICK_TEST_GUIDE.md

或直接查看 Console 的错误信息，通常会有清晰的提示。

---

**预计测试时间**：30-45分钟
**难度等级**：⭐ (简单 - 主要是观察)
**Ready?** 👉 点击 Play 按钮开始！

