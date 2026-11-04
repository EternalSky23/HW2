using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("BGM Setup")]
    [SerializeField] private AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private AudioSource audioSource;

    void Awake()
    {
        // 確保跨場景只有一個 AudioManager 存在
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 場景切換時不銷毀此物件

            // 初始化並播放 BGM
            audioSource = GetComponent<AudioSource>();
            SetupAudioSource();
            PlayBGM();
        }
        else if (instance != this)
        {
            // 如果切換場景後又讀到新的 AudioManager，立刻銷毀自己，防止兩首音樂重疊
            Destroy(gameObject);
        }
    }

    private void SetupAudioSource()
    {
        audioSource.clip = bgmClip;
        audioSource.loop = true;          // 設定循環播放
        audioSource.playOnAwake = false;
        audioSource.volume = bgmVolume;
    }

    public void PlayBGM()
    {
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // 調整音量用
    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = bgmVolume;
        }
    }
}
