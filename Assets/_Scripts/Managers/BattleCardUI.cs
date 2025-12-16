using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleCardUI : MonoBehaviour
{
    public TMP_Text NameText;
    public TMP_Text PowerText;
    public TMP_Text DescText;
    public Button ClickBtn;

    private DataManager.CardData myData;

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
}