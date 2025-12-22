using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleCardUI : MonoBehaviour
{
    public TMP_Text NameText;
    public TMP_Text PowerText; // 用于显示战力或消耗
    public TMP_Text DescText;
    public Button ClickBtn;
    public Image BorderImage; // 拖入卡牌背景图或专门的边框图

    private DataManager.CardData myData;

    public DataManager.CardData Data 
    { 
        get { return myData; } 
    }

    public void Setup(DataManager.CardData data)
    {
        myData = data;
        
        // 1. 设置名称
        if (NameText != null) NameText.text = data.Name;

        // 2. 🔥 设置数值显示 (根据最新的枚举类型)
        if (PowerText != null)
        {
            if (data.Type == DataManager.CardType.Unit)
            {
                // 如果是兵力卡，显示战力
                PowerText.text = $"⚔️ {data.Power}";
            }
            else
            {
                // 如果是策略卡，显示消耗 (粮/甲)
                string costStr = "";
                if (data.Cost_Food > 0) costStr += $"粮{data.Cost_Food} ";
                if (data.Cost_Armor > 0) costStr += $"甲{data.Cost_Armor}";
                if (string.IsNullOrEmpty(costStr)) costStr = "无消耗";
                
                PowerText.text = costStr;
            }
        }

        // 3. 设置描述
        if (DescText != null) DescText.text = data.Description;

        // 4. 绑定按钮事件
        if (ClickBtn != null)
        {
            ClickBtn.onClick.RemoveAllListeners();
            ClickBtn.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        // ⚠️ 注意：如果你现在的 BattleManager 还没有 OnHandCardClicked 方法
        // ⚠️ 请先保持下面这行注释状态，否则会报错。
        // BattleManager.Instance.OnHandCardClicked(myData);
        
        // 临时调试反馈
        Debug.Log($"点击了卡牌: {myData.Name}");

        // 视觉反馈：变个颜色表示选中
        if (GetComponent<Image>() != null)
            GetComponent<Image>().color = Color.yellow; 
    }

    public void SetSelected(bool isSelected)
    {
        if (BorderImage != null)
        {
            // 选中变绿，没选中变白
            BorderImage.color = isSelected ? Color.green : Color.white;
            
            // 或者放大一点
            transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
        }
    }
}