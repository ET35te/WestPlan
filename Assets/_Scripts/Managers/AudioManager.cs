using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("挂载 AudioSource 组件")]
    public AudioSource MusicSource; // 用来播 BGM
    public AudioSource SFXSource;   // 用来播音效

    private void Awake()
    {
        // 🛑 严格的单例检查
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 如果已经有 AudioManager 了，新来的立刻销毁
            return;              // 🔥 必须加这行！立刻停止运行，不要让“尸体”继续执行下面的代码
        }

        Instance = this;

        // ✅ 恢复这行代码：让音乐在切换场景时不会断
        DontDestroyOnLoad(gameObject);
    }

    // --- 下面的代码保持不变 ---

    // 播背景音乐 (循环)
    public void PlayMusic(AudioClip clip)
    {
        if (MusicSource.clip == clip) return; // 如果已经是这首，就不重播
        MusicSource.clip = clip;
        MusicSource.loop = true;
        MusicSource.Play();
    }

    // 播音效 (一次性)
    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip); // 推荐用 PlayOneShot，这样短促音效可以重叠播放
    }
}