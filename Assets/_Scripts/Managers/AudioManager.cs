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
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

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
        SFXSource.PlayOneShot(clip);
    }
}