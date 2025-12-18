using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleCardUI : MonoBehaviour
{
    public TMP_Text NameText;
    public TMP_Text PowerText;
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
        NameText.text = data.Name;
        PowerText.text = data.Type == 1 ? $"兵力: {data.Power}" : "兵法";
        DescText.text = data.Description;

        ClickBtn.onClick.RemoveAllListeners();
        ClickBtn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // 通知 BattleManager 我被选中了
        BattleManager.Instance.OnHandCardClicked(myData);
        
        // 视觉反馈：变个颜色表示选中
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