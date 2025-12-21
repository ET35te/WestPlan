using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ResourceItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("配置")]
    public string ChineseName; 
    
    [Header("引用")]
    public GameObject TooltipObj; // 👈 那个关不掉的弹窗就是它！
    public TMP_Text TooltipText;  

    // ... (你的 OnPointerEnter 和 OnPointerExit 代码保持不变) ...

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(TooltipObj != null)
        {
             // 如果 TooltipText 不为空才赋值，防止报错
            if(TooltipText != null) TooltipText.text = ChineseName;
            TooltipObj.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    // 🔥🔥🔥 必须加这部分！这是修复“无法销毁”的关键！ 🔥🔥🔥
    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    // 统一关闭方法
    void HideTooltip()
    {
        if(TooltipObj != null)
        {
            TooltipObj.SetActive(false);
        }
    }
}