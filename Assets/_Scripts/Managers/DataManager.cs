using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // 🔥 新增：加载完成标记（解决 GM 死等的问题）
    public bool IsReady { get; private set; } = false;

    // --- 数据类定义 ---
    
    // 旧版本（兼容保留）
    [System.Serializable]
    public class EventData
    {
        public int ID; public bool IsPeaceful; public string Title; public string Context;
        public string OptA_Text; public string OptA_Res1_Txt; public string OptA_Res1_Data; public int OptA_Res2_Rate; public string OptA_Res2_Txt; public string OptA_Res2_Data;
        public string OptB_Text; public string OptB_Res1_Txt; public string OptB_Res1_Data; public int OptB_Res2_Rate; public string OptB_Res2_Txt; public string OptB_Res2_Data;
        public string Effect_Type; public string OptB_Condition;
    }
    
    // 新版本（支持线性分支）
    [System.Serializable]
    public class EventData_v2
    {
        public int ID;
        public bool IsPeaceful;
        public string Title;
        public string Context;
        
        // 选项A：文本、结果、跳转、条件
        public string OptA_Text;
        public string OptA_Result_Txt;
        public string OptA_Result_Data;
        public int NextID_A;  // -1表示节点结束
        public string Condition_A;
        
        // 选项B
        public string OptB_Text;
        public string OptB_Result_Txt;
        public string OptB_Result_Data;
        public int NextID_B;
        public string Condition_B;
        
        // 特效（保留兼容）
        public string Effect_Type;
    }
    
    // 节点剧情面板数据
    [System.Serializable]
    public class StoryPanelData
    {
        public int NodeID;
        public string Title;
        public string Content;
        public int FirstEventID;
    }
    
    // 结局配置
    [System.Serializable]
    public class EndingData
    {
        public int EndingID;
        public string Title;
        public string Description;
        public string Condition;
    }
    
    [System.Serializable]
    public class CardData
    {
        public int ID; public string Name; public CardType Type; public CardSubType SubType;
        public int Cost_Food; public int Cost_Armor; public int Power; public string Effect_ID; public int Effect_Val; public string Description;
    }
    [System.Serializable]
    public class EnemyData { public int ID; public string Name; public int Power; public string Description; public string Intent_Pattern; }

    public enum CardType { Unit, Strategy }
    public enum CardSubType { Auxiliary, Regular, Elite, Tactic }

    public List<EventData> AllEvents = new List<EventData>();
    public List<EventData_v2> AllEvents_v2 = new List<EventData_v2>();  // 新版本事件表
    public List<StoryPanelData> AllStoryPanels = new List<StoryPanelData>();  // 节点剧情面板
    public List<EndingData> AllEndings = new List<EndingData>();  // 结局表
    public List<CardData> AllCards = new List<CardData>();
    public List<EnemyData> AllEnemies = new List<EnemyData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); LoadAllData(); }
    }

    void LoadAllData()
    {
        IsReady = false;
        LoadEventTable();
        LoadEventTable_v2();  // 新增：加载v2版本事件表
        LoadStoryPanelTable();  // 新增：加载节点剧情面板
        LoadEndingTable();  // 新增：加载结局表
        LoadCardTable();
        LoadEnemyTable();
        IsReady = true; // 🔥 标记加载完成
    }

    void LoadEventTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable");
        if (textAsset == null)
        {
            Debug.LogError("❌ DataManager: 找不到 EventTable！将生成保底测试数据。");
            GenerateFallbackEvent(); // 生成保底数据，防止白屏
            return;
        }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEvents.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length < 10) continue;
            try
            {
                EventData evt = new EventData();
                evt.ID = ParseInt(row[0]);
                evt.IsPeaceful = (row[1] == "1" || row[1].ToLower() == "true");
                evt.Title = row[2]; evt.Context = row[3];
                evt.OptA_Text = row[4]; evt.OptA_Res1_Txt = row[5]; evt.OptA_Res1_Data = row[6];
                evt.OptA_Res2_Rate = ParseInt(row[7]); evt.OptA_Res2_Txt = row[8]; evt.OptA_Res2_Data = row[9];
                if (row.Length > 10) evt.OptB_Text = row[10];
                if (row.Length > 11) evt.OptB_Res1_Txt = row[11];
                if (row.Length > 12) evt.OptB_Res1_Data = row[12];
                if (row.Length > 13) evt.OptB_Res2_Rate = ParseInt(row[13]);
                if (row.Length > 14) evt.OptB_Res2_Txt = row[14];
                if (row.Length > 15) evt.OptB_Res2_Data = row[15];
                if (row.Length > 16) evt.Effect_Type = row[16];
                if (row.Length > 17) evt.OptB_Condition = row[17];
                AllEvents.Add(evt);
            }
            catch { }
        }

        if (AllEvents.Count == 0) GenerateFallbackEvent();
    }

    void GenerateFallbackEvent()
    {
        AllEvents.Add(new EventData
        {
            ID = 999,
            Title = "调试模式",
            Context = "数据文件缺失，这是自动生成的保底事件。",
            IsPeaceful = true,
            OptA_Text = "确定",
            OptB_Text = "跳过"
        });
    }

    // --- 1. 加载卡牌数据 (整合你发的代码) ---
    void LoadCardTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/CardTable");
        if (textAsset == null) { Debug.LogWarning("⚠️ 找不到 CardTable"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllCards.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length < 10) continue;

            try
            {
                CardData card = new CardData();
                card.ID = ParseInt(row[0]); card.Name = row[1];
                card.Type = ParseEnum<CardType>(row[2]); card.SubType = ParseEnum<CardSubType>(row[3]);
                card.Cost_Food = ParseInt(row[4]); card.Cost_Armor = ParseInt(row[5]);
                card.Power = ParseInt(row[6]); card.Effect_ID = row[7]; card.Effect_Val = ParseInt(row[8]);
                card.Description = row[9];
                AllCards.Add(card);
            }
            catch { }
        }
        Debug.Log($"✅ 加载卡牌: {AllCards.Count} 张");
    }

    // --- 2. 加载敌人数据 (整合你发的代码) ---
    void LoadEnemyTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EnemyTable");
        if (textAsset == null) { return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEnemies.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length < 4) continue;
            try
            {
                EnemyData enemy = new EnemyData();
                enemy.ID = ParseInt(row[0]); enemy.Name = row[1];
                enemy.Power = ParseInt(row[2]); enemy.Description = row[3];
                if (row.Length > 4) enemy.Intent_Pattern = row[4];
                AllEnemies.Add(enemy);
            }
            catch { }
        }
        Debug.Log($"✅ 加载敌人: {AllEnemies.Count} 个");
    }

    // --- 3. 组建初始套牌 (整合你发的代码) ---
    public List<CardData> GetStarterDeck()
    {
        List<CardData> deck = new List<CardData>();
        // 如果表是空的，生成默认牌防止报错
        if (AllCards.Count == 0) return deck;

        void AddCardById(int id, int count)
        {
            CardData card = AllCards.Find(c => c.ID == id);
            if (card != null) for (int i = 0; i < count; i++) deck.Add(card);
        }

        AddCardById(1001, 6); AddCardById(1002, 4); AddCardById(1003, 2);
        AddCardById(2001, 5); AddCardById(2002, 3);
        AddCardById(3001, 1); AddCardById(3002, 1); AddCardById(3003, 1); AddCardById(3004, 1);

        if (deck.Count == 0)
        {
            // 保底：全部塞第一张牌
            for (int i = 0; i < 10; i++) deck.Add(AllCards[0]);
        }
        return deck;
    }

    // 工具方法
    private string[] SplitCsvLine(string line)
    {
        string pattern = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        string[] fields = Regex.Split(line, pattern);
        for (int i = 0; i < fields.Length; i++)
        {
            fields[i] = fields[i].Trim();
            if (fields[i].StartsWith("\"") && fields[i].EndsWith("\"")) fields[i] = fields[i].Substring(1, fields[i].Length - 2);
            fields[i] = fields[i].Replace("\"\"", "\"");
        }
        return fields;
    }
    int ParseInt(string s) { int.TryParse(s, out int r); return r; }
    T ParseEnum<T>(string s) { try { return (T)Enum.Parse(typeof(T), s, true); } catch { return default(T); } }

    public EventData GetRandomEvent()
    {
        if (AllEvents.Count == 0) return null;
        return AllEvents[UnityEngine.Random.Range(0, AllEvents.Count)];
    }
    
    // ===== 新增方法：线性分支系统 =====
    
    // 加载新版本事件表
    void LoadEventTable_v2()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable_v2");
        if (textAsset == null) { Debug.LogWarning("⚠️ 找不到 EventTable_v2，将跳过加载"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEvents_v2.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length < 8) continue;
            try
            {
                EventData_v2 evt = new EventData_v2();
                evt.ID = ParseInt(row[0]);
                evt.IsPeaceful = (row[1] == "1" || row[1].ToLower() == "true");
                evt.Title = row[2];
                evt.Context = row[3];
                
                // 选项A
                evt.OptA_Text = row[4];
                evt.OptA_Result_Txt = row[5];
                evt.OptA_Result_Data = row[6];
                evt.NextID_A = ParseInt(row[7]);
                if (row.Length > 8) evt.Condition_A = row[8];
                
                // 选项B
                if (row.Length > 9) evt.OptB_Text = row[9];
                if (row.Length > 10) evt.OptB_Result_Txt = row[10];
                if (row.Length > 11) evt.OptB_Result_Data = row[11];
                if (row.Length > 12) evt.NextID_B = ParseInt(row[12]);
                if (row.Length > 13) evt.Condition_B = row[13];
                
                // 特效
                if (row.Length > 14) evt.Effect_Type = row[14];
                
                AllEvents_v2.Add(evt);
            }
            catch (System.Exception ex) { Debug.LogWarning($"解析事件行失败: {ex.Message}"); }
        }
        Debug.Log($"✅ 加载v2事件表: {AllEvents_v2.Count} 条");
    }
    
    // 加载节点剧情面板表
    void LoadStoryPanelTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/StoryPanelTable");
        if (textAsset == null) { Debug.LogWarning("⚠️ 找不到 StoryPanelTable，将跳过加载"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllStoryPanels.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length < 4) continue;
            try
            {
                StoryPanelData panel = new StoryPanelData();
                panel.NodeID = ParseInt(row[0]);
                panel.Title = row[1];
                panel.Content = row[2];
                panel.FirstEventID = ParseInt(row[3]);
                AllStoryPanels.Add(panel);
            }
            catch { }
        }
        Debug.Log($"✅ 加载剧情面板: {AllStoryPanels.Count} 个");
    }
    
    // 加载结局表
    void LoadEndingTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EndingTable");
        if (textAsset == null) { Debug.LogWarning("⚠️ 找不到 EndingTable，将跳过加载"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEndings.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            if (row.Length < 4) continue;
            try
            {
                EndingData ending = new EndingData();
                ending.EndingID = ParseInt(row[0]);
                ending.Title = row[1];
                ending.Description = row[2];
                ending.Condition = row[3];
                AllEndings.Add(ending);
            }
            catch { }
        }
        Debug.Log($"✅ 加载结局表: {AllEndings.Count} 个");
    }
    
    // 查询方法：按ID获取v2事件
    public EventData_v2 GetEventByID_v2(int eventID)
    {
        return AllEvents_v2.Find(e => e.ID == eventID);
    }
    
    // 查询方法：按节点ID获取剧情面板
    public StoryPanelData GetStoryPanelByNodeID(int nodeID)
    {
        return AllStoryPanels.Find(p => p.NodeID == nodeID);
    }
    
    public EnemyData GetEnemyByID(int id) { return AllEnemies.Find(e => e.ID == id); }
}