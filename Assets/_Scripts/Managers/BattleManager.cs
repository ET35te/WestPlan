using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("--- ⚖️ 战斗配置 ---")]
    public int DefaultUnitCount = 5;
    public int VictoryLootFood = 2;
    public int VictoryLootArmor = 1;

    // ==============================
    // 👉 UI 引用
    // ==============================
    [Header("--- UI 引用 (请手动拖拽) ---")]
    public TMP_Text Text_Player_Food;
    public TMP_Text Text_Player_Armor;
    public TMP_Text Text_Player_Unit; // 现在代表信念/血量
    public TMP_Text Text_Enemy_Unit;
    public TMP_Text BattleLogText;

    [Header("--- 🔥 新增 UI ---")]
    public TMP_Text Text_Enemy_Intent; // 显示敌人意图

    public Button AttackBtn;
    public Button DefendBtn;
    public Button SkipBtn;
    public Button ConfirmPlayCardBtn; 

    [Header("--- 容器 ---")]
    public Transform HandAreaTransform;
    public GameObject CardPrefab; 

    [Header("--- 战斗数据 ---")]
    public int PlayerUnitCount; // 对应：信念 (Belief)
    public int PlayerFood;      // 对应：战斗内可用粮 (Grain)
    public int PlayerArmor;     // 对应：战斗内叠加甲 (Armor)
    
    public int EnemyUnitCount;
    public int EnemyFood = 5; 
    public int EnemyArmor = 2;

    // 库存数据 (从 ResourceManager 读来的)
    private int stockFood;
    private int stockArmor;

    public List<DataManager.CardData> DrawPile = new List<DataManager.CardData>();
    public List<DataManager.CardData> HandPile = new List<DataManager.CardData>();
    public List<DataManager.CardData> DiscardPile = new List<DataManager.CardData>();

    private BattleCardUI currentSelectedCardUI;
    private bool isPlayerTurn;
    private int turnCount = 0;
    
    // 事件广播：解耦架构，通知 UI 打开结算面板
    public System.Action<string> OnBattleEnded;
    // 1. 增加一个状态枚举，让自己知道现在处于什么阶段
    public enum BattlePhase { Init, PlayerTurn, EnemyTurn, End }
    public BattlePhase CurrentPhase;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (UIManager.Instance != null && UIManager.Instance.BattlePanel != null)
            UIManager.Instance.BattlePanel.SetActive(false);
    }

    void Start()
    {
        if (ConfirmPlayCardBtn != null)
        {
            ConfirmPlayCardBtn.onClick.RemoveAllListeners();
            ConfirmPlayCardBtn.onClick.AddListener(OnConfirmPlayCardClicked);
            ConfirmPlayCardBtn.interactable = false;
            UpdateBtnText("请选牌");
        }

        if (AttackBtn) AttackBtn.onClick.AddListener(OnAttackCmd);
        if (DefendBtn) DefendBtn.onClick.AddListener(OnDefendCmd);
        if (SkipBtn) SkipBtn.onClick.AddListener(OnSkipCmd);
        
    }
        // =========================================================
    // 🎬 战斗初始化与开场流程
    // =========================================================

        public void StartBattle(DataManager.EnemyData enemyData)
    {
        // 1. 切换 UI 状态
        if (UIManager.Instance) UIManager.Instance.SwitchState(UIManager.UIState.Battle);

        // 2. 读取全局资源
        if (ResourceManager.Instance != null) {
            stockFood = ResourceManager.Instance.Grain;
            stockArmor = ResourceManager.Instance.Armor;
            PlayerUnitCount = ResourceManager.Instance.Belief; 
        } else {
            stockFood = 10; stockArmor = 5; PlayerUnitCount = 100;
        }

        // 3. 初始化战斗数值
        PlayerFood = 0; 
        PlayerArmor = 0; 
        
        if (enemyData != null) {
            EnemyUnitCount = enemyData.Power;
            LogToScreen($"遭遇：{enemyData.Name} (战力{EnemyUnitCount})");
        } else {
            EnemyUnitCount = 10;
            LogToScreen("遭遇伏兵！");
        }

        // 4. 准备卡牌
        InitializeDeck(); 
        ShuffleDeck();
        ClearHandUI();
        DrawCards(4);

        turnCount = 0;

        // ❌❌❌ 删掉下面这行！绝对不要直接调用 StartTurnRoutine！❌❌❌
        // StartCoroutine(StartTurnRoutine()); 

        // ✅✅✅ 改为调用开场表现流程 ✅✅✅
        StartCoroutine(BattleStartSequence());
    }
       // 🎞️ 战斗开场表现层逻辑
    IEnumerator BattleStartSequence()
    {
        CurrentPhase = BattlePhase.Init; // 标记状态

        // A. 锁住所有输入 (防止玩家在动画期间乱点)
        ConfirmPlayCardBtn.interactable = false;
        AttackBtn.interactable = false;
        DefendBtn.interactable = false;
        SkipBtn.interactable = false;

        // B. 第一阶段：遭遇提示
        // 这里的 ShowMessage 就是刚才在 UIManager 里加的方法
        if (UIManager.Instance) UIManager.Instance.ShowMessage("⚔️ 遭遇强敌！\n正在判定先手..."); 
        
        // ⏳ 表现层等待：给玩家 1.5秒 阅读时间
        yield return new WaitForSeconds(1.5f);

        // C. 第二阶段：逻辑计算 (瞬间完成)
        // 50% 概率玩家先手
        bool isPlayerFirst = Random.value > 0.5f;
        string startText = isPlayerFirst ? "<color=#00FF00>【我方先攻】</color>" : "<color=#FF0000>【敌方先攻】</color>";

        // D. 第三阶段：结果展示
        if (UIManager.Instance) UIManager.Instance.ShowMessage(startText);
        
        // ⏳ 表现层等待：给玩家 1.0秒 看清结果
        yield return new WaitForSeconds(1.0f);

        // E. 收尾：关闭弹窗，进入正式逻辑
        if (UIManager.Instance) UIManager.Instance.HideMessage();

        // 🚀 分流跳转
        if (isPlayerFirst) 
        {
            StartCoroutine(StartTurnRoutine());
        }
        else 
        {
            StartCoroutine(EnemyTurnRoutine());
        }
    }
        // =========================================================
    // ⚔️ 指令逻辑 (含绝粮反击修复)
    // =========================================================
    
    void OnAttackCmd() 
    { 
        if (!isPlayerTurn) return; 

        // --- 🔥 修复：绝粮死锁逻辑 ---
        if (PlayerFood >= 1) 
        {
            // 正常攻击
            PlayerFood -= 1; 
            int damage = 5; // 基础伤害 (可以改为 PlayerUnitCount / 10 等公式)
            EnemyUnitCount -= damage;
            LogToScreen($"全军突击！造成 {damage} 点伤害");
            // 🔥 飘字效果
            DamagePopup.SpawnPopup($"-{damage}", Camera.main.transform.position + Vector3.right * 2, Color.red);
        }
        else
        {
            // 绝境反击：扣血攻击
            int hpCost = Mathf.Max(1, Mathf.FloorToInt(PlayerUnitCount * 0.1f)); // 扣10%信念
            PlayerUnitCount -= hpCost;
            
            int weakDamage = 2; // 虚弱伤害
            EnemyUnitCount -= weakDamage;

            LogToScreen($"<color=red>断粮强攻！信念-{hpCost}，造成 {weakDamage} 点伤害</color>");
            // 🔥 飘字效果
            DamagePopup.SpawnPopup($"-{weakDamage}", Camera.main.transform.position + Vector3.right * 2, new Color(1, 0.5f, 0));
        }
        

        EndPlayerTurn(); 
    }

    void OnDefendCmd() 
    { 
        if (!isPlayerTurn) return; 

        if (PlayerFood >= 1) 
        {
            PlayerFood -= 1; 
            PlayerArmor += 5; // 正常防御
            LogToScreen("修筑工事 +5甲"); 
        }
        else
        {
            // 疲惫防御：不扣血，但加甲很少
            PlayerArmor += 2; 
            LogToScreen($"<color=red>疲惫防守 +2甲 (粮草不足)</color>");
        }

        EndPlayerTurn(); 
    }

    void OnSkipCmd() 
    { 
        if (!isPlayerTurn) return; 
        LogToScreen("按兵不动"); 
        EndPlayerTurn(); 
    }

    public void OnHandCardClicked(BattleCardUI cardUI)
    {
        if (!isPlayerTurn) return;

        // 验资
        if (PlayerFood < cardUI.Data.Cost_Food || PlayerArmor < cardUI.Data.Cost_Armor)
        {
            LogToScreen($"<color=red>资源不足！(需 粮{cardUI.Data.Cost_Food} 甲{cardUI.Data.Cost_Armor})</color>");
            return; 
        }

        // 选中逻辑
        if (currentSelectedCardUI == cardUI)
        {
            DeselectAll();
            return;
        }

        if (currentSelectedCardUI != null) currentSelectedCardUI.UpdateState(false);
        currentSelectedCardUI = cardUI;
        currentSelectedCardUI.UpdateState(true); 

        if (ConfirmPlayCardBtn != null)
        {
            ConfirmPlayCardBtn.interactable = true;
            UpdateBtnText("确认出牌");
        }
    }

    void OnConfirmPlayCardClicked()
    {
        if (currentSelectedCardUI == null) return;
        DataManager.CardData card = currentSelectedCardUI.Data;

        if (PlayerFood < card.Cost_Food || PlayerArmor < card.Cost_Armor) return;

        PlayerFood -= card.Cost_Food;
        PlayerArmor -= card.Cost_Armor;

        ApplyCardEffect(card);

        HandPile.Remove(card);
        DiscardPile.Add(card);
        Destroy(currentSelectedCardUI.gameObject);

        DeselectAll();
        UpdateUI();
        CheckVictoryCondition();
    }

    // =========================================================
    // 🔄 回合与结算
    // =========================================================

    void ApplyCardEffect(DataManager.CardData card)
    {
        if (card.Type == DataManager.CardType.Unit) {
            // 注意：现在 Unit 类型可能代表加信念/回血
            PlayerUnitCount += card.Power;
            LogToScreen($"信念恢复 +{card.Power}");
            // 🔥 飘字效果：绿色回血
            DamagePopup.SpawnPopup($"+{card.Power}", Camera.main.transform.position + Vector3.left * 2, Color.green);
            return;
        }
        switch (card.Effect_ID) {
            case "ADD_RES": 
                PlayerFood += card.Effect_Val; 
                LogToScreen($"获得粮草 +{card.Effect_Val}");
                break;
            case "ADD_ARMOR": 
                PlayerArmor += card.Effect_Val; 
                LogToScreen($"获得护甲 +{card.Effect_Val}");
                break;
            case "DRAW_SELF": 
                DrawCards(card.Effect_Val); 
                LogToScreen($"抽取 {card.Effect_Val} 张牌");
                break;
            case "DMG_ENEMY": 
                EnemyUnitCount -= card.Effect_Val;
                LogToScreen($"卡牌伤害！造成 {card.Effect_Val} 点伤害");
                // 🔥 飘字效果：红色伤害
                DamagePopup.SpawnPopup($"-{card.Effect_Val}", Camera.main.transform.position + Vector3.right * 2, Color.red);
                break;
            default: 
                EnemyUnitCount -= card.Effect_Val;
                LogToScreen($"造成 {card.Effect_Val} 点伤害");
                // 🔥 飘字效果：红色伤害
                DamagePopup.SpawnPopup($"-{card.Effect_Val}", Camera.main.transform.position + Vector3.right * 2, Color.red);
                break;
        }
    }

    void CheckVictoryCondition()
    {
        // 胜利
        if (EnemyUnitCount <= 0)
        {
            EnemyUnitCount = 0;
            LogToScreen("<color=green>🏆 胜利！</color>");
            StopAllCoroutines();

            // 结算资源：库存 + 剩余行动力 + 战利品
            int finalFood = stockFood + PlayerFood + VictoryLootFood;
            int finalArmor = stockArmor + PlayerArmor + VictoryLootArmor; // 假设护甲能带走一部分
            int finalBelief = PlayerUnitCount; // 继承剩余信念

            // 写回 ResourceManager (铁三角更新)
            if (ResourceManager.Instance != null) {
                ResourceManager.Instance.Grain = finalFood;
                ResourceManager.Instance.Armor = finalArmor;
                ResourceManager.Instance.Belief = finalBelief;
            }

            // 广播胜利消息
            string msg = $"大获全胜！\n信念:{finalBelief} 粮:{finalFood} 甲:{finalArmor}";
            OnBattleEnded?.Invoke(msg);
        }
        // 失败 (信念耗尽)
        else if (PlayerUnitCount <= 0)
        {
            StopAllCoroutines();
            if (ResourceManager.Instance != null)
            {
                // 确保归零以触发全局死亡事件
                ResourceManager.Instance.ChangeResource(101, -9999); 
            }
            // 备用：如果没有 RM，就自己喊结束
            else if (UIManager.Instance != null) 
            {
                UIManager.Instance.ShowEnding("信念崩塌，埋骨黄沙...");
            }
        }
    }

    IEnumerator StartTurnRoutine()
    {
        turnCount++;
        isPlayerTurn = true;
        CurrentPhase = BattlePhase.PlayerTurn;

        // ... (扣粮逻辑不变) ...

        LogToScreen($"第{turnCount}回合");

        // 🔥🔥🔥 核心修复：一定要在这里解锁按钮！ 🔥🔥🔥
        SetBasicButtonsActive(true); 

        // 刷新意图显示
        if (Text_Enemy_Intent != null)
        {
            // ... (意图计算逻辑不变) ...
        }

        DrawCards(1);
        DeselectAll(); // 注意：这个方法会锁住 ConfirmBtn，这是对的
        UpdateUI();
        yield return null;
    }

    IEnumerator EnemyTurnRoutine()
    {
        isPlayerTurn = false;
        DeselectAll();
        LogToScreen("敌方回合...");
        
        if (Text_Enemy_Intent != null) Text_Enemy_Intent.text = "⚔️ 敌军正在行动...";
        
        yield return new WaitForSeconds(1.0f);
        
        if(EnemyUnitCount > 0) {
            // 简单伤害公式：敌人战力 - 玩家当前护甲
            int baseAttack = Mathf.CeilToInt(EnemyUnitCount * 0.2f); 
            // 还要确保至少有 1 点基础攻击力（除非兵力为0）
            if (EnemyUnitCount > 0 && baseAttack < 1) baseAttack = 1;

            int dmg = Mathf.Max(0, baseAttack - PlayerArmor);
            
            if (dmg > 0) {
                PlayerUnitCount -= dmg;
                LogToScreen($"受到 {dmg} 点伤害！");
                // 🔥 飘字效果：橙色伤害（标记为受敌人伤害）
                DamagePopup.SpawnPopup($"-{dmg}", Camera.main.transform.position + Vector3.left * 2, new Color(1, 0.5f, 0));
            } else {
                LogToScreen("完美防御！");
                // 🔥 飘字效果：蓝色防御提示
                DamagePopup.SpawnPopup("BLOCK", Camera.main.transform.position + Vector3.left * 2, Color.cyan);
            }

            // 敌人回合结束，玩家护甲通常会衰减 (可选，这里暂时保留一半)
            PlayerArmor = PlayerArmor / 2; 
        }
        
        UpdateUI();
        CheckVictoryCondition();
        if(PlayerUnitCount > 0 && EnemyUnitCount > 0) StartCoroutine(StartTurnRoutine());
    }

    // =========================================================
    // 辅助方法
    // =========================================================

    void DeselectAll()
    {
        if (currentSelectedCardUI != null) currentSelectedCardUI.UpdateState(false);
        currentSelectedCardUI = null;
        if (ConfirmPlayCardBtn != null) {
            ConfirmPlayCardBtn.interactable = false;
            UpdateBtnText("请选牌");
        }
    }

    void EndPlayerTurn() 
    { 
        isPlayerTurn = false; 
        
        // 🔥 回合结束立刻锁住，防止连点
        SetBasicButtonsActive(false);

        UpdateUI(); 
        CheckVictoryCondition(); 
        if(EnemyUnitCount > 0) StartCoroutine(EnemyTurnRoutine()); 
    }
    
    void InitializeDeck() { DrawPile.Clear(); HandPile.Clear(); DiscardPile.Clear(); if (DataManager.Instance) DrawPile = DataManager.Instance.GetStarterDeck(); }
    void ShuffleDeck() { for (int i = 0; i < DrawPile.Count; i++) { var temp = DrawPile[i]; int r = Random.Range(i, DrawPile.Count); DrawPile[i] = DrawPile[r]; DrawPile[r] = temp; } }
    void ClearHandUI() { foreach (Transform t in HandAreaTransform) Destroy(t.gameObject); }
    void DrawCards(int c) {
        for (int i = 0; i < c; i++) {
            if (DrawPile.Count == 0 && DiscardPile.Count > 0) { DrawPile.AddRange(DiscardPile); DiscardPile.Clear(); ShuffleDeck(); }
            if (DrawPile.Count == 0) break;
            var card = DrawPile[0]; DrawPile.RemoveAt(0); HandPile.Add(card);
            var obj = Instantiate(CardPrefab, HandAreaTransform);
            if(obj.GetComponent<BattleCardUI>()) obj.GetComponent<BattleCardUI>().Setup(card);
        }
        UpdateUI();
    }
    
    void UpdateUI() {
        if(Text_Player_Food) Text_Player_Food.text = $"{PlayerFood}";
        if(Text_Player_Armor) Text_Player_Armor.text = $"{PlayerArmor}";
        if(Text_Player_Unit) Text_Player_Unit.text = $"{PlayerUnitCount}"; // 显示信念
        if(Text_Enemy_Unit) Text_Enemy_Unit.text = $"{EnemyUnitCount}";

        if (Text_Enemy_Intent != null && isPlayerTurn)
        {
            int baseAttack = Mathf.CeilToInt(EnemyUnitCount * 0.2f);
            if (EnemyUnitCount > 0 && baseAttack < 1) baseAttack = 1;

            int predictedDmg = Mathf.Max(0, baseAttack - PlayerArmor);
            Text_Enemy_Intent.text = $"⚠️ 敌军意图: 攻击\n预计伤害: {predictedDmg}";
        }
    }
    
    void UpdateBtnText(string t) { if(ConfirmPlayCardBtn) { var txt = ConfirmPlayCardBtn.GetComponentInChildren<TMP_Text>(); if(txt) txt.text = t; } }
    private string fullLog = "";

    void LogToScreen(string m) 
    { 
        Debug.Log(m); 
        
        // 加上换行符
        fullLog += m + "\n"; 
        
        // 可选：只保留最后 5 行（防止文本太长爆内存）
        // 这里用简单粗暴的方法：如果太长就清空一半，或者用 Queue<string> 管理
        // 简单版：
        if (fullLog.Length > 1000) fullLog = fullLog.Substring(fullLog.Length - 500);

        if (BattleLogText) 
        {
            BattleLogText.text = fullLog;
            // 如果你的 Text 在 ScrollView 里，这里可以加代码自动滚动到底部
        }
    }
    void SetBasicButtonsActive(bool isActive)
    {
        if (AttackBtn) AttackBtn.interactable = isActive;
        if (DefendBtn) DefendBtn.interactable = isActive;
        if (SkipBtn) SkipBtn.interactable = isActive;
    }
}