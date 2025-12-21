using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("核心资源")]
    public int Belief;   // 101
    public int Grain;    // 102
    public int Water;    // 103
    public int Troops;   // 104
    public int Money;    // 105
    public int Horses;   // 106
    public int Armor;    // 107

    [Header("配置")]
    public int MaxBelief = 100;
    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(this.gameObject);
        }
        else 
        { 
            Instance = this; 
            DontDestroyOnLoad(this.gameObject); // 👈 恢复这行代码！一定要有！
        } 
    }
    private void Start()
    {
        // --- 修复点：不要在这里强制重置 ---
        // 只有当这是“第一次运行”且“没有存档加载”时，才需要初始化。
        // 但为了简单，我们可以把初始化权交给 GameManager.StartNewGame()。
        // 所以这里留空，或者只做防空判断。
        
        // 如果数值全是0（说明没初始化），给个保底值方便测试
        if (Belief == 0 && Grain == 0)
        {
            ResetResources();
        }
        
        Debug.Log("【资源系统】就绪。");
    }

    // 新增：重置资源方法 (供 GameManager 开始新游戏时调用)
    public void ResetResources()
    {
        Belief = 80;
        Grain = 200;
        Water = 100;
        Troops = 500;
        Money = 100;
        Horses = 20;
        Armor = 30;
    }

    public void ChangeResource(int id, int amount)
    {
        string resName = "";

        switch (id)
        {
            case 101: 
                Belief += amount;
                Belief = Mathf.Clamp(Belief, 0, MaxBelief); 
                resName = "信念";
                // --- 修复点：拼写修正 ---
                Debug.Log($"检测当前信念：{Belief}");
                if (Belief <= 0) 
                {
                    Debug.Log("【检测】信念归零，触发死亡！");
                    // 确保 GameManager 存在且方法名正确
                    if(GameManager.Instance != null) GameManager.Instance.TriggerEnding("Death_Belief");
                }
                break;

            case 102: Grain = Mathf.Max(0, Grain + amount); resName = "粮食"; break;
            case 103: Water = Mathf.Max(0, Water + amount); resName = "储水"; break;
            case 104: Troops = Mathf.Max(0, Troops + amount); resName = "兵力"; break;
            case 105: Money = Mathf.Max(0, Money + amount); resName = "财货"; break;
            case 106: Horses = Mathf.Max(0, Horses + amount); resName = "马匹"; break;
            case 107: Armor = Mathf.Max(0, Armor + amount); resName = "披甲"; break;
            
            default: Debug.LogWarning($"未知资源ID: {id}"); return;
        }

        // 刷新 UI
        if(UIManager.Instance != null) UIManager.Instance.UpdateResourceDisplay();
    }
// 把这段代码加到 ResourceManager 类的大括号里面
    public string GetResName(int resID)
    {
        switch (resID)
        {
            case 101: return "黄金";
            case 102: return "粮草";
            case 103: return "木材";
            case 104: return "兵力";
            case 105: return "马匹";
            default: return "资源";
        }
    }
    public int GetResourceValue(int id)
    {
        switch (id) {
            case 101: return Belief; case 102: return Grain;
            case 103: return Water; case 104: return Troops;
            case 105: return Money; case 106: return Horses;
            case 107: return Armor; default: return 0;
        }
    }
}