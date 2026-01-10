# 🎯 节点分页事件系统 - 快速集成指南

**快速开始**：按照以下步骤 5 分钟内完成基本集成测试

---

## ✅ 第一步：代码编译验证

### 检查清单

在 Unity Editor 中打开 Console，检查是否有编译错误：

```
✅ NodeEventPoolManager.cs       - 编译通过
✅ EventPageUIEffects.cs          - 编译通过
✅ GameManager.cs (修改)          - 编译通过
✅ UIManager.cs (修改)            - 编译通过
✅ DataManager.cs (修改)          - 编译通过
```

如果有红色错误，请检查：
1. 是否缺少 `using` 语句
2. 是否有拼写错误
3. 是否缺少必要的类/方法

---

## 🎨 第二步：Canvas 结构配置

### 必要的 UI 元素

在你的 `GamePlayPanel` Canvas 中，确保存在以下物体（名称必须精确匹配）：

#### 进度显示
```
Canvas
└─ EventPanel
   ├─ ProgressText          [TMP_Text] 用于显示 "2/5"
```

#### 事件内容
```
Canvas
└─ EventPanel
   ├─ EventTitle           [TMP_Text] 事件标题
   ├─ EventContext         [TMP_Text] 事件内容
   └─ ContextText          [TMP_Text] (可选，同上)
```

#### 翻页按钮
```
Canvas
├─ PrevButton             [Button]   上一页按钮 (<)
└─ NextButton             [Button]   下一页按钮 (>)
```

#### 选项按钮
```
Canvas
├─ ButtonA                [Button]   选项 A
│  └─ Text               [TMP_Text] 选项 A 文本
└─ ButtonB                [Button]   选项 B
   └─ Text               [TMP_Text] 选项 B 文本
```

#### 完成相关
```
Canvas
├─ AllEventsCompleteButton     [Button]   "出发" / "确认" 按钮
├─ StatusBadge                 [Image]    ✅ 已完成标记（初始禁用）
└─ EventCompletionConfirmationPanel  [Panel]    确认窗口（初始禁用）
   └─ ConfirmButton            [Button]   窗口中的确认按钮
```

### 快速配置命令

如果找不到这些元素，可以在 Hierarchy 中快速创建：

```
右键 Canvas
→ Create Empty
→ 重命名为对应名称
→ Add Component → Button (if needed)
→ Add Component → Text - TextMeshPro (if needed)
```

---

## 🔌 第三步：脚本绑定

### 自动绑定

以下代码会自动寻找物体，无需手动绑定：

```csharp
// UIManager.cs 中的 FindButton() / FindText() 方法
// 会自动搜索 Canvas 子物体
```

### 手动绑定（如果自动寻找失败）

1. 在 UIManager Inspector 中找到以下公共字段
2. 拖拽相应的 UI 元素到字段

```csharp
public Button ButtonA;           // 拖拽选项 A 按钮
public Button ButtonB;           // 拖拽选项 B 按钮
public TMP_Text EventTitleText;  // 拖拽事件标题文本
public TMP_Text ContextText;     // 拖拽事件内容文本
```

---

## 🧪 第四步：快速测试

### 测试 1：基础翻页

```
1. 运行游戏
2. 进入任何节点事件
3. 验证：
   ✓ 显示第一个事件标题和内容
   ✓ 进度条显示 "1/X"
   ✓ NextButton 可点击（如果有多个事件）
   ✓ PrevButton 禁用（第一页）
```

### 测试 2：翻页交互

```
1. 点击 NextButton
2. 验证：
   ✓ 事件内容更新到第二个事件
   ✓ 进度条显示 "2/X"
   ✓ 按钮状态正确更新

3. 点击 PrevButton
4. 验证：
   ✓ 回到第一个事件
   ✓ 进度条显示 "1/X"
```

### 测试 3：选项选择

```
1. 点击选项 A
2. 验证：
   ✓ 按钮高亮为绿色
   ✓ Console 输出 "玩家选择已更新: 选项A"

3. 点击选项 B
4. 验证：
   ✓ 选项 B 按钮高亮为绿色
   ✓ 选项 A 按钮变为白色
   ✓ Console 输出 "玩家选择已更新: 选项B"
```

### 测试 4：资源不足置灰

```
如果事件选项消耗资源：
1. 手动修改资源数值（编辑器中修改 ResourceManager）
2. 让某个资源不足
3. 翻页到该事件
4. 验证：
   ✓ 消耗该资源的选项按钮变灰
   ✓ 点击灰色按钮无反应
   ✓ 资源足够的选项仍可点击
```

### 测试 5：全部完成

```
1. 处理所有事件
2. 验证：
   ✓ AllEventsCompleteButton 变为可用（绿色）
   ✓ 点击按钮时弹出确认窗口

3. 点击窗口中的确认按钮
4. 验证：
   ✓ Console 输出 "资源结算完成"
   ✓ 资源数值被正确应用
```

