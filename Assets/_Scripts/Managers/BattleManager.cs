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

    [Header("--- UI 引用 (自动绑定) ---")]
    public TMP_Text Text_Player_Food;
    public TMP_Text Text_Player_Armor;
    public TMP_Text Text_Player_Unit;
    public TMP_Text Text_Enemy_Unit;
    public TMP_Text BattleLogText;

    public Button AttackBtn;
    public Button DefendBtn;
    public Button SkipBtn;
    public Button ConfirmPlayCardBtn; // 对应 EndTurn_Btn

    [Header("--- 容器 ---")]
    public Transform HandAreaTransform;
    public GameObject CardPrefab;

    [Header("--- 战斗数据 ---")]
    public int PlayerUnitCount;
    public int EnemyUnitCount;
    public int PlayerFood, PlayerArmor;
    
    // 🔥 新增：敌人资源 (为了支持 STEAL_RES 效果)
    public int EnemyFood = 5; 
    public int EnemyArmor = 2;

    private int stockFood, stockArmor; // 全局库存缓存

    // 牌堆列表
    public List<DataManager.CardData> DrawPile = new List<DataManager.CardData>();
    public List<DataManager.CardData> HandPile = new List<DataManager.CardData>();
    public List<DataManager.CardData> DiscardPile = new List<DataManager.CardData>();

    private BattleCardUI currentSelectedCardUI;
    private bool isPlayerTurn;
    private int turnCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        AutoBindBattleUI();

        // 防止游戏刚开始时面板闪烁，先关掉，等 StartBattle 再打开
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
    // 🖱️ 交互逻辑：点击卡牌 -> 选中 -> 点击确认 -> 出牌
    // =========================================================

    public void OnHandCardClicked(BattleCardUI cardUI)
    {
        if (!isPlayerTurn) return;

        // 如果点击已选中的 -> 取消选中
        if (currentSelectedCardUI == cardUI)
        {
            DeselectAll();
            return;
        }

        // 1. 重置旧卡状态
        if (currentSelectedCardUI != null) currentSelectedCardUI.UpdateState(false);

        // 2. 选中新卡
        currentSelectedCardUI = cardUI;
        currentSelectedCardUI.UpdateState(true); // 变黄

        // 3. 激活按钮
        if (ConfirmPlayCardBtn != null)
        {
            ConfirmPlayCardBtn.interactable = true;
            UpdateBtnText("确认出牌");
        }
    }

    public void OnConfirmPlayCardClicked()
    {
        if (currentSelectedCardUI == null) return;

        DataManager.CardData card = currentSelectedCardUI.Data;

        // 1. 资源检查 (糧/甲)
        if (PlayerFood < card.Cost_Food || PlayerArmor < card.Cost_Armor)
        {
            LogToScreen($"<color=red>资源不足！需 粮{card.Cost_Food} / 甲{card.Cost_Armor}</color>");
            return;
        }

        // 2. 扣除消耗
        PlayerFood -= card.Cost_Food;
        PlayerArmor -= card.Cost_Armor;

        // 3. 执行效果
        ApplyCardEffect(card);

        // 4. 移出逻辑
        HandPile.Remove(card);
        DiscardPile.Add(card);
        Destroy(currentSelectedCardUI.gameObject);

        // 5. 收尾
        DeselectAll();
        UpdateUI();
        CheckVictoryCondition();
    }

    void DeselectAll()
    {
        if (currentSelectedCardUI != null) currentSelectedCardUI.UpdateState(false);
        currentSelectedCardUI = null;

        if (ConfirmPlayCardBtn != null)
        {
            ConfirmPlayCardBtn.interactable = false;
            UpdateBtnText("请选牌");
        }
    }

    // =========================================================
    // 🔥 核心逻辑：卡牌效果解析
    // =========================================================
    void ApplyCardEffect(DataManager.CardData card)
    {
        LogToScreen($"使用了 [{card.Name}]");

        // 1. 单位牌 (Unit)
        if (card.Type == DataManager.CardType.Unit)
        {
            PlayerUnitCount += card.Power;
            LogToScreen($"💂 增援抵达！兵力 +{card.Power}");
            return;
        }

        // 2. 策略牌 (Strategy) - 解析 Effect_ID
        switch (card.Effect_ID)
        {
            case "ADD_RES":
            case "ADD_FOOD":
                PlayerFood += card.Effect_Val;
                LogToScreen($"🌾 屯田收获 +{card.Effect_Val}");
                break;
            case "ADD_ARMOR":
                PlayerArmor += card.Effect_Val;
                LogToScreen($"🛡️ 修补护甲 +{card.Effect_Val}");
                break;
            case "DRAW_SELF":
            case "DRAW_CARD":
                DrawCards(card.Effect_Val);
                LogToScreen($"🃏 抽卡 +{card.Effect_Val}");
                break;
            case "STEAL_RES":
                int f = Mathf.Min(EnemyFood, card.Effect_Val); // 不能抢成负数
                EnemyFood -= f; 
                PlayerFood += f;
                LogToScreen($"✋ 劫粮成功 +{f}");
                break;
            case "DMG_ENEMY": 
                EnemyUnitCount -= card.Effect_Val;
                LogToScreen($"💥 法术伤害 {card.Effect_Val}");
                break;
            case "AOE_EARTHQUAKE":
                int pDmg = (int)(PlayerUnitCount * 0.3f);
                int eDmg = (int)(EnemyUnitCount * 0.3f);
                PlayerUnitCount -= pDmg; 
                EnemyUnitCount -= eDmg;
                LogToScreen($"🌋 地震！敌损{eDmg} 我损{pDmg}");
                break;
            default:
                // 保底逻辑：如果ID写错，默认当做直接伤害
                if(card.Effect_Val > 0) {
                     EnemyUnitCount -= card.Effect_Val;
                     LogToScreen($"⚔️ 攻击造成 {card.Effect_Val} 伤害");
                }
                break;
        }
    }

    // =========================================================
    // ⚔️ 战斗流程控制
    // =========================================================

    public void StartBattle(DataManager.EnemyData enemyData)
    {
        // 确保 UI 处于战斗状态
        if (UIManager.Instance) UIManager.Instance.SwitchState(UIManager.UIState.Battle);

        // 读取库存
        if (GameManager.Instance != null) { 
            stockFood = GameManager.Instance.GlobalFoodStock; 
            stockArmor = GameManager.Instance.GlobalArmorStock; 
        } else { 
            stockFood = 10; stockArmor = 5; 
        }

        // 初始化数值
        PlayerFood = 0; PlayerArmor = 0; PlayerUnitCount = DefaultUnitCount;
        EnemyFood = 5; EnemyArmor = 2; // 默认敌人资源

        if (enemyData != null) {
            EnemyUnitCount = enemyData.Power;
            LogToScreen($"遭遇：{enemyData.Name} (兵力{EnemyUnitCount})");
        } else {
            EnemyUnitCount = DefaultUnitCount;
            LogToScreen("遭遇伏兵！");
        }

        // 洗牌发牌
        InitializeDeck(); 
        ShuffleDeck();
        ClearHandUI();
        DrawCards(4);

        turnCount = 0;
        StartCoroutine(StartTurnRoutine());
    }

    IEnumerator StartTurnRoutine()
    {
        turnCount++;
        isPlayerTurn = true;

        // 粮道模拟 (每回合从库存运送物资)
        string supplyLog = "";
        if (stockFood >= 1) { stockFood -= 1; PlayerFood += 1; } 
        else supplyLog += "断粮! ";
        
        if (stockArmor >= 1) { stockArmor -= 1; PlayerArmor += 1; }

        LogToScreen($"第{turnCount}回合。{supplyLog}");
        
        DrawCards(1);
        DeselectAll(); 
        UpdateUI();
        yield return null;
    }

    void CheckVictoryCondition()
    {
        // 1. 胜利
        if (EnemyUnitCount <= 0)
        {
            EnemyUnitCount = 0;
            LogToScreen("<color=green>🏆 战斗胜利！</color>");
            StopAllCoroutines();

            // 计算最终资源
            int finalFood = stockFood + PlayerFood + VictoryLootFood;
            int finalArmor = stockArmor + PlayerArmor + VictoryLootArmor;

            // 回写到 GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GlobalFoodStock = finalFood;
                GameManager.Instance.GlobalArmorStock = finalArmor;
            }

            // 弹窗显示结果
            if (UIManager.Instance != null)
            {
                string msg = $"大获全胜！\n\n剩余库存: {stockFood}\n战场回收: 粮{PlayerFood} 甲{PlayerArmor}\n战利品: 粮+{VictoryLootFood} 甲+{VictoryLootArmor}\n\n总计: 粮{finalFood} 甲{finalArmor}";
                UIManager.Instance.ShowResult(msg);
                // UIManager.Instance.BattlePanel.SetActive(false); // 交给UIManager处理状态切换
            }
        }
        // 2. 失败
        else if (PlayerUnitCount < 0)
        {
            PlayerUnitCount = 0;
            StopAllCoroutines();
            if (UIManager.Instance != null) UIManager.Instance.ShowEnding("兵败身死，黄沙埋骨...");
        }
    }

    // --- 敌方回合 (简化版AI) ---
    IEnumerator EnemyTurnRoutine()
    {
        isPlayerTurn = false;
        DeselectAll();
        LogToScreen(">>> 敌方回合 <<<");
        yield return new WaitForSeconds(1.0f);

        if (EnemyUnitCount > 0)
        {
            // 简单伤害公式：(敌兵 - 我甲)
            int dmg = Mathf.Max(1, EnemyUnitCount - PlayerArmor);
            PlayerUnitCount -= dmg;
            LogToScreen($"敌军造成 {dmg} 点伤害！");
        }

        UpdateUI();
        CheckVictoryCondition();

        // 如果双方都活着，进入下一回合
        if (PlayerUnitCount >= 0 && EnemyUnitCount > 0)
            StartCoroutine(StartTurnRoutine());
    }

    // =========================================================
    // 🛠️ 辅助与自动绑定
    // =========================================================

    [ContextMenu("执行战斗UI绑定")]
    public void AutoBindBattleUI()
    {
        Transform canvasTr = GameObject.Find("Canvas")?.transform;
        if (!canvasTr) return;

        Text_Player_Food = FindText(canvasTr, "Text_Player_Food");
        Text_Player_Armor = FindText(canvasTr, "Text_Player_Armor");
        Text_Player_Unit = FindText(canvasTr, "Text_Player_Unit");
        Text_Enemy_Unit = FindText(canvasTr, "Text_Unit_Count"); // 敌人兵力
        BattleLogText = FindText(canvasTr, "Battle_Log_Text");

        AttackBtn = FindButton(canvasTr, "Btn_Attack");
        DefendBtn = FindButton(canvasTr, "Btn_Defend");
        SkipBtn = FindButton(canvasTr, "Btn_Skip");
        ConfirmPlayCardBtn = FindButton(canvasTr, "EndTurn_Btn"); // 绑定结束按钮作为确认按钮

        Transform ha = FindChild(canvasTr, "Hand_Card_Area");
        if (ha) HandAreaTransform = ha;
    }

    void UpdateUI()
    {
        if (Text_Player_Food) Text_Player_Food.text = $"{PlayerFood}";
        if (Text_Player_Armor) Text_Player_Armor.text = $"{PlayerArmor}";
        if (Text_Player_Unit) Text_Player_Unit.text = $"{PlayerUnitCount}";
        if (Text_Enemy_Unit) Text_Enemy_Unit.text = $"{EnemyUnitCount}";
    }

    void UpdateBtnText(string txt)
    {
        if (ConfirmPlayCardBtn) {
            var t = ConfirmPlayCardBtn.GetComponentInChildren<TMP_Text>();
            if (t) t.text = txt;
        }
    }

    void InitializeDeck() 
    { 
        DrawPile.Clear(); HandPile.Clear(); DiscardPile.Clear(); 
        if (DataManager.Instance) DrawPile = DataManager.Instance.GetStarterDeck(); 
    }
    
    void ShuffleDeck() 
    { 
        for (int i = 0; i < DrawPile.Count; i++) { 
            var temp = DrawPile[i]; int r = Random.Range(i, DrawPile.Count); 
            DrawPile[i] = DrawPile[r]; DrawPile[r] = temp; 
        } 
    }
    
    void ClearHandUI() { foreach (Transform t in HandAreaTransform) Destroy(t.gameObject); }
    
    void DrawCards(int c)
    {
        for (int i = 0; i < c; i++)
        {
            if (DrawPile.Count == 0) {
                 if(DiscardPile.Count > 0) {
                     DrawPile.AddRange(DiscardPile); DiscardPile.Clear(); ShuffleDeck();
                 } else break;
            }
            var card = DrawPile[0]; DrawPile.RemoveAt(0); HandPile.Add(card);
            var obj = Instantiate(CardPrefab, HandAreaTransform);
            var ui = obj.GetComponent<BattleCardUI>();
            if (ui) ui.Setup(card);
        }
        UpdateUI();
    }

    void LogToScreen(string m) { Debug.Log(m); if (BattleLogText) BattleLogText.text = m; }
    
    // 基础指令 (如果没有牌可打)
    public void OnAttackCmd() { 
        if (!isPlayerTurn || PlayerFood < 1) return; 
        PlayerFood -= 1; EnemyUnitCount -= PlayerUnitCount; 
        LogToScreen("全军突击！"); EndPlayerTurn(); 
    }
    public void OnDefendCmd() { 
        if (!isPlayerTurn || PlayerFood < 1) return; 
        PlayerFood -= 1; PlayerArmor += 2; 
        LogToScreen("修筑工事 +2甲"); EndPlayerTurn(); 
    }
    public void OnSkipCmd() { if (!isPlayerTurn) return; LogToScreen("按兵不动"); EndPlayerTurn(); }
    
    void EndPlayerTurn() { 
        isPlayerTurn = false; UpdateUI(); CheckVictoryCondition(); 
        if(EnemyUnitCount > 0) StartCoroutine(EnemyTurnRoutine()); 
    }

    private Transform FindChild(Transform p, string n) { if (p.name == n) return p; foreach (Transform c in p) { var r = FindChild(c, n); if (r) return r; } return null; }
    private Button FindButton(Transform r, string n) { var t = FindChild(r, n); return t ? t.GetComponent<Button>() : null; }
    private TMP_Text FindText(Transform r, string n) { var t = FindChild(r, n); return t ? t.GetComponent<TMP_Text>() : null; }
}