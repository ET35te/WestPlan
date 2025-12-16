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
        if (cardsInLane.Count >= MaxSlots) return false; // 满了
        
        cardsInLane.Add(card);
        UpdateVisuals();
        return true;
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