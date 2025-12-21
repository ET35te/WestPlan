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

    // --- 2. 新版卡牌数据 (NEW!) ---
    [System.Serializable]
    public class CardData
    {
        public int ID;
        public string Name;
        public int Type;      // 1=兵力卡, 2=兵法卡
        public int Power;     // 兵力值 (1/2/3)
        public int Cost;      // 费用 (预留)
        public string EffectID; // 特殊效果ID (如 "RUSH", "SHIELD")
        public string Description;
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
    public List<CardData> AllCards = new List<CardData>(); // 新卡牌库
    public List<EnemyData> AllEnemies = new List<EnemyData>();
    private void Awake()
    {
        // 1. 单例逻辑 (保持不变)
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject); 
        }
        else 
        {
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
            
            // 2. 🔥 【关键修改】在这里加载数据！不要在 Start 里加载！
            LoadEventTable();
            LoadCardTable();
            LoadEnemyTable();
        }
    }
    private void Start()
    {
        Debug.Log("数据中心就绪");
    }
       
    // ... (LoadEventTable 和 LoadEnemyTable 代码保持不变，此处省略以节省空间) ...
void LoadEventTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable"); // 确保你的CSV文件在这个路径
        if (textAsset == null) { Debug.LogWarning("找不到 Data/EventTable"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEvents.Clear();

        // 从第1行开始（跳过表头）
        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            // 简单校验列数，EventData 字段较多，根据实际CSV列数调整
            if (row.Length < 4) continue; 

            try
            {
                EventData evt = new EventData();
                evt.ID = ParseInt(row[0]);
                evt.IsPeaceful = (ParseInt(row[1]) == 1); // 假设CSV里用1表示和平
                evt.Title = row[2];
                evt.Context = row[3].Replace(";", ","); // 支持分号转逗号

                // 如果你的CSV列数更多，请继续读取：
                if(row.Length > 4) evt.OptA_Text = row[4];
                if(row.Length > 5) evt.OptA_Res1_Txt = row[5];
                if(row.Length > 6) evt.OptA_Res1_Data = row[6];
                if(row.Length > 7) evt.OptA_Res2_Rate = ParseInt(row[7]);
                if(row.Length > 8) evt.OptA_Res2_Txt = row[8];
                if(row.Length > 9) evt.OptA_Res2_Data = row[9];
                
                if(row.Length > 10) evt.OptB_Text = row[10];
                if(row.Length > 11) evt.OptB_Res1_Txt = row[11];
                if(row.Length > 12) evt.OptB_Res1_Data = row[12];
                if(row.Length > 13) evt.OptB_Res2_Rate = ParseInt(row[13]);
                if(row.Length > 14) evt.OptB_Res2_Txt = row[14];
                if(row.Length > 15) evt.OptB_Res2_Data = row[15];

                // 如果有更多字段继续往下写...

                AllEvents.Add(evt);
            }
            catch (Exception e) 
            { 
                Debug.LogError($"事件表行 {i} 解析错误: {e.Message}"); 
            }
        }
        Debug.Log($"【数据】加载了 {AllEvents.Count} 个事件。");
    }
    // --- 新增：加载卡牌表 ---
    void LoadCardTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/CardTable");
        if(textAsset == null) { Debug.LogWarning("找不到 Data/CardTable"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllCards.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 7) continue;

            try
            {
                CardData card = new CardData();
                card.ID = ParseInt(row[0]);
                card.Name = row[1];
                card.Type = ParseInt(row[2]);
                card.Power = ParseInt(row[3]);
                card.Cost = ParseInt(row[4]);
                card.EffectID = row[5];
                card.Description = row[6].Replace(";", ",");
                AllCards.Add(card);
            }
            catch (Exception e) { Debug.LogError($"卡牌表行 {i} 错误: {e.Message}"); }
        }
        Debug.Log($"【数据】加载了 {AllCards.Count} 张战斗卡牌。");
    }
void LoadEnemyTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EnemyTable");
        if (textAsset == null) { Debug.LogWarning("找不到 Data/EnemyTable"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEnemies.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 4) continue;

            try
            {
                EnemyData enemy = new EnemyData();
                enemy.ID = ParseInt(row[0]);
                enemy.Name = row[1];
                enemy.Power = ParseInt(row[2]);
                enemy.Description = row[3].Replace(";", ",");
                
                // 如果有第5列意图模式
                if (row.Length > 4) enemy.Intent_Pattern = row[4];

                AllEnemies.Add(enemy);
            }
            catch (Exception e)
            {
                Debug.LogError($"敌人表行 {i} 解析错误: {e.Message}");
            }
        }
        Debug.Log($"【数据】加载了 {AllEnemies.Count} 个敌人数据。");
    }
    // 辅助方法
    int ParseInt(string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        int.TryParse(str, out int result);
        return result;
    }
    
    public EventData GetRandomEvent() {
        if (AllEvents.Count == 0) return null;
        return AllEvents[UnityEngine.Random.Range(0, AllEvents.Count)];
    }
    
    public EnemyData GetEnemyByID(int id) {
        return AllEnemies.Find(e => e.ID == id);
    }
    
    // 获取初始卡组 (测试用：返回12张固定卡的列表)
    public List<CardData> GetStarterDeck() {
        // 这里简单返回前12张卡，后续可根据ID筛选
        List<CardData> deck = new List<CardData>();
        foreach(var c in AllCards) deck.Add(c); 
        // 补齐或截断到12张
        while(deck.Count < 12 && AllCards.Count > 0) deck.Add(AllCards[0]);
        if(deck.Count > 12) deck = deck.GetRange(0, 12);
        return deck;
    }
}