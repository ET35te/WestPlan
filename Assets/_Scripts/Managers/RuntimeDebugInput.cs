using UnityEngine;

/// <summary>
/// 🎮 运行时Debug快捷键
/// 在Play模式下按以下快捷键快速测试功能
/// </summary>
public class RuntimeDebugInput : MonoBehaviour
{
    private void Update()
    {
        // 🔑 Ctrl + D: 打印资源状态
        if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
        {
            DebugTools.PrintResourceStatus();
        }

        // 🔑 Ctrl + B: 打印战斗状态
        if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.LeftControl))
        {
            DebugTools.PrintBattleStatus();
        }

        // 🔑 Ctrl + I: 资源无限
        if (Input.GetKeyDown(KeyCode.I) && Input.GetKey(KeyCode.LeftControl))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatInfiniteResources();
        }

        // 🔑 Ctrl + K: 秒杀敌人
        if (Input.GetKeyDown(KeyCode.K) && Input.GetKey(KeyCode.LeftControl))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatOneHitEnemy();
        }

        // 🔑 Ctrl + S: 自杀测试
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatSelfDestruct();
        }

        // 🔑 Ctrl + J: 跳转到事件2005
        if (Input.GetKeyDown(KeyCode.J) && Input.GetKey(KeyCode.LeftControl))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatJumpToEvent(2005);
        }

        // 🔑 Ctrl + W: 快速胜利（敌人秒杀+攻击）
        if (Input.GetKeyDown(KeyCode.W) && Input.GetKey(KeyCode.LeftControl))
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.EnemyUnitCount = 1;
                Debug.Log("⚡ [DEBUG] 快速胜利模式：敌人生命已变为1");
            }
        }
    }
}
