using UnityEngine;
using System.Collections.Generic;
using System;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // --- 1. 事件数据 (保持不变) ---
    [System.Serializable]
    public class EventData
    {
        public int ID;
        public bool IsPeaceful;
        public string Title;
        public string Context;
        public string OptA_Text; public string OptA_Res1_Txt; public string OptA_Res1_Data;
        public int OptA_Res2_Rate; public string OptA_Res2_Txt; public string OptA_Res2_Data;
        public string OptB_Text; public string OptB_Res1_Txt; public string OptB_Res1_Data;
        public int OptB_Res2_Rate; public string OptB_Res2_Txt; public string OptB_Res2_Data;
        public string Effect_Type; public string OptB_Condition;
    }

    // --- 2. 新卡牌数据结构 ---
    [System.Serializable]
    public class CardData
    {
        public int ID;
        public string Name;
        
        public CardType Type;         // 枚举：Unit / Strategy
        public CardSubType SubType;   // 枚举：Auxiliary / Regular / Elite / Tactic
        
        public int Cost_Food;         // 粮耗
        public int Cost_Armor;        // 甲耗
        
        public int Power;             // 战力 (策略卡为0)
        
        public string Effect_ID;      // 效果逻辑ID
        public int Effect_Val;        // 效果数值
        
        public string Description;
    }

    // 枚举定义
    public enum CardType 
    { 
        Unit, 
        Strategy 
    }

    public enum CardSubType 
    { 
        Auxiliary, // 辅兵
        Regular,   // 正规
        Elite,     // 精锐
        Tactic     // 战术(策略)
    }

    // --- 3. 敌人数据 (保持不变) ---
    [System.Serializable]
    public class EnemyData
    {
        public int ID;
        public string Name;
        public int Power;
        public string Description;
        public string Intent_Pattern;
    }

    public List<EventData> AllEvents = new List<EventData>();
    public List<CardData> AllCards = new List<CardData>();
    public List<EnemyData> AllEnemies = new List<EnemyData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject); 
        }
        else 
        {
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
            
            LoadEventTable();
            LoadCardTable(); // 🔥 重点修复了这个方法
            LoadEnemyTable();
        }
    }

    private void Start()
    {
        Debug.Log("数据中心就绪");
    }

    // ... (LoadEventTable 代码保持不变，略) ...
    void LoadEventTable()
    {
        // ... 请保持你原来的 LoadEventTable 代码 ...
        // 为了防报错，这里放一个空的实现，你需要把你原来的代码贴回来
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable"); 
        if (textAsset == null) return;
        // ... (你的原有逻辑)
    }

    // 🔥🔥🔥【重点修复】加载卡牌表 🔥🔥🔥
    void LoadCardTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/CardTable");
        if(textAsset == null) { Debug.LogWarning("找不到 Data/CardTable"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllCards.Clear();

        // 假设 CSV 列顺序如下 (共10列):
        // 0:ID, 1:Name, 2:Type, 3:SubType, 4:Cost_Food, 5:Cost_Armor, 6:Power, 7:Effect_ID, 8:Effect_Val, 9:Desc
        
        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            
            // 简单检查列数，防止越界
            if (row.Length < 10) 
            {
                // 如果你的描述里有逗号导致 split 多了，可以不管；但如果少了肯定不行
                if (row.Length < 9) continue; 
            }

            try
            {
                CardData card = new CardData();
                
                // 基础解析
                card.ID = ParseInt(row[0]);
                card.Name = row[1];

                // --- 枚举解析 (将字符串转为 Enum) ---
                // 假设 CSV 里填的是 "Unit" 或 "Strategy"
                card.Type = ParseEnum<CardType>(row[2]); 
                // 假设 CSV 里填的是 "Regular" 或 "Tactic"
                card.SubType = ParseEnum<CardSubType>(row[3]);

                // --- 数值解析 ---
                card.Cost_Food = ParseInt(row[4]);
                card.Cost_Armor = ParseInt(row[5]);
                card.Power = ParseInt(row[6]);

                // --- 效果与描述 ---
                card.Effect_ID = row[7];
                card.Effect_Val = ParseInt(row[8]);
                
                // 防止描述里有逗号被截断，这里取最后一列 (如果有逗号问题需特殊处理，这里先简单处理)
                card.Description = row[9].Replace(";", ","); 

                AllCards.Add(card);
            }
            catch (Exception e) 
            { 
                Debug.LogError($"卡牌表行 {i} 解析错误: {e.Message} | 数据: {lines[i]}"); 
            }
        }
        Debug.Log($"【数据】加载了 {AllCards.Count} 张战斗卡牌。");
    }

    // ... (LoadEnemyTable 代码保持不变，略) ...
    void LoadEnemyTable()
    {
        // ... 请保持你原来的 LoadEnemyTable 代码 ...
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EnemyTable"); 
        if (textAsset == null) return;
        // ...
    }

    // --- 辅助方法 ---

    int ParseInt(string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        int.TryParse(str, out int result);
        return result;
    }

    // 🔥 新增：通用的枚举解析方法
    // 用法：ParseEnum<CardType>("Unit") -> 返回 CardType.Unit
    T ParseEnum<T>(string str)
    {
        try
        {
            return (T)Enum.Parse(typeof(T), str, true); // true 表示忽略大小写
        }
        catch
        {
            Debug.LogWarning($"枚举解析失败: {str}, 将使用默认值");
            return default(T);
        }
    }
    public EventData GetRandomEvent()
    {
        if (AllEvents.Count == 0) return null;
        return AllEvents[UnityEngine.Random.Range(0, AllEvents.Count)];
     }
     public EnemyData GetEnemyByID(int id)
    {
        return AllEnemies.Find(e => e.ID == id);
    }
    // ... (GetRandomEvent 等方法保持不变) ...
    public List<CardData> GetStarterDeck() {
        List<CardData> deck = new List<CardData>();
        // 简单改一下，防止越界
        int count = Mathf.Min(AllCards.Count, 12);
        for(int i = 0; i < count; i++) deck.Add(AllCards[i]);
        return deck;
    }

}