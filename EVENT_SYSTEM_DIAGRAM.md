# 事件系统架构 - Mermaid 图表

## 1. 完整数据流转流程

```mermaid
graph TD
    A["📁 EventTable.csv<br/>(7条事件记录)"] -->|加载| B["DataManager<br/>AllEvents List"]
    B -->|GetRandomEvent| C["UIManager<br/>ShowNextEvent()"]
    
    C -->|检查IsPeaceful| D{事件类型}
    D -->|True| E["ShowPeacefulEvent<br/>(对话选项)"]
    D -->|False| F["EnterBattleLogic<br/>(进入战斗)"]
    
    E -->|按键点击| G["OnOptionABtn/B"]
    F -->|战斗结束| H["OnBattleEnded"]
    
    G -->|ResolveEventOption| I["计算结果<br/>Result1/Result2<br/>概率决策"]
    H -->|胜利| I
    
    I -->|ApplyMultiResources| J["执行资源变化<br/>Belief/Grain/Armor"]
    I -->|HandleEventEffect| K["执行特效<br/>JUMP/SWITCH/VICTORY"]
    
    J -->|GoToNextNode| L["推进进度<br/>Month++<br/>NodeIndex++"]
    K -->|GoToNextNode| L
    
    L -->|SaveGame| M["💾 PlayerPrefs<br/>保存进度"]
    L -->|ShowNextEvent| C
```

---

## 2. EventData 结构详解

```mermaid
graph TD
    A["EventData<br/>(CSV单行)"] -->|基础| B["ID: 2001<br/>IsPeaceful: true/false<br/>Title: 事件名称<br/>Context: 背景描述"]
    
    A -->|选项A| C["OptA_Text: 选项文本<br/>OptA_Res1_Txt: 结果文本<br/>OptA_Res1_Data: 资源数据<br/>OptA_Res2_Rate: 概率%<br/>OptA_Res2_Txt: 条件结果<br/>OptA_Res2_Data: 条件资源"]
    
    A -->|选项B| D["OptB_Text: 选项文本<br/>OptB_Res1_Txt: 结果文本<br/>OptB_Res1_Data: 资源数据<br/>OptB_Res2_Rate: 概率%<br/>OptB_Res2_Txt: 条件结果<br/>OptB_Res2_Data: 条件资源"]
    
    A -->|特殊| E["Effect_Type: 特效类型<br/>JUMP:EventID<br/>SWITCH_ROUTE_FANTASY<br/>GAME_OVER<br/>VICTORY<br/>NODE_END<br/><br/>OptB_Condition: 选项B条件<br/>ex. Grain>=50"]
```

---

## 3. 事件决议流程（重要！）

```mermaid
graph LR
    A["玩家选择<br/>OptionA/B"] --> B["Random 0-100"]
    B -->|Roll < Res2_Rate| C["触发Result2<br/>低概率路线"]
    B -->|Roll >= Res2_Rate| D["触发Result1<br/>高概率路线"]
    
    C -->|取Result2_Txt<br/>+ Result2_Data| E["字符串解析"]
    D -->|取Result1_Txt<br/>+ Result1_Data| E
    
    E -->|数据解析<br/>支持格式| F["DAMAGE:50<br/>ADD_RES:belief:20<br/>SUB_RES:grain:10<br/>等"]
    F -->|ApplyMultiResources| G["更新资源状态"]
    F -->|HandleEventEffect| H["执行特效命令"]
    H -->|重要| I["JUMP:NextID<br/>或<br/>NODE_END强制结算"]
```

---

## 4. 节点循环流程

