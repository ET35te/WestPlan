using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // 🔥 必须引用，用于 Action

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // --- 🔥 新增：事件广播系统 ---
    // 谁想知道资源变了，就监听这个事件
    public event Action OnResourcesChanged;

    // 谁想知道玩家死没死，就监听这个事件 (string 参数传递死亡原因)
    public event Action<string> OnResourceDepleted;

    [Header("核心资源")]
    public int Belief;
    public int Grain;
    public int Water;
    public int Troops;
    public int Money;
    public int Horses;
    public int Armor;

    [Header("配置")]
    public int MaxBelief = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; // 🔥 必须加！否则被销毁后代码还会往下跑，导致报错
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // 重置方法保持不变
    public void ResetResources()
    {
        Belief = 80;
        Grain = 200;
        Water = 100;
        Troops = 500;
        Money = 100;
        Horses = 20;
        Armor = 30;

        // 重置后，广播一次让 UI 刷新
        OnResourcesChanged?.Invoke();
    }

    public void ChangeResource(int id, int amount)
    {
        switch (id)
        {
            case 101:
                Belief += amount;
                Belief = Mathf.Clamp(Belief, 0, MaxBelief);
                Debug.Log($"资源变动：信念 {Belief}");

                // 💀 死亡判定：不再直接调用 GM，而是喊一声“我死了”
                if (Belief <= 0)
                {
                    Debug.Log("【广播】信念归零事件触发！");
                    OnResourceDepleted?.Invoke("Death_Belief");
                }
                break;

            case 102: Grain = Mathf.Max(0, Grain + amount); break;
            case 103: Water = Mathf.Max(0, Water + amount); break;
            case 104: Troops = Mathf.Max(0, Troops + amount); break;
            case 105: Money = Mathf.Max(0, Money + amount); break;
            case 106: Horses = Mathf.Max(0, Horses + amount); break;
            case 107: Armor = Mathf.Max(0, Armor + amount); break;

            default: Debug.LogWarning($"未知资源ID: {id}"); return;
        }

        // 📢 广播：资源变啦！UI 你们自己看着办！
        // ?.Invoke() 的意思是：如果有人在监听，就执行；没人监听就算了
        OnResourcesChanged?.Invoke();
    }

    // GetResName 和 GetResourceValue 保持不变...
    public string GetResName(int resID) { /*...*/ return "资源"; } // 简写了，请保留你原来的代码
    public int GetResourceValue(int id) { /*...*/ return 0; }      // 简写了，请保留你原来的代码
}