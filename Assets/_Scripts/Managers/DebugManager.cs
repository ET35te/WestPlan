using UnityEngine;

/// <summary>
/// 🔥 Debug 管理器 - 4个快速作弊功能
/// </summary>
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    // =========================================================
    // 🎮 4个作弊功能
    // =========================================================

    /// <summary>
    /// 作弊功能 1: 资源无限
    /// </summary>
    public void CheatInfiniteResources()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Belief = 999;
            ResourceManager.Instance.Grain = 999;
            ResourceManager.Instance.Armor = 999;
            ResourceManager.Instance.ForceUpdateUI();
            Debug.Log("💰 [DEBUG] 资源已设置为无限！Belief=999, Grain=999, Armor=999");
        }
    }

    /// <summary>
    /// 作弊功能 2: 秒杀敌人
    /// </summary>
    public void CheatOneHitEnemy()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.EnemyUnitCount = 1;
            Debug.Log("⚔️ [DEBUG] 敌人生命已设置为 1，一击秒杀！");
        }
        else
        {
            Debug.LogWarning("⚠️ BattleManager 未初始化，请在战斗中使用此功能");
        }
    }

    /// <summary>
    /// 作弊功能 3: 自杀测试
    /// </summary>
    public void CheatSelfDestruct()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Belief = 1;
            ResourceManager.Instance.ForceUpdateUI();
            Debug.Log("💀 [DEBUG] 信念已设置为 1，触发死亡判定！");
        }
    }

    /// <summary>
    /// 作弊功能 4: 强制跳关
    /// </summary>
    public void CheatJumpToEvent(int eventID)
    {
        if (GameManager.Instance != null && DataManager.Instance != null)
        {
            // ✅ 新系统：使用v2事件系统
            if (UIManager.Instance != null && GameManager.Instance != null)
            {
                // 直接跳转到指定事件ID
                GameManager.Instance.ShowEventByID_v2(eventID);
                Debug.Log($"🚀 [DEBUG] 已强制跳转到事件 ID: {eventID}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到 ID 为 {eventID} 的事件！");
            }
        }
    }
}