---

## 📋 Console 输出示例

成功运行时，Console 应该输出类似的日志：

```
📋 ✅ 初始化节点事件池：5 个事件
   首个事件: 第一次接触
   末尾事件: 庆功宴

📄 显示事件页面 1/5: 第一次接触
📊 进度条: 1/5

⬅️ 上一页按钮: 禁用
➡️ 下一页按钮: 启用

👆 玩家选择了选项A
🎯 玩家选择已更新: 选项A

📄 翻到下一页: 2/5

✅ 事件 1 已处理 (1/5)
📊 进度条已更新: 2/5

✅ 事件完成确认
💰 开始结算所有事件的资源变化...
✅ 所有事件资源结算完成
```

---

## 🔧 常见问题排查

### ❌ 按钮点击无反应

**原因**：Canvas 中找不到对应名称的按钮

**解决**：
```csharp
// 在 UIManager 中添加调试
Debug.Log("🔍 寻找 PrevButton...");
var prevButton = FindButton(canvasTransform, "PrevButton");
Debug.Log(prevButton != null ? "✅ 找到" : "❌ 未找到");
```

### ❌ 显示空白或错误

**原因**：EventData 为空或事件不存在

**解决**：
```
// 检查 CSV 数据
// 确保 EventTable_v2 已正确加载
// 在 DataManager Console 中查看加载信息
```

### ❌ 资源没有标红

**原因**：TMP_Text 没有启用 richText

**解决**：
```csharp
// 在代码中确保启用：
buttonText.richText = true;
```

### ❌ 翻页时出现异常

**原因**：EventPoolManager 未初始化

**解决**：
```
// 确保在 StartNodeStoryFlow() 中调用了：
// InitializeNodeEventPool(panel.FirstEventID);
```

---

## 📊 调试技巧

### 打印事件池状态

```csharp
// 在任何地方调用此方法查看当前状态
NodeEventPoolManager.Instance.PrintDebugInfo();

// 输出示例：
// ========== 📋 NodeEventPoolManager 状态 ==========
// 总事件数: 5
// 当前页: 2/5
// 已处理: 1/5
// 未处理: 4
// 全部完成: ❌ 否
// ===============================================
```

### 监听方法调用

```csharp
// 在 GameManager 中添加断点或日志
// 验证方法是否被正确调用

// 例如：OnEventPageNext() 被调用时
Debug.Log("📄 翻到下一页");

// OnEventOptionSelected_v3() 被调用时
Debug.Log($"🎯 玩家选择: {(chooseA ? 'A' : 'B')}");
```

### 检查资源变化

```csharp
// 在 ApplyAllEventResults() 中查看每个资源的应用情况
Debug.Log($"📥 应用事件 {eventID} 的资源变化: {resultData}");

// 结算后验证资源值
Debug.Log($"粮食: {ResourceManager.Instance.Grain}");
Debug.Log($"铠甲: {ResourceManager.Instance.Armor}");
Debug.Log($"信念: {ResourceManager.Instance.Belief}");
```

---

## ✅ 验收清单

在提交代码前，请确认以下项目：

### 功能验收
- [ ] 能正常显示第一个事件
- [ ] 翻页按钮正常工作
- [ ] 选项可以选择和切换
- [ ] 资源不足时按钮置灰
- [ ] 全部完成时显示确认窗口
- [ ] 确认后正确应用资源

### 代码质量
- [ ] 无编译错误
- [ ] 无运行时异常
- [ ] Console 输出正常的调试日志
- [ ] 符合代码风格

### 用户体验
- [ ] UI 反馈清晰（颜色、禁用状态等）
- [ ] 按钮点击有音效反馈
- [ ] 动效不刺眼或太快
- [ ] 文本清晰易读

---

## 🚀 进阶配置

### 调整动效参数

编辑 `EventPageUIEffects.cs`：

```csharp
// 按钮缩放强度
button.DOPunchScale(Vector3.one * 0.15f, ...)  // 改为 0.1f 或 0.2f

// 资源抖动强度
icon.DOShakePosition(0.3f, new Vector3(8, 8, 0), ...)  // 改为 5 或 10

// 动效持续时间
.SetDuration(0.2f)  // 改为 0.1f 或 0.3f
```

### 添加自定义音效

在 `PlayOptionClickFeedback()` 中：

```csharp
// 修改音效名称以使用其他音效
AudioManager.Instance.Play("UI_Click");  // 改为 "Option_Select"
```

---

## 📞 支持

如遇问题，请检查：

1. ✅ 是否按照上述步骤配置了 Canvas
2. ✅ 是否在 Unity 中看到了编译错误
3. ✅ 是否在 Console 中查看了调试日志
4. ✅ 是否检查了数据（EventTable_v2 是否加载）

**常见问题**：见本文档 "常见问题排查" 部分
