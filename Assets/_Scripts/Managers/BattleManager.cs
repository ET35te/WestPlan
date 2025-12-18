using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("UI 引用")]
    public GameObject BattlePanel;
    public Transform HandContainer;     // 手牌区父物体
    public GameObject CardPrefab;       // 手牌预制体
    public Button EndTurnBtn;           // 结束回合/攻击按钮
    public TMPro.TMP_Text BattleLogText;// 战报显示
    public TMPro.TMP_Text EnemyInfoText;// 敌方信息

    [Header("战线引用 (0:前军, 1:中军, 2:侧军)")]
    public BattleLaneUI[] Lanes;        // 必须拖入3个Lane

    // --- 运行时数据 ---
    private List<DataManager.CardData> drawPile = new List<DataManager.CardData>(); // 抽牌堆
    private List<DataManager.CardData> handPile = new List<DataManager.CardData>(); // 手牌
    private List<DataManager.CardData> discardPile = new List<DataManager.CardData>(); // 弃牌堆

    private DataManager.CardData selectedHandCard; // 当前选中的手牌
    
    private int enemyCenterHP = 5; // 敌方中军生命
    private int playerCenterHP = 5; // 我方中军生命 (简化版)

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    void Start()
    {
        EndTurnBtn.onClick.AddListener(OnEndTurnClicked);
        BattlePanel.SetActive(false);
    }

    // --- 1. 战斗初始化 ---
    public void StartBattle(DataManager.EventData evt)
    {
        BattlePanel.SetActive(true);
        enemyCenterHP = 5; // 重置Boss血量
        
        // 解析敌人信息 (仅作显示)
        string enemyName = "未知敌军";
        if (!string.IsNullOrEmpty(evt.OptA_Res1_Data) && evt.OptA_Res1_Data.StartsWith("ENEMY:"))
        {
            int eid = int.Parse(evt.OptA_Res1_Data.Split(':')[1]);
            var enemy = DataManager.Instance.GetEnemyByID(eid);
            if(enemy != null) enemyName = enemy.Name;
        }
        EnemyInfoText.text = $"{enemyName} (中军生命: {enemyCenterHP})";

        // 初始化卡组 (洗牌)
        drawPile = new List<DataManager.CardData>(DataManager.Instance.GetStarterDeck());
        Shuffle(drawPile);
        handPile.Clear();
        discardPile.Clear();

        // 初始化战线
        foreach(var lane in Lanes) lane.ResetLane();

        StartTurn();
    }

    // --- 2. 回合开始 (摸牌) ---
        void StartTurn()
    {
        GenerateEnemyMoves(); // 1. 先生成敌人意图
        
        BattleLogText.text = ">>> 新回合：敌军意图已暴露！请部署卡牌。";
        DrawCards(2); 
        RefreshHandUI();
    }
        void GenerateEnemyMoves()
    {
        foreach (var lane in Lanes)
        {
            // 简单 AI：随机生成 1~3 点战力
            int power = UnityEngine.Random.Range(1, 4);
            // 50% 概率攻击，50% 概率防守
            bool isAttack = UnityEngine.Random.value > 0.5f;
            
            lane.SetEnemyIntent(power, isAttack);
        }
    }
    void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                // 洗牌逻辑：弃牌堆回抽牌堆
                if (discardPile.Count == 0) break; // 没牌了
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
            }
            handPile.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }
    }

    // --- 3. 玩家操作 (选牌 -> 选路) ---
    // 由 BattleCardUI 调用
    public void OnHandCardClicked(DataManager.CardData card)
    {
        selectedHandCard = card;
        BattleLogText.text = $"选择了：{card.Name}";

        // 遍历所有手牌UI，更新高亮状态
        foreach (Transform child in HandContainer)
        {
            var ui = child.GetComponent<BattleCardUI>();
            // 判断这个UI代表的卡是不是当前选中的卡
            // 注意：这里需要 BattleCardUI 公开它的 myData，或者在 Setup 里存一下 ID 对比
            // 简单做法：BattleCardUI 增加一个 public DataManager.CardData Data { get; private set; }
            
            if (ui.Data == card) ui.SetSelected(true);
            else ui.SetSelected(false);
        }
    }
    // 由 BattleLaneUI 调用
    public void OnLaneClicked(int laneIndex)
    {
        if (selectedHandCard == null) return;

        // 部署卡牌到该路
        bool success = Lanes[laneIndex].AddCard(selectedHandCard);
        
        if (success)
        {
            // 从手牌移除
            handPile.Remove(selectedHandCard);
            selectedHandCard = null;
            RefreshHandUI(); // 刷新手牌显示
        }
    }

    // --- 4. 战斗结算 (核心公式) ---
    void OnEndTurnClicked()
    {
        StartCoroutine(ResolveBattleRoutine());
    }

    IEnumerator ResolveBattleRoutine()
    {
        BattleLogText.text = ">>> 开始战斗结算...";
        yield return new WaitForSeconds(0.5f);

        int totalDamageToEnemy = 0;

        // 依次结算 3 路 (0:前, 1:中, 2:侧)
        for (int i = 0; i < 3; i++)
        {
             var lane = Lanes[i];
    int myPower = lane.GetTotalPower(); // 我方总战力
    int enemyPower = lane.EnemyPower;   // 敌方总战力 (新增)
    
    // 简单的抵消逻辑
    //int damage = 0;
    
    if (lane.IsEnemyAttacking)
    {
        // 敌方进攻 vs 我方 (可能是攻也可能是守)
        // 假设我方战力可以抵消敌方战力
        int netDamage = enemyPower - myPower;
        if (netDamage > 0)
        {
            BattleLogText.text = $"{lane.LaneName}: 防守失败！受到 {netDamage} 伤害";
            // 扣我方资源/血量 (这里暂扣兵力)
            ResourceManager.Instance.ChangeResource(104, -netDamage);
        }
        else
        {
            BattleLogText.text = $"{lane.LaneName}: 成功防御！";
        }
    }
    else // 敌方防守
    {
        // 我方进攻 vs 敌方防守
        int netDamage = myPower - enemyPower;
        if (netDamage > 0)
        {
            // 伤害公式：(净胜战力)^2
            int finalDmg = netDamage * netDamage;
            BattleLogText.text = $"{lane.LaneName}: 突破防线！对敌造成 {finalDmg} 伤害";
            totalDamageToEnemy += finalDmg;
        }
        else
        {
            BattleLogText.text = $"{lane.LaneName}: 攻击被阻挡。";
        }
    }
            
            // 结算后清空该路卡牌 -> 进弃牌堆
            discardPile.AddRange(lane.ClearLane());
            
            yield return new WaitForSeconds(1f);
        }

        // 判定胜负
        enemyCenterHP -= totalDamageToEnemy;
        EnemyInfoText.text = $"敌军中军生命: {enemyCenterHP}";

        if (enemyCenterHP <= 0)
        {
            EndBattle(true);
        }
        else
        {
            // 敌方反击 (简单扣兵力)
            ResourceManager.Instance.ChangeResource(104, -5);
            BattleLogText.text = "敌方反击！我军兵力 -5";
            yield return new WaitForSeconds(1f);
            
            // 下一回合
            StartTurn();
        }
    }

    void EndBattle(bool isWin)
    {
        BattlePanel.SetActive(false);
        string res = isWin ? "【大捷】敌方中军溃败！" : "【撤退】";
        if(isWin) ResourceManager.Instance.ChangeResource(105, 50);
        UIManager.Instance.ShowResult(res);
    }

    // --- 辅助 UI 刷新 ---
    void RefreshHandUI()
    {
        foreach (Transform child in HandContainer) Destroy(child.gameObject);
        foreach (var card in handPile)
        {
            GameObject go = Instantiate(CardPrefab, HandContainer);
            go.GetComponent<BattleCardUI>().Setup(card);
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}