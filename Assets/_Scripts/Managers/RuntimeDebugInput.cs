using UnityEngine;

/// <summary>
/// 🎮 运行时Debug快捷键
/// 在Play模式下按以下快捷键快速测试功能
/// </summary>
public class RuntimeDebugInput : MonoBehaviour
{
    private void Update()
    {
        // 🔑 Shift + D: 打印资源状态
        if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftShift))
        {
            DebugTools.PrintResourceStatus();
        }

        // 🔑 Shift + B: 打印战斗状态
        if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.LeftShift))
        {
            DebugTools.PrintBattleStatus();
        }

        // 🔑 Shift + I: 资源无限
        if (Input.GetKeyDown(KeyCode.I) && Input.GetKey(KeyCode.LeftShift))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatInfiniteResources();
        }

        // 🔑 Shift + K: 秒杀敌人 (OneHitEnemy)
        if (Input.GetKeyDown(KeyCode.K) && Input.GetKey(KeyCode.LeftShift))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatOneHitEnemy();
        }

        // 🔑 Shift + S: 自杀测试
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatSelfDestruct();
        }

        // 🔑 Shift + J: 跳转到事件2005
        if (Input.GetKeyDown(KeyCode.J) && Input.GetKey(KeyCode.LeftShift))
        {
            if (DebugManager.Instance != null)
                DebugManager.Instance.CheatJumpToEvent(2005);
        }

        // 🔑 Shift + W: 快速胜利（敌人秒杀+攻击）
        if (Input.GetKeyDown(KeyCode.W) && Input.GetKey(KeyCode.LeftShift))
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.EnemyUnitCount = 1;
                Debug.Log("⚡ [DEBUG] 快速胜利模式：敌人生命已变为1");
            }
        }

        // 🔑 Shift + T: 测试线性事件系统（新系统快速验证）
        if (Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("🧪 [TEST] 启动线性事件系统测试...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNodeStoryFlow();
            }
        }

        // 🔑 Shift + Q: 跳过剧情面板（直接进入事件）
        if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("⏭️ [DEBUG] 跳过剧情面板...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseStoryPanelAndStartEvents();
            }
        }
    }
}

