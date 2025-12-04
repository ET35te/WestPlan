using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleLaneUI : MonoBehaviour
{
    [Header("UI引用")]
    public TMP_Text LaneNameText;       // 显示 "左翼" 等
    public Image EnemyIntentIcon;       // 敌方意图图标
    public Button PlayerCommandBtn;     // 玩家点击的按钮
    public TMP_Text CommandText;        // 按钮上的字
    public TMP_Text ResultText;         // 结算文字

    [Header("图标配置")]
    public Sprite Icon_Attack;
    public Sprite Icon_Defend;
    public Sprite Icon_Empty;

    // 数据索引
    private int laneIndex; 
    
    // 初始化
    public void Setup(int index, string name)
    {
        laneIndex = index;
        LaneNameText.text = name;
        ResultText.text = "";
        
        // 绑定点击事件：点击按钮 -> 呼叫 Manager 切换指令
        PlayerCommandBtn.onClick.RemoveAllListeners();
        PlayerCommandBtn.onClick.AddListener(() => NewBattleManager.Instance.OnLaneClicked(laneIndex));
    }

    // 更新敌方显示 (明牌)
    public void UpdateEnemyView(NewBattleManager.MilitaryStance stance)
    {
        switch (stance)
        {
            case NewBattleManager.MilitaryStance.Attack:
                EnemyIntentIcon.sprite = Icon_Attack;
                EnemyIntentIcon.color = Color.red;
                break;
            case NewBattleManager.MilitaryStance.Defend:
                EnemyIntentIcon.sprite = Icon_Defend;
                EnemyIntentIcon.color = Color.blue;
                break;
            case NewBattleManager.MilitaryStance.Empty:
                EnemyIntentIcon.sprite = Icon_Empty;
                EnemyIntentIcon.color = new Color(0,0,0,0.5f); // 半透明
                break;
        }
    }

    // 更新我方显示
    public void UpdatePlayerView(NewBattleManager.MilitaryStance stance)
    {
        switch (stance)
        {
            case NewBattleManager.MilitaryStance.Attack:
                CommandText.text = "攻 (兵-5)";
                PlayerCommandBtn.image.color = new Color(1f, 0.6f, 0.6f); // 淡红
                break;
            case NewBattleManager.MilitaryStance.Defend:
                CommandText.text = "守 (粮-10)";
                PlayerCommandBtn.image.color = new Color(0.6f, 0.6f, 1f); // 淡蓝
                break;
            case NewBattleManager.MilitaryStance.Empty:
                CommandText.text = "空 (无消耗)";
                PlayerCommandBtn.image.color = Color.white;
                break;
        }
    }

    // 显示本路结算结果
    public void ShowResult(string text, Color color)
    {
        ResultText.text = text;
        ResultText.color = color;
    }
}
