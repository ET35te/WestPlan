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
        // 🔥 强制上位逻辑 🔥
        // 如果之前有其他的 Instance，那是旧时代的残党，直接杀掉！
        // 我们要用当前场景里这个配置齐全的新 Manager！
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"⚔️ [BattleManager] 发现旧的实例 {Instance.gameObject.name}，正在销毁它...");
            Destroy(Instance.gameObject); // 杀掉旧的
        }

        // 我就是新的王！
        Instance = this;
        
        // 注意：因为 BattleManager 现在是属于 GameScene 本地的，
        // 所以【不要】加 DontDestroyOnLoad。
        // 让它随场景生，随场景死。
        
        Debug.Log("✅ [BattleManager] 初始化完成，我是新的单例。");
    }    
    
    void Start()
    {
        // --- 1. 按钮绑定的“双重保险” ---
        if (EndTurnBtn != null)
        {
            // 🔥 关键一步：先移除所有旧的监听！
            // 防止：如果脚本重置，按钮被绑定了两次，点击一下就会触发两次结算（导致双倍弹窗）
            EndTurnBtn.onClick.RemoveAllListeners(); 
            
            // 然后再绑定
            EndTurnBtn.onClick.AddListener(OnEndTurnClicked);
        }
        else
        {
            // 之前的报错教训：如果没有这一行，EndTurnBtn 为空时游戏直接卡死
            Debug.LogError("❌ [BattleManager] Start时发现 EndTurnBtn 是空的！请检查 Awake 是否自动找到了它。");
        }

        // --- 2. 面板隐藏 ---
        if (BattlePanel != null)
        {
            BattlePanel.SetActive(false);
        }
    }
    // --- 1. 战斗初始化 ---
    public void StartBattle(DataManager.EventData evt)
    {
        Debug.Log("⚔️ [Battle] 正在初始化战斗...");

        // 1. 强制打开面板 (双重保险)
        if (BattlePanel != null) 
        {
            BattlePanel.SetActive(true);
            Debug.Log("⚔️ [Battle] 面板已激活");
        }
        else Debug.LogError("❌ [Battle] BattlePanel 没拖！无法显示！");

        enemyCenterHP = 5; 
        
        // 2. 解析敌人
        string enemyName = "未知敌军";
        if (evt != null && !string.IsNullOrEmpty(evt.OptA_Res1_Data))
        {
            Debug.Log($"⚔️ [Battle] 解析敌人数据: {evt.OptA_Res1_Data}");
            // 注意：这里如果 Split 失败会报错，加个 TryCatch
            try {
                if (evt.OptA_Res1_Data.StartsWith("ENEMY:")) {
                    int eid = int.Parse(evt.OptA_Res1_Data.Split(':')[1]);
                    var enemy = DataManager.Instance.GetEnemyByID(eid);
                    if(enemy != null) enemyName = enemy.Name;
                }
            } catch { Debug.LogError("❌ [Battle] 敌人数据解析失败！"); }
        }
        
        if (EnemyInfoText != null) EnemyInfoText.text = $"{enemyName} (中军生命: {enemyCenterHP})";

        // 3. 初始化卡组
        if (DataManager.Instance == null) { Debug.LogError("❌ [Battle] DataManager 丢失！"); return; }
        
        Debug.Log("⚔️ [Battle] 正在洗牌...");
        drawPile = new List<DataManager.CardData>(DataManager.Instance.GetStarterDeck());
        Shuffle(drawPile);
        handPile.Clear();
        discardPile.Clear();

        // 4. 初始化战线
        Debug.Log("⚔️ [Battle] 重置战线...");
        if (Lanes == null || Lanes.Length == 0) Debug.LogError("❌ [Battle] Lanes 数组是空的！没法打仗！");
        else 
        {
            foreach(var lane in Lanes) 
            {
                if(lane != null) lane.ResetLane();
            }
        }

        Debug.Log("⚔️ [Battle] 回合开始！");
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
        Debug.Log("🚀 [结算] 协程启动！");

        // 1. 检查 Log 组件
        if (BattleLogText != null) 
        {
            BattleLogText.text = ">>> 开始战斗结算...";
        }
        else 
        {
            Debug.LogError("❌ [结算中断] BattleLogText 没拖！代码在这里死掉了！");
            yield break; // 强制退出
        }

        yield return new WaitForSeconds(0.5f);

        int totalDamageToEnemy = 0;

        // 2. 检查 Lanes 数组
        if (Lanes == null || Lanes.Length < 3)
        {
            Debug.LogError("❌ [结算中断] Lanes 数组没满3个！请去 Inspector 拖拽赋值！");
            yield break;
        }

        // 依次结算 3 路
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"⚔️ [结算] 正在处理第 {i} 路...");
            
            var lane = Lanes[i];
            if (lane == null)
            {
                Debug.LogError($"❌ [结算中断] 第 {i} 路 (Element {i}) 是空的 (None)！");
                yield break;
            }

            int myPower = lane.GetTotalPower();
            int enemyPower = lane.EnemyPower;
            
            // ... (原有的结算逻辑) ...
            // 为了测试，先简单打印一下
            Debug.Log($"   -> 我方: {myPower} vs 敌方: {enemyPower}");

            // 模拟伤害逻辑 (把你原来的逻辑粘回来，或者暂时只保留 Log)
            if (lane.IsEnemyAttacking)
            {
                int netDamage = enemyPower - myPower;
                if (netDamage > 0)
                {
                    if(BattleLogText) BattleLogText.text = $"{lane.LaneName}: 防守失败！受到 {netDamage} 伤害";
                    ResourceManager.Instance.ChangeResource(104, -netDamage);
                }
                else
                {
                    if(BattleLogText) BattleLogText.text = $"{lane.LaneName}: 成功防御！";
                }
            }
            else 
            {
                int netDamage = myPower - enemyPower;
                if (netDamage > 0)
                {
                    int finalDmg = netDamage * netDamage;
                    if(BattleLogText) BattleLogText.text = $"{lane.LaneName}: 突破防线！造成 {finalDmg} 伤害";
                    totalDamageToEnemy += finalDmg;
                }
                else
                {
                    if(BattleLogText) BattleLogText.text = $"{lane.LaneName}: 攻击被阻挡。";
                }
            }

            // 清理卡牌
            discardPile.AddRange(lane.ClearLane());

            yield return new WaitForSeconds(1f);
        }

        // 3. 结算完毕
        Debug.Log($"🏁 [结算] 最终伤害: {totalDamageToEnemy}");
        
        enemyCenterHP -= totalDamageToEnemy;
        if (EnemyInfoText != null) EnemyInfoText.text = $"敌军中军生命: {enemyCenterHP}";

        if (enemyCenterHP <= 0)
        {
            EndBattle(true);
        }
        else
        {
            // 敌方反击
            ResourceManager.Instance.ChangeResource(104, -5);
            if(BattleLogText) BattleLogText.text = "敌方反击！我军兵力 -5";
            yield return new WaitForSeconds(1f);
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