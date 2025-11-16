using System;
using System.Collections;
using UnityEngine;

public class ExperienceEndingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MaskPanelController maskPanel;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private BGMController bgmController;

    [Header("Timings")]
    [SerializeField] private float idleAfterCompletion = 5f;
    [SerializeField] private float textVisibleDuration = 2.5f;

    private Coroutine currentRoutine;

    /// <summary>
    /// 播放結尾流程。
    /// endText : 要顯示的結尾文字
    /// onReset : 在文字淡出後、黑幕仍存在時呼叫，用來重設場景或模式。
    /// </summary>
    public void PlayEnding(string endText, Action onReset)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PlayEndSequence(endText, onReset));
    }

    private IEnumerator PlayEndSequence(string endText, Action onReset)
    {
        if (maskPanel != null && maskPanel.MaskText != null)
        {
            maskPanel.MaskText.text = endText;
        }

        Debug.Log("[ExperienceEndingController] 結尾流程開始");

        // 0. 先確保一開始是全透明
        if (maskPanel != null)
            maskPanel.SetAlpha(0f);

        // 1. 體驗完成後先停一小段時間
        yield return new WaitForSeconds(idleAfterCompletion);

        // 2. 開始淡出音樂（總長度：Image + Text 的淡入淡出時間）
        float totalFadeDuration = 0f;
        if (maskPanel != null)
            totalFadeDuration = maskPanel.FadeDuration * 4 + textVisibleDuration;

        if (bgmController != null)
            bgmController.FadeOut(totalFadeDuration);

        // 3. 淡入 Image（黑幕）
        if (maskPanel != null)
            maskPanel.FadeInImageOnly();

        yield return new WaitForSeconds(maskPanel.FadeDuration);

        // 4. 淡入 Text
        if (maskPanel != null)
            maskPanel.FadeInTextOnly();

        yield return new WaitForSeconds(maskPanel.FadeDuration);
        yield return new WaitForSeconds(textVisibleDuration);

        // 5. 淡出 Text
        if (maskPanel != null)
            maskPanel.FadeOutTextOnly();

        yield return new WaitForSeconds(maskPanel.FadeDuration);

        // 6. 呼叫外部重設邏輯（此時黑幕仍在）
        onReset?.Invoke();

        // 7. 淡出 Image（讓畫面回來）
        if (maskPanel != null)
            maskPanel.FadeOutImageOnly();

        // do not stop music here anymore

        currentRoutine = null;
    }

    private IEnumerator FadeAudio(float start, float end, float fadeDuration)
    {
        if (audioSource == null || fadeDuration <= 0f)
            yield break;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float v = Mathf.Lerp(start, end, t);
            audioSource.volume = v;
            yield return null;
        }
    }
}
