using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // 单例模式：让其他脚本能通过 ResourceManager.Instance 找到我
    public static ResourceManager Instance { get; private set; }

    [Header("核心资源 (Core Resources)")]
    public int Belief;   // 101 信念
    public int Grain;    // 102 粮食
    public int Water;    // 103 储水
    public int Troops;   // 104 兵力
    public int Money;    // 105 财货
    public int Horses;   // 106 马匹
    public int Armor;    // 107 披甲
    [Header("配置 (Config)")]
    public int MaxBelief = 100;

    private void Awake()
    {
        // 保证全局唯一
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        // 初始化一些测试数据
        Belief = 80;
        Grain = 200;
        Water = 100;
        Troops = 500;
        Money = 100;
        Horses = 20;
        Armor = 30;
        
        Debug.Log("【资源系统】初始化完成！");
    }

    /// <summary>
    /// 通用的资源变更方法
    /// </summary>
    /// <param name="id">资源ID (101-105)</param>
    /// <param name="amount">变化量 (可以是负数)</param>
    public void ChangeResource(int id, int amount)
    {
        string resName = "";

        switch (id)
        {
            case 101: // 信念
                Belief += amount;
                // 限制信念不能超过上限，也不能低于0
                Belief = Mathf.Clamp(Belief, 0, MaxBelief); 
                resName = "信念";
                break;

            case 102: // 粮食
                Grain += amount;
                if (Grain < 0) Grain = 0;
                resName = "粮食";
                break;

            case 103: // 储水
                Water += amount;
                if (Water < 0) Water = 0;
                resName = "储水";
                break;

            case 104: // 兵力
                Troops += amount;
                if (Troops < 0) Troops = 0;
                resName = "兵力";
                break;

            case 105: // 财货
                Money += amount;
                if (Money < 0) Money = 0;
                resName = "财货";
                break;
            case 106: //马匹
                Horses += amount;
                if (Horses < 0) Horses = 0;
                resName = "马匹";
                break;
            case 107: //披甲
                Armor += amount;
                if (Armor < 0) Armor = 0;
                resName = "披甲";
                break;
            default:
                Debug.LogWarning($"【资源系统】未知的资源ID: {id}");
                return;
        }

        // 在控制台打印变化，方便调试
        string sign = amount >= 0 ? "+" : "";
        Debug.Log($"【资源变动】{resName} {sign}{amount} (当前: {GetResourceValue(id)})");
        
        // TODO: 这里未来会通知 UI 更新
    }

    // 辅助方法：获取当前资源数值
    public int GetResourceValue(int id)
    {
        switch (id)
        {
            case 101: return Belief;
            case 102: return Grain;
            case 103: return Water;
            case 104: return Troops;
            case 105: return Money;
            case 106: return Horses;
            case 107: return Armor;

            default: return 0;
        }
    }
}