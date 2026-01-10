using UnityEngine;
using System.Collections;

#if DOTween
using DG.Tweening;
#endif

/// <summary>
/// UI 动效管理器 - 处理节点分页 UI 的各种动效
/// 支持 DOTween 和 Coroutine 两种方式
/// </summary>
public class EventPageUIEffects : MonoBehaviour
{
    /// <summary>
    /// 选项按钮点击时的 Punch Scale 动效
    /// </summary>
    public static void PlayButtonPunchEffect(RectTransform button)
    {
        if (button == null) return;

        #if DOTween
        button.DOPunchScale(Vector3.one * 0.15f, 0.2f, 10, 1f);
        #else
        MonoSingleton.StartCoroutine(CoroutineButtonPunch(button));
        #endif
    }

    /// <summary>
    /// 资源图标变动时的抖动效果
    /// </summary>
    public static void PlayResourceIconShake(RectTransform icon)
    {
        if (icon == null) return;

        #if DOTween
        icon.DOShakePosition(0.3f, new Vector3(8, 8, 0), 15, 90);
        #else
        MonoSingleton.StartCoroutine(CoroutineShakePosition(icon, 0.3f, new Vector3(8, 8, 0)));
        #endif
    }

    /// <summary>
    /// 事件完成时的盖章动效（旋转 + 缩放）
    /// </summary>
    public static void PlayEventCompletedEffect(RectTransform completedBadge)
    {
        if (completedBadge == null) return;

        #if DOTween
        completedBadge.localScale = Vector3.zero;
        completedBadge.localRotation = Quaternion.Euler(0, 0, -45f);
        
        var tween = completedBadge.DOScale(1f, 0.3f);
        tween.OnStart(() =>
        {
            completedBadge.DOLocalRotate(new Vector3(0, 0, 0), 0.3f);
        });
        #else
        MonoSingleton.StartCoroutine(CoroutineCompletedStamp(completedBadge));
        #endif
    }

    /// <summary>
    /// 翻页时的淡入淡出动效
    /// </summary>
    public static void PlayPageTransitionEffect(CanvasGroup contentGroup)
    {
        if (contentGroup == null) return;

        #if DOTween
        contentGroup.DOFade(0f, 0.15f).OnComplete(() =>
        {
            contentGroup.DOFade(1f, 0.15f);
        });
        #else
        MonoSingleton.StartCoroutine(CoroutinePageTransition(contentGroup));
        #endif
    }

    /// <summary>
    /// 拒绝操作时的 Shake 反馈（如资源不足）
    /// </summary>
    public static void PlayErrorShakeEffect(RectTransform target)
    {
        if (target == null) return;

        #if DOTween
        target.DOShakePosition(0.3f, new Vector3(10, 0, 0), 5, 90);
        #else
        MonoSingleton.StartCoroutine(CoroutineShakePosition(target, 0.3f, new Vector3(10, 0, 0)));
        #endif
    }

    // ========== Coroutine 备选方案（不需要 DOTween）==========

    private static IEnumerator CoroutineButtonPunch(RectTransform button)
    {
        Vector3 originalScale = button.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            button.localScale = originalScale * scale;
            yield return null;
        }

        button.localScale = originalScale;
    }

    private static IEnumerator CoroutineShakePosition(RectTransform target, float duration, Vector3 intensity)
    {
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-intensity.x, intensity.x);
            float y = Random.Range(-intensity.y, intensity.y);
            target.localPosition = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        target.localPosition = originalPos;
    }

    private static IEnumerator CoroutineCompletedStamp(RectTransform badge)
    {
        badge.localScale = Vector3.zero;
        badge.localRotation = Quaternion.Euler(0, 0, -45f);

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            badge.localScale = Vector3.one * t;
            badge.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -45f), Quaternion.identity, t);
            yield return null;
        }

        badge.localScale = Vector3.one;
        badge.localRotation = Quaternion.identity;
    }

    private static IEnumerator CoroutinePageTransition(CanvasGroup group)
    {
        // 淡出
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        group.alpha = 0f;

        // 淡入
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        group.alpha = 1f;
    }
}

/// <summary>
/// 单例协程启动器（用于静态方法调用协程）
/// </summary>
public class MonoSingleton : MonoBehaviour
{
    private static MonoSingleton instance;

    public static MonoSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = new GameObject("MonoSingleton");
                instance = obj.AddComponent<MonoSingleton>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    public static void StartCoroutine(IEnumerator coroutine)
    {
        Instance.StartCoroutine(coroutine);
    }
}
