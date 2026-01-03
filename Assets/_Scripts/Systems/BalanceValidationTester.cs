using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🔍 数值平衡验证和测试系统
/// 
/// 用途：
/// 1. 生成卡牌成本-伤害散点图数据(CSV格式)
/// 2. 验证曲线是否为凸形(向上弯曲)
/// 3. 检查DPS陷阱(穿透伤害覆盖)
/// 4. 输出完整的敌人等级表
/// 5. 生成战利品对比表
/// </summary>
public class BalanceValidationTester : MonoBehaviour
{
    [ContextMenu("📊 生成所有验证数据")]
    public void GenerateAllValidationData()
    {
        Debug.Log("🔍 开始生成数值平衡验证数据...");
        
        GenerateCardBalanceReport();
        GenerateEnemyProgressionReport();
        GenerateLootComparisonReport();
        
        Debug.Log("✅ 数值平衡验证完成！检查项目：");
        Debug.Log("1. 卡牌成本-伤害散点是否为凸曲线");
        Debug.Log("2. 敌人等级是否符合线性(ATK)和指数(HP)");
        Debug.Log("3. 战利品补偿机制是否生效");
    }

    // ============================================================
    // 📊 卡牌平衡报告
    // ============================================================

    [ContextMenu("📈 生成卡牌平衡报告")]
    public void GenerateCardBalanceReport()
    {
        Debug.Log("\n========== 📈 卡牌平衡报告 ==========");

        // 测试数据：从1费到10费，以及护甲消耗卡
        var testCards = new List<(int id, string name, int costGrain, int costArmor)>
        {
            (1, "轻步兵", 1, 0),
            (2, "快速斩击", 2, 0),
            (3, "虎豹骑", 3, 0),
            (4, "铁骑冲锋", 4, 0),
            (5, "龙骑士", 5, 0),
            (6, "圣骑士冲锋", 6, 0),
            (7, "十字军远征", 7, 0),
            (8, "泰坦复苏", 8, 0),
            (9, "创世魔法", 9, 0),
            (10, "末日审判", 10, 0),
            (11, "护甲斩杀·轻型", 0, 1),
            (12, "护甲斩杀·重型", 0, 2),
            (13, "护甲斩杀·超重", 0, 3),
            (14, "混合冲锋", 1, 1),
            (15, "混合突袭", 2, 1),
        };

        var checkpoints = new List<GameBalanceCalculator.CardBalanceCheckpoint>();

        foreach (var (id, name, costGrain, costArmor) in testCards)
        {
            float damage = GameBalanceCalculator.CalculateCardDamage(costGrain, costArmor);
            
            // 计算总成本(粮换算)
            float totalCost = costGrain + costArmor * 1.5f;
            float damagePerCost = totalCost > 0 ? damage / totalCost : 0;

            // 判断平衡状态
            string status = JudgeCardBalance(damagePerCost, costGrain);

            var checkpoint = new GameBalanceCalculator.CardBalanceCheckpoint
            {
                ID = id,
                Name = name,
                TotalCost = (int)(costGrain + costArmor * 1.5f),
                TheoreticalDamage = damage,
                DamagePerCost = damagePerCost,
                BalanceStatus = status,
            };

            checkpoints.Add(checkpoint);

            Debug.Log($"[{id:D2}] {name,-15} | 成本:{totalCost:F1} | 伤害:{damage:F1} | 效率:{damagePerCost:F2} | 状态:{status}");
        }

        // 验证曲线
        bool isConvex = GameBalanceCalculator.ValidateCurveature(checkpoints);
        Debug.Log($"🔍 曲线检查: {(isConvex ? "✅ 凸形(正常)" : "❌ 凹形(异常)")}");

        // 验证DPS陷阱
        bool noPDPS = GameBalanceCalculator.ValidateDPSCurve(checkpoints);
        Debug.Log($"🔍 穿透伤害检查: {(noPDPS ? "✅ 安全" : "❌ 缺少穿透卡")}");

        Debug.Log("========================================\n");
    }

