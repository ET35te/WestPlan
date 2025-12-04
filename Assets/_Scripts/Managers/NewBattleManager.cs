using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NewBattleManager : MonoBehaviour
{
    public static NewBattleManager Instance { get; private set; }

    // 定义三种姿态
    public enum MilitaryStance { Empty = 0, Attack = 1, Defend = 2 }

    [System.Serializable]
    public class LaneData
    {
        public string Name;
        public MilitaryStance EnemyIntent;  // 敌方意图
        public MilitaryStance PlayerCommand; // 玩家指令
        public BattleLaneUI UI_Reference;   // UI引用
    }

    [Header("配置")]
    public LaneData[] Lanes = new LaneData[5]; // 0:左, 1:前, 2:中, 3:后, 4:右
    public int MaxActionPoints = 3;            // 每回合行动点 (限制玩家不能全攻)
    public int CurrentActionPoints;

    [Header("UI引用")]
    public GameObject BattlePanel;
    public TMP_Text AP_Text;
    public Button ExecuteBtn;
    public TMP_Text BattleLogText; // 简单战报显示

    private DataManager.EventData currentEvent;

    private void Awake() { Instance = this; }

    void Start()
    {
        // 绑定执行按钮
        ExecuteBtn.onClick.AddListener(ExecuteTurn);
        BattlePanel.SetActive(false);
    }

    // --- 1. 战斗初始化 ---
    public void StartBattle(DataManager.EventData evt)
    {
        currentEvent = evt;
        BattlePanel.SetActive(true);
        BattleLogText.text = ">> 战斗开始！敌军阵势已成！";

        // 初始化5路数据
        string[] names = { "左翼", "前军", "中军", "后军", "右翼" };
        
        // 获取UI里的 5 个 Lane 对象 (假设你已经在编辑器里拖进去了，或者通过代码找)
        // 这里假设你在 Inspector 里已经把 Lanes 数组的大小设为 5，并且把 UI_Reference 拖进去了
        for (int i = 0; i < 5; i++)
        {
            Lanes[i].Name = names[i];
            Lanes[i].PlayerCommand = MilitaryStance.Empty; // 玩家默认空
            
            // 随机生成敌方意图 (简单AI)
            // 50%概率攻，30%概率守，20%概率空
            float rand = Random.value;
            if (rand < 0.5f) Lanes[i].EnemyIntent = MilitaryStance.Attack;
            else if (rand < 0.8f) Lanes[i].EnemyIntent = MilitaryStance.Defend;
            else Lanes[i].EnemyIntent = MilitaryStance.Empty;

            // 初始化UI
            Lanes[i].UI_Reference.Setup(i, names[i]);
            Lanes[i].UI_Reference.UpdateEnemyView(Lanes[i].EnemyIntent);
            Lanes[i].UI_Reference.UpdatePlayerView(Lanes[i].PlayerCommand);
        }

        CurrentActionPoints = MaxActionPoints;
        UpdateAPDisplay();
    }

    // --- 2. 玩家点击槽位 (切换指令) ---
    public void OnLaneClicked(int index)
    {
        // 简单的循环切换：空 -> 攻 -> 守 -> 空
        // 可以在这里加入 Cost 判断 (比如没行动点就不能切到攻)
        
        MilitaryStance current = Lanes[index].PlayerCommand;
        MilitaryStance next = MilitaryStance.Empty;

        if (current == MilitaryStance.Empty) next = MilitaryStance.Attack;
        else if (current == MilitaryStance.Attack) next = MilitaryStance.Defend;
        else if (current == MilitaryStance.Defend) next = MilitaryStance.Empty;

        // 检查行动点消耗 (假设 攻耗1点，守耗1点，空不耗)
        int costDiff = GetCost(next) - GetCost(current);
        
        if (CurrentActionPoints - costDiff >= 0)
        {
            CurrentActionPoints -= costDiff;
            Lanes[index].PlayerCommand = next;
            
            // 更新UI
            Lanes[index].UI_Reference.UpdatePlayerView(next);
            UpdateAPDisplay();
        }
        else
        {
            Debug.Log("行动点不足！");
            // 可以加个飘字提示
        }
    }

    int GetCost(MilitaryStance stance)
    {
        if (stance == MilitaryStance.Empty) return 0;
        return 1; // 攻和守都消耗1点 AP
    }

    void UpdateAPDisplay()
    {
        AP_Text.text = $"行动令: {CurrentActionPoints} / {MaxActionPoints}";
    }

    // --- 3. 执行回合 (核心结算) ---
    void ExecuteTurn()
    {
        int totalPlayerDamage = 0;
        int totalEnemyDamage = 0;
        BattleLogText.text = "";

        // 遍历 5 路结算
        for (int i = 0; i < 5; i++)
        {
            MilitaryStance p = Lanes[i].PlayerCommand;
            MilitaryStance e = Lanes[i].EnemyIntent;
            string laneName = Lanes[i].Name;
            
            // 扣除资源消耗
            if (p == MilitaryStance.Attack) ResourceManager.Instance.ChangeResource(104, -5); // 攻耗兵
            if (p == MilitaryStance.Defend) ResourceManager.Instance.ChangeResource(102, -10); // 守耗粮

            // 博弈逻辑
            if (p == MilitaryStance.Attack && e == MilitaryStance.Empty)
            {
                totalPlayerDamage += 20; // 直击空门，大伤
                Lanes[i].UI_Reference.ShowResult("突袭成功!", Color.green);
                Log($"{laneName}: 趁虚而入，敌军重创！");
            }
            else if (p == MilitaryStance.Attack && e == MilitaryStance.Attack)
            {
                totalPlayerDamage += 10;
                totalEnemyDamage += 10; // 对攻，两败俱伤
                Lanes[i].UI_Reference.ShowResult("血战!", Color.yellow);
                Log($"{laneName}: 双方血战，各有损伤。");
            }
            else if (p == MilitaryStance.Attack && e == MilitaryStance.Defend)
            {
                totalEnemyDamage += 5; // 撞墙了
                Lanes[i].UI_Reference.ShowResult("被阻挡", Color.gray);
                Log($"{laneName}: 攻击被敌方防御阻挡。");
            }
            else if (p == MilitaryStance.Defend && e == MilitaryStance.Attack)
            {
                totalEnemyDamage += 2; // 成功防御
                Lanes[i].UI_Reference.ShowResult("完美防御!", Color.cyan);
                Log($"{laneName}: 成功抵御敌军冲锋！");
            }
            else if (p == MilitaryStance.Empty && e == MilitaryStance.Attack)
            {
                totalEnemyDamage += 20; // 空门被打
                Lanes[i].UI_Reference.ShowResult("防线溃败!", Color.red);
                Log($"{laneName}: 防线空虚，被敌军突破！");
            }
            else
            {
                Lanes[i].UI_Reference.ShowResult("无事发生", Color.white);
            }
        }

        // 最终结算
        // 这里简化为：谁造成的伤害高谁赢
        EndBattle(totalPlayerDamage > totalEnemyDamage);
    }

    void Log(string msg)
    {
        BattleLogText.text += msg + "\n";
    }

    void EndBattle(bool isWin)
    {
        // 延迟一点关闭，让玩家看清结果 (这里简化直接关)
        BattlePanel.SetActive(false);
        
        string res = isWin ? "【大捷】五路博弈，技高一筹！" : "【败北】指挥失误，防线崩坏！";
        if(isWin) ResourceManager.Instance.ChangeResource(105, 50);
        else ResourceManager.Instance.ChangeResource(104, -50);

        UIManager.Instance.ShowResult(res);
    }
}