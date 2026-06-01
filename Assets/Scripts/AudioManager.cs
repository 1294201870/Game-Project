using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM资源")]
    public AudioSource bgmSource; // 用于播放BGM
    public AudioClip BGM1, BGM2;

    [Header("风声音源")]
    public AudioSource windSource1, windSource2, windSource3;
    public AudioClip wind1, wind2, wind3;

    [Header("一次性音效")]
    public AudioSource sfxSource;  // PlayOneShot用
    public AudioClip paraClip;     // 降落伞
    public AudioClip crashClip;    // 坠毁
    public AudioClip checkpointClip;

    [Header("音量控制")]
    [Range(0, 1)] public float bgmVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 如需全局常驻可解开
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (windSource1 != null) { windSource1.clip = wind1; windSource1.loop = true; windSource1.volume = 0; windSource1.Play(); }
        if (windSource2 != null) { windSource2.clip = wind2; windSource2.loop = true; windSource2.volume = 0; windSource2.Play(); }
        if (windSource3 != null) { windSource3.clip = wind3; windSource3.loop = true; windSource3.volume = 0; windSource3.Play(); }
    }

    //---------------------------------------
    // 背景音乐切换
    public void PlayBGM(int idx)
    {
        AudioClip target = (idx == 1) ? BGM1 : BGM2;
        if (bgmSource.clip != target)
        {
            bgmSource.Stop();
            bgmSource.clip = target;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    // 可扩展更多音乐用字典或数组
    // public void PlayBGM(string key) {...}

    //---------------------------------------
    // 音效播放
    public void PlayParachute()
    {
        sfxSource.PlayOneShot(paraClip, sfxVolume);
    }

    public void PlayCrash()
    {
        sfxSource.PlayOneShot(crashClip, sfxVolume);
    }

    public void PlayCheckpoint()
    {
        sfxSource.PlayOneShot(checkpointClip, sfxVolume);
    }

    //---------------------------------------
    // 动态风声调整（传归一化速度0~1）
    public void UpdateWindSound(float speedNorm)
    {
        // 0~0.33 wind1，0.33~0.66 wind2，0.66~1 wind3，平滑淡入淡出
        windSource1.volume = Mathf.Lerp(windSource1.volume, speedNorm < 0.33f ? 1f : 0f, Time.deltaTime * 3f);
        windSource2.volume = Mathf.Lerp(windSource2.volume, (speedNorm >= 0.33f && speedNorm < 0.66f) ? 1f : 0f, Time.deltaTime * 3f);
        windSource3.volume = Mathf.Lerp(windSource3.volume, speedNorm >= 0.66f ? 1f : 0f, Time.deltaTime * 3f);
    }

    //---------------------------------------
    // 音量界面/代码动态调整
    public void SetBGMVolume(float v)
    {
        bgmVolume = v;
        if (bgmSource != null) bgmSource.volume = v;
    }
    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        // sfxSource.volume = v; // PlayOneShot 用参数更可靠
    }

    public void ForceMuteAllWind()
    {
        if (windSource1 != null) windSource1.volume = 0f;
        if (windSource2 != null) windSource2.volume = 0f;
        if (windSource3 != null) windSource3.volume = 0f;
    }
}