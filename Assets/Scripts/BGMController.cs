using UnityEngine;
using System.Collections;

public class BGMController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float fadeDuration = 1f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
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

        // 淡入
        time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }

        fadeRoutine = null;
    }
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
