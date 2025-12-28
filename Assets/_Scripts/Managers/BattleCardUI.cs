using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleCardUI : MonoBehaviour
{
    public DataManager.CardData Data;

    [Header("UI 组件")]
    public TMP_Text NameText;
    public TMP_Text CostText; // 显示: 粮2 甲1
    public TMP_Text DescText; // 显示效果描述
    public Image CardBackground; // 用于变色

    // 初始化显示
    public void Setup(DataManager.CardData cardData)
    {
        Data = cardData;

        if (NameText) NameText.text = Data.Name;
        // 简单拼装一下描述，比如 "粮1 甲0"
        if (CostText) CostText.text = $"<color=#FFCC00>粮{Data.Cost_Food}</color> <color=#ADD8E6>甲{Data.Cost_Armor}</color>";
        if (DescText) DescText.text = Data.Description;
        if (CostText) 
{
    // 这行代码的意思是：粮(黄色) + 数值 + 空格 + 甲(蓝色) + 数值
    CostText.text = $"<color=#FFD700>粮{Data.Cost_Food}</color>   <color=#ADD8E6>甲{Data.Cost_Armor}</color>";
    
    // 💡 必须开启 Rich Text：
    // 请去 Unity 编辑器，选中 BattleCard_Prefab 里的 CostText 物体
    // 在 Inspector 的 TextMeshPro 组件里，找到 "Extra Settings"，勾选 ✅ Rich Text
}
        // 自动获取背景图用于变色
        if (CardBackground == null) CardBackground = GetComponent<Image>();
    }

    // 点击事件 (绑定到 Button 组件)
    public void OnClick()
    {
        // 告诉 BattleManager 我被点了
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnHandCardClicked(this);
        }
    }

    // 🔥 核心修复：选中/取消选中的视觉效果
    public void UpdateState(bool isSelected)
    {
        if (CardBackground != null)
        {
            // 选中变黄，没选中变白
            CardBackground.color = isSelected ? Color.yellow : Color.white;
        }

        // 可选：选中时放大一点点
        transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
    }
}