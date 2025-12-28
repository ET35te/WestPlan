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
        // 如果已经有其他的 Instance 了
        if (Instance != null && Instance != this)
        {
            // 🔥 关键修改：停用组件，延迟销毁
            // 这样能避开 Unity 在加载帧的断言检查
            this.enabled = false; 
            Destroy(this.gameObject); 
            return;
        }

        Instance = this;
        // 只有根物体才能 DontDestroyOnLoad，防止报错
        transform.SetParent(null); 
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