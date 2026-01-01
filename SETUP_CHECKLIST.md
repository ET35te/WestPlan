# ✅ 周四冲刺设置检查清单

## 🔧 已完成的代码工作

- ✅ **DebugManager.cs** - 4个作弊功能（资源无限、秒杀敌人、自杀测试、强制跳关）
- ✅ **DebugPanelUI.cs** - Debug面板UI绑定脚本
- ✅ **DamagePopup.cs** - 飘字特效脚本（含工厂方法）
- ✅ **BattleManager.cs** - 三处伤害结算集成飘字（OnAttackCmd、ApplyCardEffect、EnemyTurnRoutine）
- ✅ **EnemyTable.csv** - 2个敌人数据完善（15战力、20战力）
- ✅ **GameManager.cs** - forcedNextEventID已为public

---

## 📋 需要在Editor中完成的操作

### 1️⃣ DamagePopup.prefab 配置

进入 `Assets/Resources/UI/DamagePopup.prefab`，确保：
- [ ] 有 TextMeshProUGUI 组件
- [ ] **挂载 DamagePopup.cs 脚本** 到该prefab的根GameObject
- [ ] RectTransform设置：
  - SizeDelta: 200 x 50
  - AnchorMin: (0.5, 0.5)
  - AnchorMax: (0.5, 0.5)

### 2️⃣ Debug UI 配置

在 Gameplay Scene 的 Canvas 下，确保：
- [ ] OpenDebugBtn（位置：右上角，60x60）
- [ ] DebugPanel（包含以下子物体）：
  - [ ] Button_InfiniteResources
  - [ ] Button_OneHitEnemy  
  - [ ] Button_SelfDestruct
  - [ ] Button_JumpToEvent
  - [ ] Button_CloseDebug
  - [ ] EventIDInput（TMP_InputField）
  
**将DebugPanelUI.cs脚本挂到DebugPanel上，并在Inspector中拖拽以下引用：**
- [ ] BtnInfiniteResources → Button_InfiniteResources
- [ ] BtnOneHitEnemy → Button_OneHitEnemy
- [ ] BtnSelfDestruct → Button_SelfDestruct
- [ ] BtnJumpToEvent → Button_JumpToEvent
- [ ] BtnCloseDebug → Button_CloseDebug
- [ ] EventIDInput → EventIDInput
- [ ] DebugPanel → DebugPanel (自己)
- [ ] OpenDebugBtn → OpenDebugBtn

### 3️⃣ BattleCardUI 确认

检查 BattleCard_Prefab：
- [ ] 在 CostText 的 TextMeshPro 组件中，开启 ✅ Rich Text

### 4️⃣ Tooltips 实装

给资源图标挂载 ResourceItem.cs：
- [ ] 粮草图标 → ResourceItem.cs（ChineseName: "粮草"）
- [ ] 护甲图标 → ResourceItem.cs（ChineseName: "护甲"）
- [ ] 信念图标 → ResourceItem.cs（ChineseName: "信念"）

为每个ResourceItem配置Tooltip对象的引用。

---

## 🧪 测试清单

### 战斗测试
- [ ] 玩家攻击时，敌方生命条减少并弹出红色伤害数字
- [ ] 敌方攻击时，玩家生命条减少并弹出橙色伤害数字
- [ ] 卡牌伤害时，敌方受伤并弹出红色伤害数字
- [ ] 完美防御时，出现"BLOCK"蓝色飘字

### Debug功能测试
- [ ] 点击右上角Debug按钮，弹出Debug面板
- [ ] 资源无限：点击后粮/甲/信念变999
- [ ] 秒杀敌人：点击后敌人生命变1，下一次攻击胜利
- [ ] 自杀测试：点击后信念变1，应触发死亡
- [ ] 强制跳关：输入2005，应跳到ID为2005的事件

### 胜利结算
- [ ] 战斗胜利后，弹出结算面板
- [ ] 战利品（粮2甲1）正确加入库存
- [ ] "确认"按钮回到事件界面

---

## 🚀 优化建议

1. **飘字位置微调** - 若需要精确定位敌人/玩家位置，改进 Camera.main.transform.position 逻辑
2. **飘字颜色方案**：
   - 红色 = 玩家输出伤害
   - 橙色 = 受到伤害
   - 绿色 = 治疗/回血
   - 蓝色 = 防御成功
   - 黄色 = 资源获得

3. **音效** - 可在DamagePopup.SpawnPopup后添加AudioManager.PlaySFX()

---

## 📊 当前数据状态

| 文件 | 卡牌数 | 敌人数 | 事件数 | 状态 |
|------|--------|--------|--------|------|
| CardTable.csv | 9 | - | - | ✅ 完整 |
| EnemyTable.csv | - | 2 | - | ✅ 完整 |
| EventTable.csv | - | - | 7 | ✅ 完整 |

---

## 📅 下一步

- [ ] **周四晚上** - 完成上述Editor配置和测试
- [ ] **周五上午** - 测试存档系统  
- [ ] **周五下午** - 优化胜利界面
- [ ] **周末** - 打包Build
