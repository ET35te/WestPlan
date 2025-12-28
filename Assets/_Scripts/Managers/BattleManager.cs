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

    public void StartBattle(DataManager.EnemyData enemyData)
    {
        if (UIManager.Instance) UIManager.Instance.SwitchState(UIManager.UIState.Battle);

        // 1. 读取全局资源 (适配铁三角系统)
        if (ResourceManager.Instance != null) {
            stockFood = ResourceManager.Instance.Grain;
            stockArmor = ResourceManager.Instance.Armor;
            PlayerUnitCount = ResourceManager.Instance.Belief; // 信念即血量
        }
        else {
            // 保底逻辑
            stockFood = 10; stockArmor = 5; PlayerUnitCount = 100;
        }

        // 2. 初始化战斗内数值
        PlayerFood = 0; 
        PlayerArmor = 0; 
        // 注意：PlayerUnitCount 已经在上面读取了全局信念，不要重置为 DefaultUnitCount

        if (enemyData != null) {
            EnemyUnitCount = enemyData.Power;
            LogToScreen($"遭遇：{enemyData.Name} (战力{EnemyUnitCount})");
        } else {
            EnemyUnitCount = 10;
            LogToScreen("遭遇伏兵！");
        }

        // 3. 准备牌堆
        InitializeDeck(); 
        ShuffleDeck();
        ClearHandUI();
        DrawCards(4);

        turnCount = 0;
        StartCoroutine(StartTurnRoutine());
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
        }
        else
        {
            // 绝境反击：扣血攻击
            int hpCost = Mathf.Max(1, Mathf.FloorToInt(PlayerUnitCount * 0.1f)); // 扣10%信念
            PlayerUnitCount -= hpCost;
            
            int weakDamage = 2; // 虚弱伤害
            EnemyUnitCount -= weakDamage;

            LogToScreen($"<color=red>断粮强攻！信念-{hpCost}，造成 {weakDamage} 点伤害</color>");
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
            return;
        }
        switch (card.Effect_ID) {
            case "ADD_RES": PlayerFood += card.Effect_Val; break;
            case "ADD_ARMOR": PlayerArmor += card.Effect_Val; break;
            case "DRAW_SELF": DrawCards(card.Effect_Val); break;
            case "DMG_ENEMY": EnemyUnitCount -= card.Effect_Val; break;
            default: EnemyUnitCount -= card.Effect_Val; break;
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
        
        // 补给逻辑：每回合从库存拿1粮1甲进场
        if (stockFood >= 1) { stockFood--; PlayerFood++; }
        if (stockArmor >= 1) { stockArmor--; PlayerArmor++; }
        
        LogToScreen($"第{turnCount}回合");
        
        // 🔥 计算并显示敌人意图
        if (Text_Enemy_Intent != null)
        {
            // 预告：如果不防御，会受多少伤
            int predictedDmg = Mathf.Max(0, EnemyUnitCount - PlayerArmor);
            Text_Enemy_Intent.text = $"⚠️ 敌军意图: 攻击\n预计伤害: {predictedDmg}";
        }

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
        
        if (Text_Enemy_Intent != null) Text_Enemy_Intent.text = "⚔️ 敌军正在行动...";
        
        yield return new WaitForSeconds(1.0f);
        
        if(EnemyUnitCount > 0) {
            // 简单伤害公式：敌人战力 - 玩家当前护甲
            int dmg = Mathf.Max(0, EnemyUnitCount - PlayerArmor);
            
            if (dmg > 0) {
                PlayerUnitCount -= dmg;
                LogToScreen($"受到 {dmg} 点伤害！");
            } else {
                LogToScreen("完美防御！");
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

    void EndPlayerTurn() { isPlayerTurn = false; UpdateUI(); CheckVictoryCondition(); if(EnemyUnitCount > 0) StartCoroutine(EnemyTurnRoutine()); }
    
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

        // 🔥 实时刷新意图 (比如玩家出了加甲牌，意图文字也要变)
        if (Text_Enemy_Intent != null && isPlayerTurn)
        {
            int predictedDmg = Mathf.Max(0, EnemyUnitCount - PlayerArmor);
            Text_Enemy_Intent.text = $"⚠️ 敌军意图: 攻击\n预计伤害: {predictedDmg}";
        }
    }
    
    void UpdateBtnText(string t) { if(ConfirmPlayCardBtn) { var txt = ConfirmPlayCardBtn.GetComponentInChildren<TMP_Text>(); if(txt) txt.text = t; } }
    void LogToScreen(string m) { Debug.Log(m); if (BattleLogText) BattleLogText.text = m; }
}