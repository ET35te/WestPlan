using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🎮 游戏平衡计算系统
/// 根据手册的单位定价模型(Unit Costing Model)，重构卡牌数值和敌人成长曲线
/// 
/// 核心概念:
/// 1. 基准线(Baseline): 1粮 = 5点价值
/// 2. 汇率: 1护甲 ≈ 1.5粮（考虑损失厌恶）
/// 3. 指数膨胀系数: 高费卡(2费+) * 1.2倍
/// 4. 护甲消耗溢价: * 1.6倍（鼓励"卖血"输出）
/// </summary>
public class GameBalanceCalculator
{
    // ============================================================
    // 📊 第一部分：卡牌定价模型
    // ============================================================

    /// <summary>
    /// 基准常量
    /// </summary>
    private const float GRAIN_BASE_VALUE = 5f;           // 1粮 = 5点价值
    private const float ARMOR_TO_GRAIN_RATE = 1.5f;      // 1护甲 ≈ 1.5粮
    private const float EXPONENTIAL_MULTIPLIER = 1.2f;   // 高费卡膨胀系数
    private const float ARMOR_COST_PREMIUM = 1.6f;       // 护甲消耗溢价

    /// <summary>
    /// 📈 计算卡牌的理论伤害值(Damage)
    /// 
    /// 规则:
    /// 1. 低费卡(1费): 线性关系 Value = Cost_Grain * 5
    /// 2. 高费卡(2费+): 指数膨胀 Value = (Cost_Grain * 5) * (1 + (Cost_Grain - 1) * 0.2)
    /// 3. 护甲消耗: 高溢价 Value = Cost_Armor * 8
    /// </summary>
    public static float CalculateCardDamage(int costGrain, int costArmor)
    {
        float damageValue = 0f;

        // 粮草成本贡献
        if (costGrain > 0)
        {
            float baseValue = costGrain * GRAIN_BASE_VALUE;

            // 如果是高费卡(2费+)，应用指数膨胀
            if (costGrain >= 2)
            {
                float exponentialBonus = 1f + (costGrain - 1) * 0.2f;
                damageValue += baseValue * exponentialBonus;
            }
            else
            {
                damageValue += baseValue;
            }
        }

        // 护甲消耗贡献（极高的转化率）
        if (costArmor > 0)
        {
            damageValue += costArmor * 8f * ARMOR_COST_PREMIUM;
        }

        return damageValue;
    }

    /// <summary>
    /// 📊 计算卡牌的理论盾值(Shield/Defense)
    /// 逻辑同上，但名义上用于防御或积累
    /// </summary>
    public static float CalculateCardDefense(int costGrain, int costArmor)
    {
        // 防御卡通常优先使用粮草成本而非护甲
        float defenseValue = 0f;

        if (costGrain > 0)
        {
            float baseValue = costGrain * GRAIN_BASE_VALUE;
            if (costGrain >= 2)
            {
                float exponentialBonus = 1f + (costGrain - 1) * 0.2f;
                defenseValue += baseValue * exponentialBonus;
            }
            else
            {
                defenseValue += baseValue;
            }
        }

        // 护甲消耗在防御中较少使用，但如果有则加权
        if (costArmor > 0)
        {
            defenseValue += costArmor * 5f;  // 相比伤害卡的8倍，防御卡只有5倍
        }

        return defenseValue;
    }

    // ============================================================
    // 📊 第二部分：敌人成长曲线（查表法）
    // ============================================================

    /// <summary>
    /// 敌人等级数据
    /// 根据关卡(NodeIndex)自动查表，避免实时计算
    /// </summary>
    [System.Serializable]
    public class EnemyLevelData
    {
        public int NodeIndex;      // 第几关(0-11)
        public string NodeName;    // 节点名称
        public int BaseATK;        // 基础攻击力（线性）
        public int BaseHP;         // 基础生命值（指数）
        public float WaveFactor;   // 波峰波谷系数(0.8 or 1.2)
        public int FinalATK;       // 最终攻击力
        public int FinalHP;        // 最终生命值

        public EnemyLevelData() { }

        public EnemyLevelData(int nodeIndex, string nodeName, int baseAtk, int baseHp, float waveFactor)
        {
            NodeIndex = nodeIndex;
            NodeName = nodeName;
            BaseATK = baseAtk;
            BaseHP = baseHp;
            WaveFactor = waveFactor;
            FinalATK = baseAtk;
            FinalHP = (int)(baseHp * waveFactor);
        }
    }

    /// <summary>
    /// 🔍 根据NodeIndex生成整个关卡的敌人数据表
    /// 
    /// 公式:
    /// - ATK: 线性增长 = 5 + (NodeIndex * 2)，第10关约为25
    /// - HP: 指数增长 = 30 * (1.2 ^ NodeIndex)，第10关约为185
    /// - 波动系数: Rest=0.8（简单关），Spike=1.2（精英关）
    /// </summary>
    public static List<EnemyLevelData> GenerateEnemyProgression()
    {
        List<EnemyLevelData> progression = new List<EnemyLevelData>();

        string[] nodeNames = {
            "玉门关", "白龙堆", "楼兰", "龟兹", "疏勒", "天山",
            "车师", "高昌", "敦煌", "长安", "洛阳", "终焉"
        };

        // ATK公式参数
        const int ATK_BASE = 5;
        const int ATK_GROWTH = 2;

        // HP公式参数
        const float HP_BASE = 30f;
        const float HP_MULTIPLIER = 1.2f;

        for (int nodeIdx = 0; nodeIdx < 12; nodeIdx++)
        {
            // 线性计算ATK
            int atk = ATK_BASE + (nodeIdx * ATK_GROWTH);

            // 指数计算HP
            float hpExpo = Mathf.Pow(HP_MULTIPLIER, nodeIdx);
            int hp = (int)(HP_BASE * hpExpo);

            // 波峰波谷系数：偶数关为Rest(0.8)，奇数关为Spike(1.2)
            // 这样形成"呼吸感"的难度节奏
            float waveFactor = (nodeIdx % 2 == 0) ? 0.8f : 1.2f;

            var levelData = new EnemyLevelData(
                nodeIdx,
                nodeNames[nodeIdx],
                atk,
                hp,
                waveFactor
            );

            // 应用波动系数
            levelData.FinalATK = Mathf.RoundToInt(atk);  // ATK不受波动影响（玩家防御压力恒定）
            levelData.FinalHP = Mathf.RoundToInt(hp * waveFactor);

            progression.Add(levelData);
        }

        return progression;
    }

