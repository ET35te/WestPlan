using UnityEngine;
using System.Collections.Generic;
using System;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // --- 1. 事件数据结构 (已更新: 22列) ---
    [System.Serializable]
    public class EventData
    {
        public int ID;
        public bool IsPeaceful; 
        public string Title;
        public string Context;

        // 选项 A
        public string OptA_Text;
        public string OptA_Res1_Txt;
        public string OptA_Res1_Data;
        public int OptA_Res2_Rate;
        public string OptA_Res2_Txt;
        public string OptA_Res2_Data;
        // 选项 B
        public string OptB_Text;
        public string OptB_Res1_Txt;
        public string OptB_Res1_Data;
        public int OptB_Res2_Rate;
        public string OptB_Res2_Txt;
        public string OptB_Res2_Data;

        // --- NEW! 新增字段 ---
        public string Effect_Type;    // 特殊效果 (SWITCH_ROUTE 等)
        public string OptB_Condition; // 选项B条件 (104:500)
    }

    // --- 2. 阵法卡数据结构 (不变) ---
    [System.Serializable]
    public class BattleCardData
    {
        public int ID;
        public string Name;
        public string Type;
        public string Description;
        public int Cond_ResID;
        public int Cond_Val;
        public float Sacrifice_Cost;
        public string Buff_Type;
        public float Buff_Val;
    }

    // --- 3. 敌人数据结构 (NEW!) ---
    [System.Serializable]
    public class EnemyData
    {
        public int ID;
        public string Name;
        public int Power;
        public string Description;
        public string Intent_Pattern; // 意图模式字符串 (如 "A,D,N,A,N")
    }

    // --- 数据列表 ---
    public List<EventData> AllEvents = new List<EventData>();
    public List<BattleCardData> AllBattleCards = new List<BattleCardData>();
    public List<EnemyData> AllEnemies = new List<EnemyData>(); // 新增敌人列表

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Start()
    {
        LoadEventTable();
        LoadBattleCardTable();
        LoadEnemyTable(); // 新增加载调用
    }

    // --- 加载事件表 (逻辑更新) ---
    void LoadEventTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EventTable");
        if(textAsset == null) { Debug.LogError("找不到 Data/EventTable !"); return; }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEvents.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            
            // 注意：现在我们期望至少有 22 列数据 (如果不够，可能是Excel没填完整，我们做个兼容)
            if (row.Length < 2) continue; 

            try 
            {
                EventData evt = new EventData();
                evt.ID = ParseInt(row[0]);
                
                string peaceStr = row[1].ToUpper().Trim();
                evt.IsPeaceful = (peaceStr == "TRUE" || peaceStr == "1");

                evt.Title = row[2];
                evt.Context = row[3].Replace(";", ",");

                // 选项 A (索引 4-11)
                 evt.OptA_Text = row[4];
                evt.OptA_Res1_Txt = row[5];
                evt.OptA_Res1_Data = row[6]; // 直接读字符串，不用 ParseInt
                
                evt.OptA_Res2_Rate = ParseInt(row[7]);
                evt.OptA_Res2_Txt = row[8];
                evt.OptA_Res2_Data = row[9]; // 直接读字符串

                // 选项 B (注意索引变化，因为少了列，后面的索引都变了)
                // 请务必根据你 Excel 实际的列数去数一下索引！
                // 假设上面合并列后：
                evt.OptB_Text = row[10];
                evt.OptB_Res1_Txt = row[11];
                evt.OptB_Res1_Data = row[12];
                
                evt.OptB_Res2_Rate = ParseInt(row[13]);
                evt.OptB_Res2_Txt = row[14];
                evt.OptB_Res2_Data = row[15];

                if (row.Length > 16) evt.Effect_Type = row[16].Trim();
                if (row.Length > 17) evt.OptB_Condition = row[17].Trim();

                AllEvents.Add(evt);
            }
            catch (Exception e) { Debug.LogError($"事件表第 {i} 行错误: {e.Message}"); }
        }
        Debug.Log($"【数据】加载了 {AllEvents.Count} 个事件。");
    }

    // --- 加载敌人表 (NEW!) ---
    void LoadEnemyTable()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/EnemyTable");
        if (textAsset == null) 
        { 
            // 如果还没建表，先不报错，只打印警告，避免卡死
            Debug.LogWarning("未找到 Data/EnemyTable，战斗系统将无法读取敌人配置。"); 
            return; 
        }

        string[] lines = textAsset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        AllEnemies.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 5) continue;

            try
            {
                EnemyData enemy = new EnemyData();
                enemy.ID = ParseInt(row[0]);
                enemy.Name = row[1];
                enemy.Power = ParseInt(row[2]);
                enemy.Description = row[3].Replace(";", ",");
                enemy.Intent_Pattern = row[4];

                AllEnemies.Add(enemy);
            }
            catch (Exception e) { Debug.LogError($"敌人表第 {i} 行错误: {e.Message}"); }
        }
        Debug.Log($"【数据】加载了 {AllEnemies.Count} 个敌人配置。");
    }

    // --- 加载阵法卡表 (保持不变) ---
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

    // --- 辅助方法 ---
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
    
    // 获取特定敌人数据的辅助方法 (供 BattleManager 使用)
    public EnemyData GetEnemyByID(int id)
    {
        return AllEnemies.Find(e => e.ID == id);
    }
}