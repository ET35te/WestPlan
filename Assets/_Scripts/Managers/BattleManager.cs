using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("UI引用")]
    public GameObject BattlePanel;      // 战斗总界面 (War Council)
    public Transform CardContainer;     // 放卡牌的父物体
    public GameObject CardPrefab;       // 卡牌预制体 (挂了BattleCardUI的物体)
    
    [Header("配置")]
    public int EnemyBasePower = 5000;   // 临时写死的敌人战力 (未来从Event表读)

    private DataManager.EventData currentEvent; // 记录当前是哪个事件触发的战斗

    private void Awake() { Instance = this; }

    // --- 1. 开始战斗 (由 UIManager 调用) ---
    public void StartBattle(DataManager.EventData evt)
    {
        currentEvent = evt;
        BattlePanel.SetActive(true);

        // 抽取 4 张卡
        List<DataManager.BattleCardData> cards = DataManager.Instance.DrawBattleCards(4);
        
        // 生成 UI
        // 先清空旧卡
        foreach (Transform child in CardContainer) Destroy(child.gameObject);
        
        // 生成新卡
        foreach (var cardData in cards)
        {
            GameObject newCard = Instantiate(CardPrefab, CardContainer);
            newCard.GetComponent<BattleCardUI>().Setup(cardData, OnCardSelected);
        }
    }

    // --- 2. 玩家选卡 (点击卡牌后触发) ---
    void OnCardSelected(DataManager.BattleCardData card)
    {
        // A. 处理牺牲逻辑
        bool isSacrificeTriggered = false;
        
        // 获取当前对应资源的库存
        int currentResVal = ResourceManager.Instance.GetResourceValue(card.Cond_ResID);
        
        // 判定条件：是满足阈值(天然绝境) 还是 需要主动牺牲?
        bool isNatural = currentResVal < card.Cond_Val;
        
        if (!isNatural && card.Sacrifice_Cost > 0)
        {
            // 需要牺牲：扣除资源
            int burnAmount = (int)(currentResVal * card.Sacrifice_Cost);
            ResourceManager.Instance.ChangeResource(card.Cond_ResID, -burnAmount);
            isSacrificeTriggered = true;
            Debug.Log($"【战报】玩家选择了牺牲！烧毁 {burnAmount} 点 {card.Cond_ResID}");
        }
        else if (isNatural)
        {
            Debug.Log("【战报】天然绝境条件满足，无需牺牲！");
        }

        // B. 计算战力
        // 基础战力公式：兵力*100 + 马匹*50 + 披甲*20
        float basePower = (ResourceManager.Instance.Troops * 100) 
                        + (ResourceManager.Instance.Horses * 50) 
                        + (ResourceManager.Instance.Armor * 20);

        // 计算Buff
        float multiplier = 1.0f;
        // 如果满足天然条件 OR 触发了牺牲，则享受Buff
        if (isNatural || isSacrificeTriggered)
        {
            multiplier = card.Buff_Val; 
        }

        float finalPower = basePower * multiplier;
        Debug.Log($"【结算】我方战力 {finalPower} (Buff: {multiplier}) VS 敌方 {EnemyBasePower}");

        // C. 判定胜负
        bool isWin = finalPower >= EnemyBasePower;

        // D. 结束战斗，通知UI
        EndBattle(isWin);
    }

    // --- 3. 结束战斗 ---
    void EndBattle(bool isWin)
    {
        BattlePanel.SetActive(false); // 关闭战斗选卡界面

        // 构造一个战斗结果文本
        string battleResultStr = isWin ? 
            "【大捷】\n你的奇策成功了！敌军溃不成军，我军缴获了大量物资。" : 
            "【惨败】\n战力悬殊，虽然拼死抵抗，但我军伤亡惨重，被迫撤退。";

        // 如果赢了，给奖励；输了，扣兵力
        if (isWin) ResourceManager.Instance.ChangeResource(105, 50); // 赢了加钱
        else ResourceManager.Instance.ChangeResource(104, -50); // 输了死人

        // 调用 UIManager 显示结果弹窗
        // 注意：这里我们复用 ResultPanel
        UIManager.Instance.ShowResult(battleResultStr);
    }
}

