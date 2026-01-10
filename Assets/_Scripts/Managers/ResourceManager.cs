using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public event Action OnResourcesChanged;
    // 只有一个死亡事件了，因为资源合并了
    public event Action<string> OnGameEndingTriggered; 

    [Header("--- 核心铁三角 ---")]
    public int Belief = 100; // 原来的“兵力”和“信念”合并，作为HP
    public int Grain = 20;   // 行动资源
    public int Armor = 5;    // 护甲储备 (修补工具/盾牌库存)

    // 删除：Water, Troops, Money, Horses

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; 
        DontDestroyOnLoad(gameObject);
    }

    public void ResetResources()
    {
        Belief = 100; 
        Grain = 20; 
        Armor = 5; 
        OnResourcesChanged?.Invoke();
    }

    public void ChangeResource(int id, int amount)
    {
        switch (id)
        {
            case 101: // 信念 (HP)
                Belief += amount; 
                if (Belief <= 0) OnGameEndingTriggered?.Invoke("Death_Belief"); 
                break;

            case 102: // 粮草 (Cost)
                {
                    int before = Grain;
                    int after = before + amount;
                    if (after >= 0)
                    {
                        Grain = after;
                    }
                    else
                    {
                        // 粮草不足：将粮草归零，并把超出的负值转移为对信念的伤害
                        int deficit = -after; // 需要扣的信念
                        Grain = 0;
                        Belief -= deficit;
                        if (Belief <= 0) OnGameEndingTriggered?.Invoke("Death_Belief");
                    }
                }
                break;

            case 103: // 盾/甲 (Stock)
                Armor += amount; 
                if (Armor < 0) Armor = 0;
                break;
                
            // 删除其他 case
        }
        OnResourcesChanged?.Invoke();
    }
    public void ForceUpdateUI()
    {
        OnResourcesChanged?.Invoke();
    }
    public string GetResName(int id)
    {
        switch(id)
        {
            case 101: return "信念";
            case 102: return "粮草";
            case 103: return "盾甲";
            default: return "未知";
        }
    }

    public int GetResourceValue(int id)
    {
        switch(id)
        {
            case 101: return Belief;
            case 102: return Grain;
            case 103: return Armor;
            default: return 0;
        }
    }
}