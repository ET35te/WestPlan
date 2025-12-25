using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public event Action OnResourcesChanged;
    public event Action<string> OnResourceDepleted;

    public int Belief = 80;
    public int Grain = 200;
    public int Water = 100;
    public int Troops = 500;
    public int Money = 100;
    public int Horses = 20;
    public int Armor = 30;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        else { Instance = this; DontDestroyOnLoad(this.gameObject); }
    }

    public void ResetResources()
    {
        Belief = 80; Grain = 200; Water = 100; Troops = 500; Money = 100; Horses = 20; Armor = 30;
        OnResourcesChanged?.Invoke();
    }

    public void ChangeResource(int id, int amount)
    {
        // ... (保留你原来的 switch case 逻辑) ...
        switch (id)
        {
            case 101: Belief += amount; if (Belief <= 0) OnResourceDepleted?.Invoke("Death_Belief"); break;
            case 102: Grain += amount; break;
            case 107: Armor += amount; break;
                // ... 其他资源 ...
        }
        OnResourcesChanged?.Invoke();
    }

    // 🔥 新增：给 GM 调用的强制刷新
    public void ForceUpdateUI()
    {
        OnResourcesChanged?.Invoke();
    }

    public string GetResName(int id) { return "资源"; } // 简写
    public int GetResourceValue(int id) { return 0; } // 简写
}