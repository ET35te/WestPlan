using UnityEngine;

/// <summary>
/// 🔥 一键调试工具 - 在Scene中手动测试
/// 使用方法：在任何地方调用这些静态方法
/// </summary>
public class DebugTools
{
    /// <summary>
    /// [快速测试] 立即触发战斗胜利
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    public static void QuickTestBattleVictory()
    {
        // Debug.Log("📌 按 Ctrl+V 可以快速胜利（需要在Input Manager配置）");
    }

    /// <summary>
    /// [快速测试] 查看当前资源状态
    /// </summary>
    public static void PrintResourceStatus()
    {
        if (ResourceManager.Instance != null)
        {
            string status = $"📊 资源状态\n" +
                           $"  信念: {ResourceManager.Instance.Belief}\n" +
                           $"  粮草: {ResourceManager.Instance.Grain}\n" +
                           $"  护甲: {ResourceManager.Instance.Armor}";
            Debug.Log(status);
        }
    }

    /// <summary>
    /// [快速测试] 查看当前战斗状态
    /// </summary>
    public static void PrintBattleStatus()
    {
        if (BattleManager.Instance != null)
        {
            string status = $"⚔️ 战斗状态\n" +
                           $"  玩家信念: {BattleManager.Instance.PlayerUnitCount}\n" +
                           $"  玩家粮草: {BattleManager.Instance.PlayerFood}\n" +
                           $"  玩家护甲: {BattleManager.Instance.PlayerArmor}\n" +
                           $"  敌人兵力: {BattleManager.Instance.EnemyUnitCount}";
            Debug.Log(status);
        }
    }
}
