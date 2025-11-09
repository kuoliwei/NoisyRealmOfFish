using System.Collections;
using UnityEngine;

/// <summary>
/// 控制整個互動體驗流程：
//—初始化 → 觸碰 → 結尾動畫 → 重置。
/// </summary>
public class ExperienceFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaterWaveDualController waterWaveController;
    [SerializeField] private MaskPanelController maskPanel;

    [Header("Timings")]
    [SerializeField] private float idleAfterCompletion = 5f;
    [SerializeField] private float textVisibleDuration = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool hasCompleted = false;

    private void Start()
    {
        InitializeExperience();
    }

    /// <summary>
    /// 重設整個體驗至初始狀態
    /// </summary>
    public void InitializeExperience()
    {
        hasCompleted = false;
        if (waterWaveController != null)
            waterWaveController.ResetToInitial();

        if (maskPanel != null)
            maskPanel.SetAlpha(0f);
    }

    /// <summary>
    /// 由 WaterWaveDualController 呼叫當觸碰達到 totalTouchesRequired。
    /// </summary>
    public void OnExperienceCompleted()
    {
        if (hasCompleted) return;
        hasCompleted = true;

        StartCoroutine(PlayEndSequence());
    }

    private IEnumerator PlayEndSequence()
    {
        Debug.Log("[FlowController] 結尾流程開始");
        yield return new WaitForSeconds(idleAfterCompletion);

        // 音樂開始淡出
        StartCoroutine(FadeAudio(1, 0, maskPanel.FadeDuration * 4 + textVisibleDuration));

        // 1. 遮罩圖片淡入
        if (maskPanel != null)
            maskPanel.FadeInImageOnly();

        //yield return new WaitForSeconds(maskPanelFadeDuration());
        yield return new WaitForSeconds(maskPanel.FadeDuration);
        // 2. 等一段時間後淡入文字
        if (maskPanel != null)
            maskPanel.FadeInTextOnly();
        yield return new WaitForSeconds(maskPanel.FadeDuration);
        yield return new WaitForSeconds(textVisibleDuration);

        // 3. 淡出文字
        if (maskPanel != null)
            maskPanel.FadeOutTextOnly();
        yield return new WaitForSeconds(maskPanel.FadeDuration);
        // 4. 重設應用
        InitializeExperience();

        // 5. 最後淡出整體遮罩
        if (maskPanel != null)
            maskPanel.FadeOutImageOnly();
        audioSource.Stop();
        yield return new WaitForSeconds(maskPanel.FadeDuration);
        Debug.Log("[FlowController] 體驗重置完成");

        yield return new WaitUntil(() => !audioSource.isPlaying);
        audioSource.Play();
        audioSource.volume = 1;
    }
    private IEnumerator FadeAudio(float start, float end, float fadeDuration)
    {
        float timer = 0f;
        float volume = audioSource.volume;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float v = Mathf.Lerp(start, end, t);
            audioSource.volume = v;
            yield return null;
        }
    }
    // 幫助取得 MaskPanel 的 fade 時長
    //private float maskPanelFadeDuration()
    //{
    //    var type = maskPanel.GetType();
    //    var field = type.GetField("fadeDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    //    return field != null ? (float)field.GetValue(maskPanel) : 1.5f;
    //}
}
