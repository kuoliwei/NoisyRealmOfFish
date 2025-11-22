using UnityEngine;
using System.Collections;

public class BGMController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float fadeDuration = 1f;

    [Header("最大音量限制")]
    [SerializeField, Range(0f, 1f)]
    private float maxVolume = 0.3f;   // ★ 你可以在 Inspector 設定最大音量

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // 確保 AudioSource 起始音量不會超過 maxVolume
        audioSource.volume = Mathf.Clamp(audioSource.volume, 0f, maxVolume);
    }

    /// <summary>
    /// 播放新的音樂 (自動淡入淡出)
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToClip(clip));
    }

    /// <summary>
    /// 停止音樂（不fade）
    /// </summary>
    public void StopBGM()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        audioSource.Stop();
    }

    private IEnumerator FadeToClip(AudioClip newClip)
    {
        float startVolume = audioSource.volume;
        float time = 0;

        // 淡出
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
            yield return null;
        }

        // 切換音樂
        audioSource.clip = newClip;

        if (newClip != null)
        {
            audioSource.Play();
        }

        // 淡入 (淡到 maxVolume，而不是 1f)
        time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, maxVolume, time / fadeDuration);
            yield return null;
        }

        audioSource.volume = maxVolume;  // 確保最終音量正確
        fadeRoutine = null;
    }

    /// <summary>
    /// 外部呼叫：淡出到 0（例如黑幕結尾）
    /// </summary>
    public void FadeOut(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeVolumeToZero(duration));
    }

    private IEnumerator FadeVolumeToZero(float duration)
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        fadeRoutine = null;
    }
}
