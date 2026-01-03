using UnityEngine;

/// <summary>
/// 🎁 动态战利品系统
/// 实现防滚雪球的资源循环机制
/// </summary>
public class DynamicLootSystem
{
    /// <summary>
    /// 🎁 计算战利品掉落
    /// 
    /// 核心公式:
    /// 获得粮草 = 基础掉落 + (粮草上限 - 当前粮草) * 0.5
    /// 
    /// 效果示例:
    /// - 玩家粮草: 10/100 (贫穷) → 获得 55 粮 (基础30 + (100-10)*0.5)
    /// - 玩家粮草: 80/100 (富裕) → 获得 40 粮 (基础30 + (100-80)*0.5)
    /// - 玩家粮草: 100/100(饱和) → 获得 30 粮 (基础30 + 0)
    /// 
    /// 这种机制自动平衡：
    /// - 穷人更容易翻身（追赶机制）
    /// - 富人需要花钱（防止无限积累）
    /// </summary>
    public static int CalculateLootReward(
        int baseLoot,           // 基础掉落 (如30)
        int currentGrain,       // 玩家当前粮草
        int grainCapacity)      // 粮草上限 (如100)
    {
        // 计算赤字系数
        float deficit = grainCapacity - currentGrain;
        float compensationFactor = deficit * 0.5f;
        
        // 最终掉落 = 基础 + 补偿
        int finalLoot = Mathf.Max(
            baseLoot,  // 至少是基础掉落
            Mathf.RoundToInt(baseLoot + compensationFactor)
        );

        return finalLoot;
    }

    /// <summary>
    /// 🎁 高级战利品计算 - 同时处理多种资源
    /// </summary>
    public class LootRewardPackage
    {
        public int GrainReward;     // 粮草奖励
        public int ArmorReward;     // 护甲奖励
        public int BonusGrain;      // 额外粮草(动态补偿)
        public int TotalGrainValue; // 总价值(用于统计)

        public override string ToString()
        {
            return $"粮{GrainReward}+{BonusGrain}额外 甲{ArmorReward}";
        }
    }

    /// <summary>
    /// 🎁 计算完整的战利品包
    /// </summary>
    public static LootRewardPackage CalculateFullLoot(
        int baseGrain,          // 基础粮草掉落
        int baseArmor,          // 基础护甲掉落
        int currentGrain,       // 玩家当前粮草
        int currentArmor,       // 玩家当前护甲
        int grainCapacity,      // 粮草上限
        int armorCapacity)      // 护甲上限
    {
        var package = new LootRewardPackage();

        // 计算粮草奖励
        int deficitGrain = grainCapacity - currentGrain;
        int compensationGrain = Mathf.RoundToInt(deficitGrain * 0.5f);
        package.GrainReward = baseGrain;
        package.BonusGrain = compensationGrain;

        // 计算护甲奖励
        // 护甲也有类似的补偿机制，但系数更低(0.3)
        // 因为护甲是"库存"，不如粮草那么紧张
        int deficitArmor = armorCapacity - currentArmor;
        int compensationArmor = Mathf.RoundToInt(deficitArmor * 0.3f);
        package.ArmorReward = baseArmor + compensationArmor;

        // 总价值计算(便于统计)
        // 按照汇率: 1护甲 ≈ 1.5粮
        package.TotalGrainValue = package.GrainReward + package.BonusGrain
                                + Mathf.RoundToInt(package.ArmorReward * 1.5f);

        return package;
    }

    /// <summary>
    /// 🎯 根据战斗强度调整战利品
    /// 
    /// 强度系数:
    /// 1. 简单战斗 (Rest): 系数 0.8 (掉落偏少)
    /// 2. 普通战斗 (Normal): 系数 1.0 (标准掉落)
    /// 3. 精英战斗 (Spike): 系数 1.2 (掉落增多)
    /// 4. Boss战: 系数 1.5 (大量掉落)
    /// </summary>
    public enum BattleDifficulty
    {
        Rest = 0,
        Normal = 1,
        Spike = 2,
        Boss = 3,
    }