    // ============================================================
    // 📊 第三部分：资源循环与战利品机制
    // ============================================================

    /// <summary>
    /// 🎁 动态战利品计算
    /// 
    /// 防止"滚雪球效应"，根据玩家当前资源动态调整掉落
    /// 
    /// 公式:
    /// 获得粮草 = 基础掉落 + (粮草上限 - 当前粮草) * 0.5
    /// 
    /// 解释:
    /// - 贫困玩家(粮少)：获得更多补偿
    /// - 富裕玩家(粮多)：获得更少，防止无限积累
    /// </summary>
    public static int CalculateDynamicLoot(
        int baseLoot,
        int currentGrain,
        int grainCapacity)
    {
        float deficitFactor = (grainCapacity - currentGrain) * 0.5f;
        int finalLoot = Mathf.Max(baseLoot, Mathf.RoundToInt(baseLoot + deficitFactor));

        return finalLoot;
    }

    /// <summary>
    /// 职业/流派定义 - 用于卡池标记和商店刷新
    /// </summary>
    public enum CardArchetype
    {
        Aggro = 0,      // 快攻：1费卡为主，直接伤害
        Control = 1,    // 控制：叠甲卡为主，后期爆发
        Midrange = 2,   // 中速：混合卡组
        Combo = 3,      // 组合：需要多个卡协同
    }

    /// <summary>
    /// 📋 根据流派返回推荐卡池
    /// </summary>
    public static List<string> GetCardPoolByArchetype(CardArchetype archetype)
    {
        switch (archetype)
        {
            case CardArchetype.Aggro:
                return new List<string>
                {
                    "劫掠",      // 0费，造成3伤，获得1粮
                    "轻步兵",    // 1费，造成3伤
                    "斩杀",      // 消耗甲，高伤害
                };

            case CardArchetype.Control:
                return new List<string>
                {
                    "铁壁推进",  // 消耗所有甲，AOE伤害
                    "铁甲卫",    // 1粮1甲，防守兼备
                    "盾猛",      // 叠甲，配合转化
                };

            case CardArchetype.Midrange:
                return new List<string>
                {
                    "虎豹骑",    // 3费，高战力
                    "屯田",      // 获得粮
                    "急行军",    // 抽牌
                };

            default:
                return new List<string>();
        }
    }

    // ============================================================
    // 🔍 第四部分：防坑指南 - 数据验证
    // ============================================================

    /// <summary>
    /// 📊 生成卡牌成本-伤害散点数据，用于验证平衡性
    /// 
    /// 验收标准:
    /// - 点应该分布在一条向上弯曲的曲线周围
    /// - 极上方的点 = OP卡
    /// - 极下方的点 = 废卡
    /// </summary>
    [System.Serializable]
    public class CardBalanceCheckpoint
    {
        public int ID;
        public string Name;
        public int TotalCost;           // Cost_Grain + Cost_Armor * 1.5(转换为粮)
        public float TheoreticalDamage; // 理论伤害
        public float DamagePerCost;     // 单位成本伤害(效率)
        public string BalanceStatus;    // "平衡" / "OP" / "废卡"
    }

    /// <summary>
    /// 🔍 DPS陷阱检查 - 检查穿透伤害
    /// 
    /// 问题: 高频低伤 vs 高护甲玩家 = 死循环
    /// 修正: 确保不仅有普通伤害，还有穿透伤害
    /// </summary>
    public static bool ValidateDPSCurve(List<CardBalanceCheckpoint> cards)
    {
        // 检查是否有高穿透伤害卡
        bool hasPenetration = cards.Any(c => c.Name.Contains("穿透") || c.Name.Contains("斩杀"));

        if (!hasPenetration)
        {
            Debug.LogWarning("⚠️ 警告: 卡池中缺少穿透伤害卡，高护甲流派可能无敌！");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 📈 验证伤害曲线是否为凸曲线(向上弯曲)而不是直线
    /// </summary>
    public static bool ValidateCurveature(List<CardBalanceCheckpoint> cards)
    {
        if (cards.Count < 3) return true;

        // 计算二阶导数的符号(判断凸性)
        // 如果大多数二阶导数 > 0，则是凸曲线(好)
        // 如果大多数二阶导数 < 0，则是凹曲线(坏)

        int positiveCount = 0;

        for (int i = 1; i < cards.Count - 1; i++)
        {
            float y0 = cards[i - 1].DamagePerCost;
            float y1 = cards[i].DamagePerCost;
            float y2 = cards[i + 1].DamagePerCost;

            float secondDerivative = (y2 - y1) - (y1 - y0);
            if (secondDerivative > 0) positiveCount++;
        }

        bool isConvex = positiveCount > cards.Count / 2;

        if (!isConvex)
        {
            Debug.LogWarning("⚠️ 警告: 伤害曲线不是凸的，可能存在费率异常！");
        }

        return isConvex;
    }
}
