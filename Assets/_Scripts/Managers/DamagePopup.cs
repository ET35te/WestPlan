using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 🔥 伤害飘字效果脚本
/// 负责：生成 -> 位移动画 -> 渐隐 -> 销毁
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI tmpText;
    private RectTransform rectTransform;
    
    [Header("动画参数")]
    public float FloatDuration = 1.5f;      // 总时长
    public float FloatHeight = 100f;        // 上升高度
    public Color StartColor = Color.white;  // 起始颜色
    public Color EndColor = new Color(1, 1, 1, 0); // 结束颜色（透明）

    // 🔥 新增：静态引用方案（支持在 Inspector 中拖拽）
    public static GameObject prefabReference;

    private void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 初始化并播放飘字效果
    /// </summary>
    public void Show(string text, Vector3 worldPosition, Color color = default)
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        // 设置文字
        tmpText.text = text;
        tmpText.color = color == default ? StartColor : color;

        // 设置初始位置 (从世界坐标转换到UI坐标)
        if (rectTransform.parent != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition),
                Camera.main,
                out Vector2 localPoint
            );
            rectTransform.anchoredPosition = localPoint;
        }

        // 启动动画协程
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * FloatHeight;

        while (elapsed < FloatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FloatDuration;

            // 位移
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            // 渐隐
            Color newColor = Color.Lerp(StartColor, EndColor, t);
            tmpText.color = newColor;

            yield return null;
        }

        // 确保最终状态正确
        rectTransform.anchoredPosition = endPos;
        tmpText.color = EndColor;

        // 销毁
        Destroy(gameObject);
    }

    /// <summary>
    /// 工厂方法：快速生成飘字
    /// 支持两种方式：Resources.Load 或 直接引用
    /// </summary>
    public static void SpawnPopup(string text, Vector3 worldPosition, Color color = default)
    {
        GameObject prefab = null;

        // 🔥 方案 A：使用静态引用（推荐 - 避免 Resources 问题）
        if (prefabReference != null)
        {
            prefab = prefabReference;
        }
        
        // 🔥 方案 B：尝试 Resources.Load（备选方案）
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("UI/DamagePopup");
        }
        
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("DamagePopup");
        }
        
        // 如果都失败，输出错误信息
        if (prefab == null)
        {
            Debug.LogError("❌ 无法加载 DamagePopup.prefab！" +
                          "\n请检查以下任一条件：" +
                          "\n1. 在 DamagePopup.cs 脚本的静态变量 'prefabReference' 中拖拽 prefab" +
                          "\n2. 或确保 prefab 在 Assets/Resources/UI/DamagePopup.prefab" +
                          "\n3. 或执行 Assets → Reimport All 重新导入资源");
            return;
        }

        GameObject popupObj = Instantiate(
            prefab,
            Vector3.zero,
            Quaternion.identity
        );

        // 放在 Canvas 下
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            popupObj.transform.SetParent(canvas.transform, false);
        }

        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Show(text, worldPosition, color);
        }
        else
        {
            Debug.LogError("❌ DamagePopup 组件未找到！");
            Destroy(popupObj);
        }
    }
}