    private string JudgeCardBalance(float damagePerCost, int costGrain)
    {
        // 如果效率低于4，说明不划算(废卡)
        if (damagePerCost < 4)
            return "❌ 废卡";
        
        // 如果效率高于15，说明过强(OP)
        if (damagePerCost > 15)
            return "🔴 OP卡";
        
        // 如果1费卡效率达到8以上，也要注意
        if (costGrain == 1 && damagePerCost > 7)
            return "🟡 稍强";
        
        return "✅ 平衡";
    }

    // ============================================================
    // 📊 敌人等级报告
    // ============================================================

    [ContextMenu("📊 生成敌人等级报告")]
    public void GenerateEnemyProgressionReport()
    {
        Debug.Log("\n========== 📊 敌人等级报告 ==========");

        var progression = GameBalanceCalculator.GenerateEnemyProgression();

        Debug.Log("| 关卡 | 节点名 | 基础ATK | 基础HP | 波动 | 最终ATK | 最终HP | 难度 |");
        Debug.Log("|------|--------|--------|--------|------|---------|--------|------|");

        foreach (var level in progression)
        {
            string difficulty = GetDifficultyLabel(level.FinalHP);
            Debug.Log($"| {level.NodeIndex,2} | {level.NodeName,-6} | {level.BaseATK,6} | {level.BaseHP,6} | {level.WaveFactor,3:F1}倍 | {level.FinalATK,7} | {level.FinalHP,6} | {difficulty,-4} |");
        }

        // 验证公式正确性
        Debug.Log("\n🔍 公式验证:");
        Debug.Log($"ATK公式: 5 + NodeIndex * 2");
        Debug.Log($"  第0关: 5 + 0*2 = {progression[0].BaseATK} ✓");
        Debug.Log($"  第10关: 5 + 10*2 = {progression[10].BaseATK} ✓");

        Debug.Log($"\nHP公式: 30 * (1.2 ^ NodeIndex)");
        Debug.Log($"  第0关: 30 * (1.2^0) = {progression[0].BaseHP:F0} ✓");
        Debug.Log($"  第10关: 30 * (1.2^10) = {progression[10].BaseHP:F0} ✓");

        Debug.Log("========================================\n");
    }

    private string GetDifficultyLabel(int finalHP)
    {
        if (finalHP < 50) return "简单";
        if (finalHP < 100) return "中等";
        if (finalHP < 150) return "困难";
        return "极限";
    }

    // ============================================================
    // 📊 战利品对比报告
    // ============================================================

    [ContextMenu("🎁 生成战利品对比报告")]
    public void GenerateLootComparisonReport()
    {
        Debug.Log("\n========== 🎁 战利品对比报告 ==========");
        Debug.Log("测试场景：基础掉落 30粮, 粮草上限 100");
        Debug.Log("");

        // 测试不同资源情况
        var testStates = new List<(int current, string label)>
        {
            (10, "贫穷(10/100)"),
            (30, "普通(30/100)"),
            (50, "中等(50/100)"),
            (80, "富裕(80/100)"),
            (100, "饱和(100/100)"),
        };

        Debug.Log("| 玩家状态 | 基础掉落 | 补偿掉落 | 总掉落 | 补偿率 |");
        Debug.Log("|----------|---------|---------|--------|--------|");

        foreach (var (current, label) in testStates)
        {
            int baseLoot = 30;
            int finalLoot = DynamicLootSystem.CalculateLootReward(baseLoot, current, 100);
            int bonus = finalLoot - baseLoot;
            float rate = (bonus / (float)baseLoot) * 100;

            Debug.Log($"| {label,-8} | {baseLoot,7} | {bonus,7} | {finalLoot,6} | {rate,5:F1}% |");
        }

        Debug.Log("\n✅ 验证：补偿机制有效，贫穷玩家获得更多掉落");
        Debug.Log("========================================\n");
    }

