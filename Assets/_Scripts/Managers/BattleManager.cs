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
    // 👉 UI 引用 (必须在 Inspector 手动拖拽)
    // ==============================
    [Header("--- UI 引用 (请手动拖拽) ---")]
    public TMP_Text Text_Player_Food;
    public TMP_Text Text_Player_Armor;
    public TMP_Text Text_Player_Unit;
    public TMP_Text Text_Enemy_Unit;
    public TMP_Text BattleLogText;

    public Button AttackBtn;
    public Button DefendBtn;
    public Button SkipBtn;
    
    // 🔥 报错点2修复：必须有这个按钮变量
    public Button ConfirmPlayCardBtn; 

    [Header("--- 容器 (请手动拖拽) ---")]
    public Transform HandAreaTransform;
    public GameObject CardPrefab; 

    [Header("--- 战斗数据 (自动显示) ---")]
    public int PlayerUnitCount;
    public int EnemyUnitCount;
    public int PlayerFood, PlayerArmor;
    public int EnemyFood = 5; 
    public int EnemyArmor = 2;

    private int stockFood, stockArmor;
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

        // 如果 UI 管理器存在，先隐藏战斗面板，防止穿帮
        if (UIManager.Instance != null && UIManager.Instance.BattlePanel != null)
            UIManager.Instance.BattlePanel.SetActive(false);
    }

    void Start()
    {
        // 🛡️ 报错点2,3,4,5 修复：绑定按钮事件
        if (ConfirmPlayCardBtn != null)
        {
            ConfirmPlayCardBtn.onClick.RemoveAllListeners();
            ConfirmPlayCardBtn.onClick.AddListener(OnConfirmPlayCardClicked); // 👈 这里需要下面的定义
            ConfirmPlayCardBtn.interactable = false;
            UpdateBtnText("请选牌");
        }

        if (AttackBtn) AttackBtn.onClick.AddListener(OnAttackCmd);
        if (DefendBtn) DefendBtn.onClick.AddListener(OnDefendCmd);
        if (SkipBtn) SkipBtn.onClick.AddListener(OnSkipCmd);
    }

    // =========================================================
    // 🔥 报错点6 修复：UIManager 调用的入口
    // =========================================================
    public void StartBattle(DataManager.EnemyData enemyData)
    {
        // 1. 确保 UI 切换到战斗状态
        if (UIManager.Instance) UIManager.Instance.SwitchState(UIManager.UIState.Battle);

        // 2. 读取库存
        if (GameManager.Instance != null) { 
            stockFood = GameManager.Instance.GlobalFoodStock; 
            stockArmor = GameManager.Instance.GlobalArmorStock; 
        } else { 
            stockFood = 10; stockArmor = 5; 
        }

        // 3. 初始化数值
        PlayerFood = 0; PlayerArmor = 0; PlayerUnitCount = DefaultUnitCount;
        EnemyFood = 5; EnemyArmor = 2;

        if (enemyData != null) {
            EnemyUnitCount = enemyData.Power;
            LogToScreen($"遭遇：{enemyData.Name} (兵力{EnemyUnitCount})");
        } else {
            EnemyUnitCount = DefaultUnitCount;
            LogToScreen("遭遇伏兵！");
        }

        // 4. 洗牌发牌
        InitializeDeck(); 
        ShuffleDeck();
        ClearHandUI();
        DrawCards(4);

        turnCount = 0;
        StartCoroutine(StartTurnRoutine());
    }

    // =========================================================
    // 🔥 报错点1 修复：BattleCardUI 调用的方法
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

    // =========================================================
    // 🔥 报错点2 修复：确认出牌逻辑
    // =========================================================
    void OnConfirmPlayCardClicked()
    {
        if (currentSelectedCardUI == null) return;

        DataManager.CardData card = currentSelectedCardUI.Data;

        // 1. 资源检查
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

    // =========================================================
    // 🔥 报错点3,4,5 修复：基础指令
    // =========================================================
    void OnAttackCmd() 
    { 
        if (!isPlayerTurn || PlayerFood < 1) return; 
        PlayerFood -= 1; 
        EnemyUnitCount -= PlayerUnitCount; // 简单伤害计算
        LogToScreen("全军突击！"); 
        EndPlayerTurn(); 
    }

    void OnDefendCmd() 
    { 
        if (!isPlayerTurn || PlayerFood < 1) return; 
        PlayerFood -= 1; 
        PlayerArmor += 2; 
        LogToScreen("修筑工事 +2甲"); 
        EndPlayerTurn(); 
    }

    void OnSkipCmd() 
    { 
        if (!isPlayerTurn) return; 
        LogToScreen("按兵不动"); 
        EndPlayerTurn(); 
    }

    // =========================================================
    // 内部逻辑 (保持不变)
    // =========================================================

    void ApplyCardEffect(DataManager.CardData card)
    {
        // ... (卡牌效果解析逻辑) ...
        if (card.Type == DataManager.CardType.Unit) {
            PlayerUnitCount += card.Power;
            LogToScreen($"💂 增援 +{card.Power}");
            return;
        }
        switch (card.Effect_ID) {
            case "ADD_RES": PlayerFood += card.Effect_Val; break;
            case "ADD_ARMOR": PlayerArmor += card.Effect_Val; break;
            case "DRAW_SELF": DrawCards(card.Effect_Val); break;
            case "DMG_ENEMY": EnemyUnitCount -= card.Effect_Val; break;
            // ... 其他 case 可以按需补充
            default: EnemyUnitCount -= card.Effect_Val; break; // 保底
        }
    }

    void CheckVictoryCondition()
    {
        if (EnemyUnitCount <= 0)
        {
            EnemyUnitCount = 0;
            LogToScreen("<color=green>🏆 胜利！</color>");
            StopAllCoroutines();

            int finalFood = stockFood + PlayerFood + VictoryLootFood;
            int finalArmor = stockArmor + PlayerArmor + VictoryLootArmor;

            if (GameManager.Instance != null) {
                GameManager.Instance.GlobalFoodStock = finalFood;
                GameManager.Instance.GlobalArmorStock = finalArmor;
            }

            if (UIManager.Instance != null) {
                string msg = $"大获全胜！\n回收: 粮{finalFood} 甲{finalArmor}";
                UIManager.Instance.ShowResult(msg);
            }
        }
        else if (PlayerUnitCount < 0)
        {
            StopAllCoroutines();
            if (UIManager.Instance != null) UIManager.Instance.ShowEnding("兵败身死...");
        }
    }

    IEnumerator StartTurnRoutine()
    {
        turnCount++;
        isPlayerTurn = true;
        
        // 简单的粮道模拟
        if (stockFood >= 1) { stockFood--; PlayerFood++; }
        if (stockArmor >= 1) { stockArmor--; PlayerArmor++; }
        
        LogToScreen($"第{turnCount}回合");
        DrawCards(1);
        DeselectAll();
        UpdateUI();
        yield return null;
    }

    IEnumerator EnemyTurnRoutine()
    {
        isPlayerTurn = false;
        DeselectAll();
        LogToScreen("敌方回合...");
        yield return new WaitForSeconds(1.0f);
        
        if(EnemyUnitCount > 0) {
            int dmg = Mathf.Max(1, EnemyUnitCount - PlayerArmor);
            PlayerUnitCount -= dmg;
            LogToScreen($"敌军造成 {dmg} 伤害");
        }
        
        UpdateUI();
        CheckVictoryCondition();
        if(PlayerUnitCount >= 0 && EnemyUnitCount > 0) StartCoroutine(StartTurnRoutine());
    }

    void DeselectAll()
    {
        if (currentSelectedCardUI != null) currentSelectedCardUI.UpdateState(false);
        currentSelectedCardUI = null;
        if (ConfirmPlayCardBtn != null) {
            ConfirmPlayCardBtn.interactable = false;
            UpdateBtnText("请选牌");
        }
    }

    void EndPlayerTurn() { isPlayerTurn = false; UpdateUI(); CheckVictoryCondition(); if(EnemyUnitCount > 0) StartCoroutine(EnemyTurnRoutine()); }
    
    // 辅助方法
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
        if(Text_Player_Unit) Text_Player_Unit.text = $"{PlayerUnitCount}";
        if(Text_Enemy_Unit) Text_Enemy_Unit.text = $"{EnemyUnitCount}";
    }
    void UpdateBtnText(string t) { if(ConfirmPlayCardBtn) { var txt = ConfirmPlayCardBtn.GetComponentInChildren<TMP_Text>(); if(txt) txt.text = t; } }
    void LogToScreen(string m) { Debug.Log(m); if (BattleLogText) BattleLogText.text = m; }
}