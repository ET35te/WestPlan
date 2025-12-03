using UnityEngine;
using System.Collections.Generic;
using System;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // --- 1. 事件数据结构 ---
    [System.Serializable]
    public class EventData
    {
        public int ID;
        public bool IsPeaceful; // TRUE=纯剧情, FALSE=触发战斗
        public string Title;
        public string Context;

        // 选项 A
        public string OptA_Text;
        public string OptA_Res1_Txt;
        public int OptA_Res1_ID;
        public int OptA_Res1_Val;
        public int OptA_Res2_Rate;
        public string OptA_Res2_Txt;
        public int OptA_Res2_ID;
        public int OptA_Res2_Val;

        // 选项 B
        public string OptB_Text;
        public string OptB_Res1_Txt;
        public int OptB_Res1_ID;
        public int OptB_Res1_Val;
        public int OptB_Res2_Rate;
        public string OptB_Res2_Txt;
        public int OptB_Res2_ID;
        public int OptB_Res2_Val;
    }

    // --- 2. 阵法卡数据结构 ---
    [System.Serializable]
    public class BattleCardData
    {
        public int ID;
        public string Name;
        public string Type;          // 火/林/山/风
        public string Description;
        public int Cond_ResID;       // 触发条件的资源ID
        public int Cond_Val;         // 触发条件的阈值
        public float Sacrifice_Cost; // 牺牲比例 (0.0 - 1.0)
        public string Buff_Type;     // 效果类型
        public float Buff_Val;       // 效果数值
    }

    public List<EventData> AllEvents = new List<EventData>();
    public List<BattleCardData> AllBattleCards = new List<BattleCardData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Start()
    {
        LoadEventTable();
        LoadBattleCardTable();
    }

    void LoadEventTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable");
        if(textAsset == null) { Debug.LogError("找不到 Data/EventTable !"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEvents.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 2) continue; // 简单防错

            try 
            {
                EventData evt = new EventData();
                evt.ID = ParseInt(row[0]);
                
                // 解析 IsPeaceful
                string peaceStr = row[1].ToUpper().Trim();
                evt.IsPeaceful = (peaceStr == "TRUE" || peaceStr == "1");

                evt.Title = row[2];
                evt.Context = row[3].Replace(";", ",");

                // 选项 A (索引从4开始)
                evt.OptA_Text = row[4];
                evt.OptA_Res1_Txt = row[5];
                evt.OptA_Res1_ID = ParseInt(row[6]);
                evt.OptA_Res1_Val = ParseInt(row[7]);
                evt.OptA_Res2_Rate = ParseInt(row[8]);
                evt.OptA_Res2_Txt = row[9];
                evt.OptA_Res2_ID = ParseInt(row[10]);
                evt.OptA_Res2_Val = ParseInt(row[11]);

                // 选项 B (索引从12开始)
                evt.OptB_Text = row[12];
                evt.OptB_Res1_Txt = row[13];
                evt.OptB_Res1_ID = ParseInt(row[14]);
                evt.OptB_Res1_Val = ParseInt(row[15]);
                evt.OptB_Res2_Rate = ParseInt(row[16]);
                evt.OptB_Res2_Txt = row[17];
                evt.OptB_Res2_ID = ParseInt(row[18]);
                evt.OptB_Res2_Val = ParseInt(row[19]);

                AllEvents.Add(evt);
            }
            catch (Exception e) { Debug.LogError($"事件表第 {i} 行错误: {e.Message}"); }
        }
        Debug.Log($"【数据】加载了 {AllEvents.Count} 个事件。");
    }

    void LoadBattleCardTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/BattleCardTable");
        if(textAsset == null) { Debug.LogError("找不到 Data/BattleCardTable !"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllBattleCards.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 9) continue;

            try
            {
                BattleCardData card = new BattleCardData();
                card.ID = ParseInt(row[0]);
                card.Name = row[1];
                card.Type = row[2];
                card.Description = row[3].Replace(";", ",");
                card.Cond_ResID = ParseInt(row[4]);
                card.Cond_Val = ParseInt(row[5]);
                float.TryParse(row[6], out card.Sacrifice_Cost);
                card.Buff_Type = row[7];
                float.TryParse(row[8], out card.Buff_Val);

                AllBattleCards.Add(card);
            }
            catch (Exception e) { Debug.LogError($"阵法表第 {i} 行错误: {e.Message}"); }
        }
        Debug.Log($"【数据】加载了 {AllBattleCards.Count} 张阵法卡。");
    }

    int ParseInt(string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        int.TryParse(str, out int result);
        return result;
    }

    public EventData GetRandomEvent()
    {
        if (AllEvents.Count == 0) return null;
        return AllEvents[UnityEngine.Random.Range(0, AllEvents.Count)];
    }

    public List<BattleCardData> DrawBattleCards(int count)
    {
        List<BattleCardData> drawn = new List<BattleCardData>();
        if (AllBattleCards.Count == 0) return drawn;
        for(int i=0; i<count; i++)
        {
            drawn.Add(AllBattleCards[UnityEngine.Random.Range(0, AllBattleCards.Count)]);
        }
        return drawn;
    }
}