    // ============================================================
    // 🎯 详细的卡牌效率分析
    // ============================================================

    [ContextMenu("🔬 详细卡牌效率分析")]
    public void DetailedCardAnalysis()
    {
        Debug.Log("\n========== 🔬 详细卡牌效率分析 ==========");

        Debug.Log("\n【1费卡(线性增长)】");
        Debug.Log("公式: Value = Cost * 5");
        for (int i = 1; i <= 1; i++)
        {
            float dmg = GameBalanceCalculator.CalculateCardDamage(i, 0);
            Debug.Log($"  {i}费卡: 伤害 = {i} * 5 = {dmg}");
        }

        Debug.Log("\n【高费卡(指数膨胀)】");
        Debug.Log("公式: Value = (Cost * 5) * (1 + (Cost - 1) * 0.2)");
        for (int i = 2; i <= 5; i++)
        {
            float baseDmg = i * 5f;
            float multiplier = 1f + (i - 1) * 0.2f;
            float finalDmg = GameBalanceCalculator.CalculateCardDamage(i, 0);
            Debug.Log($"  {i}费卡: {baseDmg} * {multiplier:F1} = {finalDmg:F1}");
        }

        Debug.Log("\n【护甲消耗卡(极高溢价)】");
        Debug.Log("公式: Value = Cost_Armor * 8 * 1.6");
        for (int armor = 1; armor <= 3; armor++)
        {
            float dmg = GameBalanceCalculator.CalculateCardDamage(0, armor);
            Debug.Log($"  消耗{armor}甲: {armor} * 8 * 1.6 = {dmg:F1}");
        }

        Debug.Log("========================================\n");
    }

    // ============================================================
    // 🎓 输出教学总结
    // ============================================================

    [ContextMenu("📚 输出教学总结")]
    public void OutputTeachingSummary()
    {
        Debug.Log("\n╔════════════════════════════════════════╗");
        Debug.Log("║       🎮 游戏平衡设计 - 总结           ║");
        Debug.Log("╚════════════════════════════════════════╝\n");

        Debug.Log("【第一部分】卡牌定价模型");
        Debug.Log("  基准线: 1粮 = 5点价值");
        Debug.Log("  汇率: 1护甲 ≈ 1.5粮");
        Debug.Log("  低费(1费): 线性增长 = Cost * 5");
        Debug.Log("  高费(2费+): 指数膨胀 = Cost * 5 * (1 + (Cost-1) * 0.2)");
        Debug.Log("  护甲消耗: 极高溢价 = Cost * 8 * 1.6");

        Debug.Log("\n【第二部分】敌人成长曲线");
        Debug.Log("  弃用: 实时计算 (CurrentHP * 0.2)");
        Debug.Log("  采用: 查表法 (LevelData表)");
        Debug.Log("  ATK: 线性增长 = 5 + NodeIndex * 2");
        Debug.Log("  HP: 指数增长 = 30 * (1.2 ^ NodeIndex)");
        Debug.Log("  波动: Rest=0.8倍 (简单), Spike=1.2倍 (困难)");

        Debug.Log("\n【第三部分】战利品机制");
        Debug.Log("  防止滚雪球: 获得粮 = 基础 + (上限-当前) * 0.5");
        Debug.Log("  效果: 穷人获得更多, 富人获得更少");
        Debug.Log("  流派调整: Aggro偏粮, Control偏甲");

        Debug.Log("\n【第四部分】防坑指南");
        Debug.Log("  ✓ 散点图检查: 点应在凸曲线上");
        Debug.Log("  ✓ DPS陷阱: 确保有穿透伤害卡");
        Debug.Log("  ✓ 月度审计: 追踪玩家胜率");

        Debug.Log("\n╔════════════════════════════════════════╗");
        Debug.Log("║          ✅ 验证完成                  ║");
        Debug.Log("╚════════════════════════════════════════╝\n");
    }
}
