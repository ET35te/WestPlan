using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // 必须引用
using UnityEngine.UI;
using TMPro; // 引用 TMP

public class ResourceItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("配置")]
    public string ChineseName; // 在Inspector里填，比如“粮食”
    
    [Header("引用")]
    public GameObject TooltipObj; // 那个提示框物体
    public TMP_Text TooltipText;  // 提示框里的文字组件

    // 鼠标移入
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(TooltipObj != null)
        {
            TooltipText.text = ChineseName;
            TooltipObj.SetActive(true); // 显示
        }
    }

    // 鼠标移出
    public void OnPointerExit(PointerEventData eventData)
    {
        if(TooltipObj != null)
        {
            TooltipObj.SetActive(false); // 隐藏
        }
    }
}