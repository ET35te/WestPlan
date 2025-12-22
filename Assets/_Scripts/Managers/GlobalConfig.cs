public static class GlobalConfig
{
    // --- 初始资源 ---
    public static int Player_Start_Food = 0;
    public static int Player_Start_Armor = 0;
    
    public static int Enemy_Start_Food = 1;
    public static int Enemy_Start_Armor = 1;

    // --- 回合回复 ---
    public static int Turn_Regen_Food = 1;
    public static int Turn_Regen_Armor = 1;

    // --- 战斗数值 ---
    public static int Defend_Cost_Food = 1;      // "守"消耗多少粮
    public static int Defend_Mitigation = 5;     // "守"抵消多少伤害
    public static float Attack_Base_Mult = 1.0f; // 基础攻击系数
    
    // --- 初始血量/兵力 (暂时用这个模拟全歼判定) ---
    public static int Initial_Unit_Count = 10; 
}
