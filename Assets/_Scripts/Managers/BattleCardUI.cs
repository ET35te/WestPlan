using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // --- 修复点：引入 TMP ---

public class BattleCardUI : MonoBehaviour
{
    [Header("UI组件 (TMP)")]
    public TMP_Text NameText;       // 改为 TMP_Text
    public TMP_Text DescText;       // 改为 TMP_Text
    public TMP_Text CostText;       // 改为 TMP_Text
    public Button SelectBtn;    

    private DataManager.BattleCardData myData;

    public void Setup(DataManager.BattleCardData data, System.Action<DataManager.BattleCardData> onClick)
    {
        myData = data;

        NameText.text = data.Name;
        DescText.text = data.Description;

        if (data.Sacrifice_Cost > 0)
        {
            string resName = GetResName(data.Cond_ResID);
            // TMP 支持富文本颜色
            CostText.text = $"<color=red>⚠️ 牺牲 {(int)(data.Sacrifice_Cost * 100)}% {resName}</color>";
            CostText.gameObject.SetActive(true);
        }
        else
        {
            CostText.gameObject.SetActive(false);
        }

        SelectBtn.onClick.RemoveAllListeners();
        SelectBtn.onClick.AddListener(() => onClick(myData));
    }

    string GetResName(int id)
    {
        switch(id) {
            case 101: return "信念"; case 102: return "粮食";
            case 103: return "储水"; case 104: return "兵力";
            case 105: return "财货"; case 106: return "马匹";
            case 107: return "披甲"; default: return "资源";
        }
    }
}