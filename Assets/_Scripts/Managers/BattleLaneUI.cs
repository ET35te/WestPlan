using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleLaneUI : MonoBehaviour
{
    [Header("配置")]
    public string LaneName; // "前军", "中军"...
    public int MaxSlots = 4; // 上限4张

    [Header("UI引用")]
    public Transform SlotContainer; // 放卡牌小图标的地方
    public Toggle GrainToggle;      // 粮草开关
    public Toggle ShieldToggle;     // 护盾开关
    public TMP_Text PowerText;      // 显示当前战力

    private List<DataManager.CardData> cardsInLane = new List<DataManager.CardData>();

    public bool HasGrain => GrainToggle.isOn;
    public bool HasShield => ShieldToggle.isOn;
// 在 BattleLaneUI.cs 中添加/修改

    [Header("敌方意图 UI")]
    public Image EnemyIcon;       // 拖入 Enemy_Intent_Icon
    public TMPro.TMP_Text EnemyValText; // 拖入 Enemy_Value_Text
    
    // 敌方这一路的数据
    public int EnemyPower { get; private set; }
    public bool IsEnemyAttacking { get; private set; } // true=攻, false=守/空

    // 设置敌方意图 (由 Manager 调用)
   
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => BattleManager.Instance.OnLaneClicked(transform.GetSiblingIndex()));
    }

    public void ResetLane()
    {
        cardsInLane.Clear();
        GrainToggle.isOn = false;
        ShieldToggle.isOn = false;
        UpdateVisuals();
    }

    // 尝试添加卡牌
    public bool AddCard(DataManager.CardData card)
    {
        if (cardsInLane.Count >= MaxSlots) 
        {
            // --- 视觉反馈：飘字或抖动 ---
            Debug.Log("战线已满！"); // 暂时用Log，下周做漂浮文字
            return false; 
        }
        
        cardsInLane.Add(card);
        UpdateVisuals();
        return true;
    } public void SetEnemyIntent(int power, bool isAttack)
    {
        EnemyPower = power;
        IsEnemyAttacking = isAttack;

        EnemyValText.text = power.ToString();
        // 简单变色：红=攻，蓝=守
        EnemyIcon.color = isAttack ? Color.red : Color.blue; 
        
        // 如果有图标资源，可以在这里 swap sprite
        // EnemyIcon.sprite = isAttack ? Icon_Sword : Icon_Shield;
    }
    // 清空并返回卡牌 (给弃牌堆)
    public List<DataManager.CardData> ClearLane()
    {
        List<DataManager.CardData> temp = new List<DataManager.CardData>(cardsInLane);
        cardsInLane.Clear();
        UpdateVisuals();
        return temp;
    }

    public int GetTotalPower()
    {
        int sum = 0;
        foreach (var c in cardsInLane) sum += c.Power;
        return sum;
    }

    void UpdateVisuals()
    {
        // 1. 更新卡牌小图标 (简单用个方块代表)
        foreach (Transform child in SlotContainer) Destroy(child.gameObject);
        
        foreach (var card in cardsInLane)
        {
            // 这里应该生成一个小一点的卡牌图标，简单起见生成一个 Text
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(SlotContainer, false);
            var txt = icon.AddComponent<TextMeshProUGUI>();
            txt.text = $"[{card.Power}]";
            txt.fontSize = 20;
            txt.color = Color.white;
        }

        // 2. 更新战力显示
        int p = GetTotalPower();
        PowerText.text = $"{LaneName}\n战力: {p*p} ({p}²)";
    }
}