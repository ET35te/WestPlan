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
        BattleLogText.text = ">>> 新回合开始，摸牌...";
        DrawCards(2); // 摸2张
        RefreshHandUI();
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
        BattleLogText.text = $"选择了：{card.Name}，请点击战线部署。";
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
            
            // A. 基础攻击力 = (兵力总和)^2
            int rawPower = lane.GetTotalPower();
            int finalDamage = rawPower * rawPower;

            // B. 资源修正
            if (lane.HasGrain) 
            {
                // 有粮：正常伤害 (或者加成，看你规则)
                ResourceManager.Instance.ChangeResource(102, -10); // 扣全局粮草
            }
            else 
            {
                // 无粮：伤害减半
                finalDamage /= 2; 
            }

            // C. 护盾逻辑 (简化：护盾抵消反伤，这里暂只算进攻)
            
            if (finalDamage > 0)
            {
                BattleLogText.text = $"{lane.LaneName} 发起攻击！造成 {finalDamage} 点伤害！";
                // 模拟攻击敌方对应路 (这里简化为直接打敌方中军)
                // 规则里：侧军打侧军，前军打前军。这里简单化：全部伤害汇总打Boss
                totalDamageToEnemy += finalDamage;
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