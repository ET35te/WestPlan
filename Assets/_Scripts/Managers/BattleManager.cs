using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("--- UI 引用 (可为空，防报错) ---")]
    public TMP_Text PlayerResourceText; // 显示粮/甲
    public TMP_Text EnemyResourceText;
    public TMP_Text BattleLogText;      // 战斗日志
    public Button AttackBtn;
    public Button DefendBtn;
    public Button SkipBtn;

    [Header("--- 战斗核心数据 ---")]
    // 双方生命值 (代表兵力，为0则败)
    public int PlayerUnitCount;
    public int EnemyUnitCount;

    // 双方资源
    public int PlayerFood, PlayerArmor;
    public int EnemyFood, EnemyArmor;

    // 回合状态标记
    private bool isPlayerTurn;
    private bool isFirstAttackOfTurn; // 标记是否为首攻
    private bool playerIsDefending;   // 玩家本回合是否防御中
    private bool enemyIsDefending;    // 敌人本回合是否防御中

    private int turnCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartBattle(null);
    }

    // 🔥 键盘调试模式：远程开发神器
    void Update()
    {
        if (!isPlayerTurn) return; // 只有玩家回合才响应按键

        // 按 A 进攻
        if (Input.GetKeyDown(KeyCode.A)) 
        {
            Debug.Log("⌨️ [键盘 A] -> 尝试进攻");
            OnAttackCmd();
        }
        // 按 D 防守
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("⌨️ [键盘 D] -> 尝试防守");
            OnDefendCmd();
        }
        // 按 Space 空过
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("⌨️ [键盘 Space] -> 尝试空过");
            OnSkipCmd();
        }
    }

    // --- 1. 战斗初始化 ---
    public void StartBattle(DataManager.EnemyData enemyData)
    {
        Debug.Log("<color=yellow>⚔️ 战斗开始！单路死斗模式！</color>");

        // 初始化数值
        PlayerUnitCount = GlobalConfig.Initial_Unit_Count;
        EnemyUnitCount = GlobalConfig.Initial_Unit_Count;

        PlayerFood = GlobalConfig.Player_Start_Food;
        PlayerArmor = GlobalConfig.Player_Start_Armor;
        
        EnemyFood = GlobalConfig.Enemy_Start_Food;
        EnemyArmor = GlobalConfig.Enemy_Start_Armor;

        turnCount = 0;
        
        // 刷新UI (带防空检查)
        UpdateUI();

        // 开始第一回合
        StartCoroutine(StartTurnRoutine());
    }

    // --- 2. 回合流程 ---
    IEnumerator StartTurnRoutine()
    {
        turnCount++;
        isFirstAttackOfTurn = true; // 重置首攻标记
        playerIsDefending = false;  // 重置防御姿态
        enemyIsDefending = false;

        // --- 资源恢复阶段 ---
        Debug.Log($"\n>>> 第 {turnCount} 回合开始 <<<");
        
        PlayerFood += GlobalConfig.Turn_Regen_Food;
        PlayerArmor += GlobalConfig.Turn_Regen_Armor;
        EnemyFood += GlobalConfig.Turn_Regen_Food;
        EnemyArmor += GlobalConfig.Turn_Regen_Armor;
        
        Debug.Log($"[资源恢复] 玩家粮:{PlayerFood} 甲:{PlayerArmor} | 敌方粮:{EnemyFood} 甲:{EnemyArmor}");
        UpdateUI();

        // --- 玩家行动阶段 ---
        isPlayerTurn = true;
        LogToScreen("轮到你了！按 A进攻, D防守(耗1粮), Space空过");
        
        // 等待玩家操作 (通过按钮或键盘触发 On...Cmd)
        yield return null; 
    }

    // --- 3. 玩家指令 (Command) ---

    // ⚔️ 进攻指令
    public void OnAttackCmd()
    {
        if (!isPlayerTurn) return;

        // 计算伤害
        int damage = CalculateDamage(PlayerUnitCount, EnemyArmor, enemyIsDefending, isFirstAttackOfTurn);
        
        // 扣血
        EnemyUnitCount -= damage;
        if (EnemyUnitCount < 0) EnemyUnitCount = 0;

        LogToScreen($"⚔️ 你发起进攻！造成 {damage} 点伤害。(敌方剩余兵力: {EnemyUnitCount})");
        
        // 结算
        isPlayerTurn = false;
        isFirstAttackOfTurn = false; // 用过一次攻击了，首攻标记失效
        UpdateUI();
        
        CheckVictoryCondition();
    }

    // 🛡️ 防守指令
    public void OnDefendCmd()
    {
        if (!isPlayerTurn) return;

        // 检查粮草
        if (PlayerFood >= GlobalConfig.Defend_Cost_Food)
        {
            PlayerFood -= GlobalConfig.Defend_Cost_Food;
            playerIsDefending = true;
            
            LogToScreen($"🛡️ 你消耗1粮进入防御姿态！(下一次受击减伤 {GlobalConfig.Defend_Mitigation})");
            
            isPlayerTurn = false;
            UpdateUI();
            StartCoroutine(EnemyTurnRoutine()); // 玩家结束，进敌人回合
        }
        else
        {
            LogToScreen("❌ 粮草不足，无法防守！");
        }
    }

    // ⏭️ 空过指令
    public void OnSkipCmd()
    {
        if (!isPlayerTurn) return;
        
        LogToScreen("💨 你选择了空过，保留资源。");
        isPlayerTurn = false;
        StartCoroutine(EnemyTurnRoutine());
    }

    // --- 4. 敌人回合 (简单的 AI) ---
    IEnumerator EnemyTurnRoutine()
    {
        LogToScreen("Thinking... 敌方思考中");
        yield return new WaitForSeconds(1.0f); // 模拟思考时间

        // 简单 AI：如果有粮就 50% 概率防守，否则进攻
        bool enemyDefends = (EnemyFood >= 1 && Random.value > 0.5f);

        if (enemyDefends)
        {
            EnemyFood -= 1;
            enemyIsDefending = true;
            LogToScreen("🛡️ 敌方消耗粮草，筑起了防线！");
        }
        else
        {
            // 敌人进攻
            // 注意：这里简单模拟，敌人如果是后手，它也算它自己的“回合首攻”，但在当前流程里，
            // 它是对玩家发起攻击。我们可以复用公式，但要反过来传参。
            // (注：严格来说"首攻无视护甲"通常指进攻方回合，这里简化处理)
            
            int damage = CalculateDamage(EnemyUnitCount, PlayerArmor, playerIsDefending, true);
            PlayerUnitCount -= damage;
            if (PlayerUnitCount < 0) PlayerUnitCount = 0;
            
            LogToScreen($"⚔️ 敌方发起进攻！对你造成 {damage} 点伤害。");
        }

        UpdateUI();
        CheckVictoryCondition();
        
        // 如果双方都活着，进下一回合
        if (PlayerUnitCount > 0 && EnemyUnitCount > 0)
        {
            StartCoroutine(StartTurnRoutine());
        }
    }

    // --- 5. 伤害计算公式 ---
    int CalculateDamage(int attackerPower, int defenderArmor, bool isDefending, bool ignoreMitigation)
    {
        // 1. 基础伤害 = 兵力 * 系数
        float rawDamage = attackerPower * GlobalConfig.Attack_Base_Mult;

        // 2. 护甲减免 (每1点甲抵消1点伤，示例逻辑)
        // 规则：如果是首攻 (ignoreMitigation)，可能无视护甲提供的额外加成，
        // 但这里我们先按你的需求：首攻无视"守"指令的减伤，还是无视"盾"资源？
        // 根据你之前的描述：进攻方首回合默认不受...加持 -> 应该是无视"盾"值。
        
        int armorReduction = defenderArmor; 
        if (ignoreMitigation) 
        {
            // 首攻：无视盾的减伤 (或者减半，看你具体规则，这里先设为无视)
            armorReduction = 0; 
            Debug.Log("⚡ [首攻] 无视护甲！");
        }

        float finalDamage = rawDamage - armorReduction;

        // 3. "守"指令的额外减伤
        if (isDefending)
        {
            finalDamage -= GlobalConfig.Defend_Mitigation;
            Debug.Log("🛡️ [防守] 触发减伤！");
        }

        return (int)Mathf.Max(0, finalDamage);
    }

    // --- 6. 胜利判定 ---
    void CheckVictoryCondition()
    {
        if (EnemyUnitCount <= 0)
        {
            LogToScreen("<color=green>🏆 敌军全灭！你赢了！</color>");
            StopAllCoroutines(); // 停止回合循环
        }
        else if (PlayerUnitCount <= 0)
        {
            LogToScreen("<color=red>💀 全军覆没... 你输了。</color>");
            StopAllCoroutines();
        }
        else if (isPlayerTurn == false) // 如果还没分胜负且玩家操作完了，进敌人回合
        {
             if(!playerIsDefending) StartCoroutine(EnemyTurnRoutine());
             // 注意：如果是 AttackCmd 调用的检查，上面已经处理了切回合逻辑，
             // 这里主要是防止逻辑重复。通常 CheckVictory 只负责检查死没死。
             // 简便起见，让 AttackCmd 自己去调切回合，CheckVictory 只报结果。
        }
    }

    // --- 辅助：UI 更新 (带防空) ---
    void UpdateUI()
    {
        if (PlayerResourceText != null)
            PlayerResourceText.text = $"粮: {PlayerFood}\n甲: {PlayerArmor}\n兵: {PlayerUnitCount}";
            
        if (EnemyResourceText != null)
            EnemyResourceText.text = $"粮: {EnemyFood}\n甲: {EnemyArmor}\n兵: {EnemyUnitCount}";
    }

    void LogToScreen(string msg)
    {
        Debug.Log(msg); // 打印到 Console
        if (BattleLogText != null) BattleLogText.text = msg; // 如果有UI也显示
    }
}