```mermaid
graph TD
    A["游戏开始<br/>Node=1,Month=1"] --> B["DisplayNode:<br/>地名+月份"]
    B --> C["获取事件<br/>GetRandomEvent"]
    
    C --> D{已触发<br/>节点事件数}
    D -->|< 3个| E["显示随机事件"]
    D -->|>= 3个| F["触发结算<br/>TriggerSettlement"]
    
    E -->|选择执行| G["ResolveEventOption"]
    G -->|资源变化| H["ApplyMultiResources"]
    G -->|特效检查| I["HandleEventEffect"]
    
    H -->|GoToNextNode| J["Month++<br/>Node++<br/>重置计数"]
    I -->|JUMP:ID| J
    I -->|VICTORY| K["触发胜利结局"]
    I -->|GAME_OVER| L["触发失败结局"]
    
    J -->|SaveGame| M["保存进度"]
    M -->|Node<12?| C
    M -->|Month>=12| K
    
    F -->|显示小结| N["ShowNodeSummary"]
    N -->|继续| C
```

---

## 5. 状态机与分支路线

```mermaid
graph TD
    A["UIState.MainMenu"] -->|新游戏/继续| B["UIState.Gameplay<br/>历史线/幻想线<br/>IsFantasyLine=false/true"]
    
    B -->|触发事件| C{IsPeaceful}
    C -->|True| D["显示对话框<br/>等待选择"]
    C -->|False| E["进入战斗<br/>UIState.Battle"]
    
    D -->|选择方案| F["UIState.ResultScreen"]
    E -->|战斗结束| G["显示战斗结果<br/>获得掉落"]
    G -->|继续| F
    
    F -->|确认| H{检查胜利条件}
    H -->|Month>=12<br/>或<br/>触发VICTORY| I["UIState.Ending<br/>胜利/失败"]
    H -->|否| J["推进进度<br/>返回Gameplay"]
    
    I -->|菜单| K["MainMenu"]
    
    J -->|SWITCH_ROUTE_FANTASY| L["切换为幻想线<br/>IsFantasyLine=true"]
```

---

## 6. 资源变化与特效系统

```mermaid
graph TD
    A["Effect_Type字符串"] --> B{解析特效}
    B -->|"JUMP:999"| C["forcedNextEventID = 999<br/>下次强制显示ID=999事件"]
    B -->|"SWITCH_ROUTE_FANTASY"| D["IsFantasyLine = true<br/>切换为幻想线路"]
    B -->|"GAME_OVER"| E["TriggerEnding<br/>Bad_End_Event<br/>游戏失败"]
    B -->|"VICTORY"| F["TriggerEnding<br/>Victory_Event<br/>触发胜利"]
    B -->|"NODE_END"| G["CurrentEventCount = 999<br/>强制进入结算"]
    B -->|空/其他| H["无特效<br/>继续正常流程"]
    
    I["Resource_Data字符串<br/>如: 'DAMAGE:50|<br/>ADD_RES:belief:10'"] -->|ApplyMultiResources| J["逐条解析数据"]
    J -->|DAMAGE:X| K["ResourceManager<br/>Belief -= X"]
    J -->|ADD_RES:type:X| L["ResourceManager<br/>按类型增加"]
    J -->|SUB_RES:type:X| M["ResourceManager<br/>按类型减少"]
```

---

## 7. 当前数据量统计

```mermaid
graph LR
    A["EventTable.csv<br/>总计: 7条事件"] --> B["ID范围: 2001-6005"]
    
    B --> C["事件类型分布"]
    C --> D1["BATTLE型: 2个"]
    C --> D2["RESOURCE型: 3个"]
    C --> D3["GAMBLE型: 1个"]
    C --> D4["VICTORY型: 1个"]
    
    E["节点数: 12个"] --> F["所需覆盖"]
    E -->|节点最少事件数| G["12 × 3 = 最少36个事件<br/>当前仅7个"]
    
    H["现状: 7个事件"] -->|可覆盖| I["1个节点(3事件)"]
    H -->|占比| J["仅需要总事件的19%"]
```

---

## 8. 当前系统的关键问题

