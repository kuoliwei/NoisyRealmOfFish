using System.Collections;
using UnityEngine;

public class ExperienceFlowController : MonoBehaviour
{
    public enum ExperienceMode
    {
        Moon,   // 靜水映月（觸碰星星達成）
        Fish    // 喧囂魚境（用時間限制）
    }

    [Header("Mode")]
    [SerializeField] private ExperienceMode mode = ExperienceMode.Moon;

    [Header("Mask Text Content")]
    [SerializeField, TextArea] private string moonEndingText;
    [SerializeField, TextArea] private string fishEndingText;

    [Header("References")]
    [SerializeField] private WaterWaveDualController waterWaveController;
    [SerializeField] private MaskPanelController maskPanel;

    [Header("Timings")]
    [SerializeField] private float idleAfterCompletion = 5f;
    [SerializeField] private float textVisibleDuration = 2.5f;

    // === 新增：喧囂魚境用的體驗時間 ===
    [Header("Fish Mode Settings")]
    [SerializeField] private float fishExperienceDuration = 30f;
    private float fishTimer = 0f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool hasCompleted = false;

    private void Start()
    {
        InitializeExperience();
    }

    public void InitializeExperience()
    {
        hasCompleted = false;

        if (waterWaveController != null)
            waterWaveController.ResetToInitial();

        if (maskPanel != null)
            maskPanel.SetAlpha(0f);

        // === 若是魚境模式，啟動計時 ===
        if (mode == ExperienceMode.Fish)
            fishTimer = fishExperienceDuration;
    }

    // ======================
    // 新增：Fish 模式倒數
    // ======================
    private void Update()
    {
        if (mode == ExperienceMode.Fish && !hasCompleted)
        {
            fishTimer -= Time.deltaTime;

            if (fishTimer <= 0f)
            {
                OnExperienceCompleted();
            }
        }
    }

    // 靜水映月：由 WaterWaveDualController 調用
    public void OnExperienceCompleted()
    {
        if (hasCompleted) return;
        hasCompleted = true;

        StartCoroutine(PlayEndSequence());
    }

    private IEnumerator PlayEndSequence()
    {
        // === 根據模式設定不同的文字內容 ===
        if (maskPanel != null && maskPanel.MaskText != null)
        {
            switch (mode)
            {
                case ExperienceMode.Moon:
                    maskPanel.MaskText.text = moonEndingText;
                    break;

                case ExperienceMode.Fish:
                    maskPanel.MaskText.text = fishEndingText;
                    break;
            }
        }
        Debug.Log("[FlowController] 結尾流程開始");
        yield return new WaitForSeconds(idleAfterCompletion);

        // 音樂淡出
        StartCoroutine(FadeAudio(1, 0, maskPanel.FadeDuration * 4 + textVisibleDuration));

        // 1. 淡入 Image
        if (maskPanel != null)
            maskPanel.FadeInImageOnly();

        yield return new WaitForSeconds(maskPanel.FadeDuration);

        // 2. 淡入 Text
        if (maskPanel != null)
            maskPanel.FadeInTextOnly();

        yield return new WaitForSeconds(maskPanel.FadeDuration);
        yield return new WaitForSeconds(textVisibleDuration);

        // 3. 淡出 Text
        if (maskPanel != null)
            maskPanel.FadeOutTextOnly();

        yield return new WaitForSeconds(maskPanel.FadeDuration);

        // 4. 重設應用
        InitializeExperience();

        // 5. 淡出 Image
        if (maskPanel != null)
            maskPanel.FadeOutImageOnly();

        audioSource.Stop();
        yield return new WaitForSeconds(maskPanel.FadeDuration);

        yield return new WaitUntil(() => !audioSource.isPlaying);

        audioSource.Play();
        audioSource.volume = 1;
    }

    private IEnumerator FadeAudio(float start, float end, float fadeDuration)
    {
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
