using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 🎭 敌人行为状态机 (Enemy Behavior FSM)
/// 管理敌人在各个战斗阶段的行为决策与伤害计算
/// </summary>
public class EnemyStateMachine
{
    public enum State { NORMAL, CHARGING, POWER_STRIKE, COUNTERATTACK, WEAKENED, DESPERATE }

    private State _currentState = State.NORMAL;
    public State CurrentState => _currentState;

    private int _chargeCounter = 0;          // 蓄力计数
    private int _weaknessCounter = 0;        // 虚弱状态持续回合数
    private int _consecutiveCardCount = 0;   // 玩家本回合出牌张数

    private int _baseEnemyPower = 10;        // 敌人基础战力

    // 参数配置（可在编辑器调整）
    public float CriticalHPThreshold = 0.3f; // 触发蓄力的血量阈值（30%）
    public float DespairHPThreshold = 0.1f;  // 触发绝望的血量阈值（10%）
    public int ConsecutiveCardThreshold = 2; // 触发反制的连续出牌张数
    public int WeaknessDefaultDuration = 2;  // 虚弱持续回合数
    public float ChargePowerMultiplier = 2f; // 蓄力后伤害倍数

    public EnemyStateMachine(int basePower)
    {
        _baseEnemyPower = basePower;
    }

    /// <summary>
    /// 更新敌人状态（应在敌人回合前调用）
    /// </summary>
    public void UpdateState(float currentHPPercent, int playerDamageLastTurn)
    {
        // 检查虚弱计数
        if (_weaknessCounter > 0)
        {
            _weaknessCounter--;
            if (_weaknessCounter == 0 && _currentState == State.WEAKENED)
            {
                _currentState = State.NORMAL;
                Debug.Log("✅ 敌军虚弱状态解除");
            }
        }

        // 检查蓄力完成
        if (_currentState == State.CHARGING)
        {
            _chargeCounter++;
            if (_chargeCounter >= 1)
            {
                _currentState = State.POWER_STRIKE;
                Debug.Log("⚡ 敌军蓄力完毕，准备发动强力一击！");
            }
        }

        // 检查蓄力被打断（玩家造成过多伤害）
        if (_currentState == State.CHARGING && playerDamageLastTurn >= 50)
        {
            _currentState = State.NORMAL;
            _chargeCounter = 0;
            Debug.Log("💥 敌军蓄力被打断！");
        }

        // 检查生命危急（优先级最高）
        if (currentHPPercent <= DespairHPThreshold && _currentState != State.DESPERATE)
        {
            _currentState = State.DESPERATE;
            Debug.Log("🔥 敌军陷入绝望，发起疯狂进攻！");
        }

        // 检查血量下降，触发蓄力
        if (currentHPPercent <= CriticalHPThreshold && _currentState == State.NORMAL)
        {
            _currentState = State.CHARGING;
            _chargeCounter = 0;
            Debug.Log("⚠️ 敌军血量下降，准备蓄力反击...");
        }
    }

    /// <summary>
    /// 玩家出牌时调用，用于触发反制逻辑
    /// </summary>
    public void OnPlayerPlayCard()
    {
        _consecutiveCardCount++;
        if (_consecutiveCardCount >= ConsecutiveCardThreshold && _currentState == State.NORMAL)
        {
            _currentState = State.COUNTERATTACK;
            Debug.Log("🔄 敌军感知到威胁，准备反制！");
        }
    }

    /// <summary>
    /// 重置玩家出牌计数（玩家回合结束时调用）
    /// </summary>
    public void ResetConsecutiveCount()
    {
        _consecutiveCardCount = 0;
    }

    /// <summary>
    /// 计算敌人伤害
    /// </summary>
    public int CalculateDamage(int playerArmor)
    {
        float baseDmg = Mathf.CeilToInt(_baseEnemyPower * 0.2f);

        float multiplier = _currentState switch
        {
            State.NORMAL => 1f,
            State.CHARGING => 0f,           // 不攻击
            State.POWER_STRIKE => ChargePowerMultiplier,
            State.COUNTERATTACK => 1.5f,
            State.WEAKENED => 0.5f,
            State.DESPERATE => 1.3f,
            _ => 1f
        };

        // 虚弱状态下护甲也减弱
        float armorReduction = _currentState == State.DESPERATE ? 0.5f : 1f;

        int totalDmg = Mathf.Max(0, Mathf.FloorToInt(baseDmg * multiplier) - Mathf.FloorToInt(playerArmor * armorReduction));
        return totalDmg;
    }

    /// <summary>
    /// 获取敌人意图提示文本
    /// </summary>
    public string GetIntentText(int expectedDamage)
    {
        return _currentState switch
        {
            State.NORMAL => $"⚔️ 敌军意图: 普通攻击\n预计伤害: {expectedDamage}",
            State.CHARGING => "⚠️ 敌军正在蓄力...\n下回合发动强力一击！",
            State.POWER_STRIKE => $"💥 敌军发动强力一击！\n预计伤害: {expectedDamage}",
            State.COUNTERATTACK => $"🔄 敌军反制攻击！\n预计伤害: {expectedDamage}",
            State.WEAKENED => $"😰 敌军虚弱中\n预计伤害: {expectedDamage}",
            State.DESPERATE => $"🔥 敌军拼死一搏！\n预计伤害: {expectedDamage}",
            _ => "敌军思考中..."
        };
    }

    /// <summary>
    /// 应用虚弱效果
    /// </summary>
    public void ApplyWeakness()
    {
        _currentState = State.WEAKENED;
        _weaknessCounter = WeaknessDefaultDuration;
        Debug.Log($"😰 敌军陷入虚弱状态，持续 {WeaknessDefaultDuration} 回合");
    }

    /// <summary>
    /// 获取状态持续的回合数（用于UI显示）
    /// </summary>
    public int GetStatusDurationRemaining()
    {
        return _currentState == State.WEAKENED ? _weaknessCounter : 0;
    }

    /// <summary>
    /// 调试用：打印当前状态
    /// </summary>
    public void DebugPrintState()
    {
        Debug.Log($"[FSM] 当前状态: {_currentState} | 蓄力计数: {_chargeCounter} | 虚弱计数: {_weaknessCounter} | 连续出牌: {_consecutiveCardCount}");
    }
}