    public static float GetDifficultyMultiplier(BattleDifficulty difficulty)
    {
        return difficulty switch
        {
            BattleDifficulty.Rest => 0.8f,
            BattleDifficulty.Normal => 1.0f,
            BattleDifficulty.Spike => 1.2f,
            BattleDifficulty.Boss => 1.5f,
            _ => 1.0f,
        };
    }

    /// <summary>
    /// 🎯 应用战斗难度系数到战利品
    /// </summary>
    public static LootRewardPackage ApplyDifficultyMultiplier(
        LootRewardPackage baseReward,
        BattleDifficulty difficulty)
    {
        float multiplier = GetDifficultyMultiplier(difficulty);

        var adjusted = new LootRewardPackage
        {
            GrainReward = Mathf.RoundToInt(baseReward.GrainReward * multiplier),
            BonusGrain = Mathf.RoundToInt(baseReward.BonusGrain * multiplier),
            ArmorReward = Mathf.RoundToInt(baseReward.ArmorReward * multiplier),
            TotalGrainValue = Mathf.RoundToInt(baseReward.TotalGrainValue * multiplier),
        };

        return adjusted;
    }

    /// <summary>
    /// 📊 流派推荐掉落偏向
    /// 
    /// 设计思路：根据玩家选择的流派，调整掉落倾向
    /// - Aggro: 偏向粮草(快速循环)
    /// - Control: 偏向护甲(积累库存)
    /// - Midrange: 均衡
    /// </summary>
    public static LootRewardPackage AdjustByArchetype(
        LootRewardPackage baseReward,
        GameBalanceCalculator.CardArchetype archetype)
    {
        var adjusted = new LootRewardPackage
        {
            GrainReward = baseReward.GrainReward,
            BonusGrain = baseReward.BonusGrain,
            ArmorReward = baseReward.ArmorReward,
            TotalGrainValue = baseReward.TotalGrainValue,
        };

        switch (archetype)
        {
            case GameBalanceCalculator.CardArchetype.Aggro:
                // 快攻需要快速循环，增加粮草掉落 +30%
                adjusted.GrainReward = Mathf.RoundToInt(adjusted.GrainReward * 1.3f);
                adjusted.BonusGrain = Mathf.RoundToInt(adjusted.BonusGrain * 1.3f);
                adjusted.ArmorReward = Mathf.RoundToInt(adjusted.ArmorReward * 0.7f);
                break;

            case GameBalanceCalculator.CardArchetype.Control:
                // 控制需要积累护甲，增加护甲掉落 +40%
                adjusted.ArmorReward = Mathf.RoundToInt(adjusted.ArmorReward * 1.4f);
                adjusted.GrainReward = Mathf.RoundToInt(adjusted.GrainReward * 0.8f);
                break;

            case GameBalanceCalculator.CardArchetype.Midrange:
                // 中速均衡分配
                break;

            case GameBalanceCalculator.CardArchetype.Combo:
                // 组合需要抽牌，增加粮草掉落 +20%
                adjusted.GrainReward = Mathf.RoundToInt(adjusted.GrainReward * 1.2f);
                break;
        }

        // 重新计算总价值
        adjusted.TotalGrainValue = adjusted.GrainReward + adjusted.BonusGrain
                                 + Mathf.RoundToInt(adjusted.ArmorReward * 1.5f);

        return adjusted;
    }

    /// <summary>
    /// 📈 生成战利品表(用于测试和可视化)
    /// </summary>
    [System.Serializable]
    public class LootTableEntry
    {
        public int NodeIndex;
        public string NodeName;
        public int BaseGrain;
        public int BaseArmor;
        public string RestReward;      // Rest场景战利品
        public string NormalReward;    // Normal场景战利品
        public string SpikeReward;     // Spike场景战利品
        public string BossReward;      // Boss场景战利品
    }
}
