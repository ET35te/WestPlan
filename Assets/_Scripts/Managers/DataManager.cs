using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // --- 数据类定义 ---
    [System.Serializable]
    public class EventData
    {
        public int ID; public bool IsPeaceful; public string Title; public string Context;
        public string OptA_Text; public string OptA_Res1_Txt; public string OptA_Res1_Data; public int OptA_Res2_Rate; public string OptA_Res2_Txt; public string OptA_Res2_Data;
        public string OptB_Text; public string OptB_Res1_Txt; public string OptB_Res1_Data; public int OptB_Res2_Rate; public string OptB_Res2_Txt; public string OptB_Res2_Data;
        public string Effect_Type; public string OptB_Condition;
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
    public List<CardData> AllCards = new List<CardData>();
    public List<EnemyData> AllEnemies = new List<EnemyData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); LoadEventTable(); LoadCardTable(); LoadEnemyTable(); }
    }

    void LoadEventTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable");
        // 🔥 增加报错提醒
        if (textAsset == null) { Debug.LogError("❌ DataManager: 找不到 Resources/Data/EventTable.csv！请检查路径！"); return; }

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
        Debug.Log($"✅ 加载事件: {AllEvents.Count} 个");
    }

    // ... (LoadCardTable 和 LoadEnemyTable 请保留你原来的代码，记得加上 Resources.Load 的判空检查) ...
    // ... (GetStarterDeck 保持不变) ...

    void LoadCardTable() { /* 略，请用上一版代码或保持现状 */ }
    void LoadEnemyTable() { /* 略 */ }

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
    public EnemyData GetEnemyByID(int id) { return AllEnemies.Find(e => e.ID == id); }
    public List<CardData> GetStarterDeck() { /* 请使用之前提供的完整代码 */ return new List<CardData>(); }
}