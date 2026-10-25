using UnityEngine;

public class PersistentAudioManager : MonoBehaviour
{
    public static PersistentAudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource chaseSource; // 针对你当前项目的 Apex 追逐音乐

    private void Awake()
    {
        // 1. 单例模式检查：确保全局只有一个 AudioManager 实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 如果场景中已有重复的 Manager，直接销毁新的
            return;
        }

        Instance = this;

        // 2. 核心：跨场景不销毁此 GameObject
        DontDestroyOnLoad(gameObject);

        PlayBGM(bgmSource.clip);
    }

    // 可以在这里封装播放、淡入淡出（Crossfade）等方法
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // 已经在播放同一首就不重播

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 针对首杀变身时的音乐淡入淡出（Crossfade）方法
    public void CrossfadeToChase(AudioClip chaseClip, float duration = 1.0f)
    {
        StartCoroutine(CrossfadeRoutine(chaseClip, duration));
    }

    private System.Collections.IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        chaseSource.clip = newClip;
        chaseSource.volume = 0f;
        chaseSource.Play();

        float timer = 0f;
        float startVol = bgmSource.volume;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // 建议用 unscaledDeltaTime，防止暂停影响淡入淡出
            float progress = timer / duration;

            bgmSource.volume = Mathf.Lerp(startVol, 0f, progress);
            chaseSource.volume = Mathf.Lerp(0f, 1f, progress);

            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVol; // 恢复原始音量备用
    }
}