```mermaid
graph TD
    A["🔴 核心架构问题"] --> B["问题1: 随机事件池<br/>GetRandomEvent无权重<br/>无法控制事件顺序"]
    A --> C["问题2: 事件内容严重不足<br/>7个事件 vs 12个节点<br/>无法支撑完整剧本"]
    A --> D["问题3: 结果解析脆弱<br/>字符串解析格式<br/>容易出错、难维护"]
    A --> E["问题4: 分支逻辑隐式<br/>效果通过字符串指定<br/>无显式事件图谱"]
    A --> F["问题5: 没有事件链系统<br/>事件间无依赖关系<br/>无法构建复杂叙事"]
    A --> G["问题6: 选项条件单一<br/>仅OptB_Condition<br/>无复杂条件判定系统"]
    
    B -->|影响| B1["用户体验重复"]
    C -->|影响| C1["无法达到12月目标"]
    D -->|影响| D1["bug风险高"]
    E -->|影响| E1["难以扩展剧情"]
    F -->|影响| F1["无法构建真正的<br/>选择-分支叙事"]
    G -->|影响| G1["选项功能受限"]
```

---

## 9. 推荐优化方向

```mermaid
graph TD
    A["🟢 优化建议"] --> B["建议1: 对象化事件系统<br/>EventData改用枚举+对象<br/>废弃字符串解析"]
    A --> C["建议2: 事件链与权重<br/>明确事件前置/后置关系<br/>按权重/队列加载"]
    A --> D["建议3: 分枝图谱系统<br/>完整定义事件决策树<br/>可视化编辑器"]
    A --> E["建议4: 条件系统升级<br/>支持复合条件判定<br/>resource_check + flag_check"]
    A --> F["建议5: 内容扩展规划<br/>12个节点 × 3+ 事件<br/>= 至少36-50个事件"]
    A --> G["建议6: 编辑工具<br/>事件编辑器UI<br/>无需手动改CSV"]
    
    B -->|改进| B1["类型安全<br/>易于维护"]
    C -->|改进| C1["叙事连贯性<br/>可控重复"]
    D -->|改进| D1["设计透明<br/>易于扩展"]
    E -->|改进| E1["玩法深度<br/>策略性"]
    F -->|改进| F1["完整体验<br/>达到目标"]
    G -->|改进| G1["提高效率<br/>降低出错"]
```

---

## 10. CSV数据行为示例

### 当前格式（已精简）
```
ID,IsPeaceful,Title,Context,OptA_Text,OptA_Res1_Txt,OptA_Res1_Data,OptA_Res2_Rate,OptA_Res2_Txt,OptA_Res2_Data,...
2001,1,农民起义,民众遭压迫,安抚民众,获得支持,ADD_RES:belief:20,20,激怒民众,SUB_RES:belief:50,...
```

### 优化后建议
```
EventID | EventName    | Type    | ChainID | Prerequisites | OptA.Result | OptB.Result | Effect
--------|--------------|---------|---------|---------------|-------------|-------------|--------
2001    | 农民起义      | CHOICE  | 2002    | Month<3       | Belief+20   | Belief-50   | -
2002    | 民心叛乱      | BATTLE  | 2003    | Event:2001.A  | Battle:101  | -           | -
...
```

---

## 总结

| 维度 | 当前状态 | 评价 | 优先级 |
|------|---------|------|--------|
| **数据量** | 7个事件 | ⚠️ 严重不足 | 🔴 优先 |
| **架构** | 字符串解析 | ⚠️ 脆弱易错 | 🔴 优先 |
| **分支系统** | 隐式JUMP | ⚠️ 无法管理 | 🟠 次优 |
| **条件系统** | 仅OptB_Condition | ⚠️ 功能有限 | 🟠 次优 |
| **扩展性** | 低效率 | ⚠️ 难维护 | 🟡 可后续 |
| **运行稳定性** | 正常 | ✅ 无bug | 🟢 已达成 |

**核心建议**: 在添加36-50个新事件前，应该先重构事件系统的数据模型和解析逻辑。
