using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewExperienceFlowController : MonoBehaviour
{
    public enum ExperienceMode
    {
        SunIntro,     // 向陽簡介
        NoisyFish,    // 喧囂魚境
        CalmWater     // 靜水映月
    }

    [Header("Start Mode")]
    [SerializeField] private ExperienceMode startMode = ExperienceMode.SunIntro;
    private ExperienceMode currentMode;

    [Header("UI Roots / Elements")]
    [SerializeField] private GameObject book;
    [SerializeField] private GameObject bg;
    [SerializeField] private GameObject parentPanel;
    [SerializeField] private GameObject mask;
    [SerializeField] private GameObject maskText;

    [Header("Ending")]
    [SerializeField] private ExperienceEndingController endingController;
    [SerializeField, TextArea] private string sunIntroEndingText;
    [SerializeField, TextArea] private string noisyFishEndingText;
    [SerializeField, TextArea] private string calmWaterEndingText;

    [Header("Fish Mode References")]
    [SerializeField] private FishSpawnerUI fishSpawner;
    [SerializeField] private NoseRayProcessor noseRayProcessor;
    [SerializeField] private HandRayProcessor handRayProcessor;
    [SerializeField] private FishModeController fishModeController;

    [Header("Fish Mode Settings")]
    [SerializeField] private float fishDuration = 30f;

    [Header("Calm Water References")]
    [SerializeField] private StarSpawnerUI starSpawner;
    [SerializeField] private WaterWaveDualController waterWave;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip sunIntroBGM;
    [SerializeField] private AudioClip noisyFishBGM;
    [SerializeField] private AudioClip calmWaterBGM;

    [SerializeField] private float bgmFadeDuration = 1.2f;

    private Coroutine bgmRoutine;
    private void Start()
    {
        SwitchMode(startMode);
    }

    public void SwitchMode(ExperienceMode mode)
    {
        currentMode = mode;

        // 全部先關掉
        if (book != null) book.SetActive(false);
        if (bg != null) bg.SetActive(false);
        if (parentPanel != null) parentPanel.SetActive(false);
        if (mask != null) mask.SetActive(false);
        if (maskText != null) maskText.SetActive(false);

        // 依模式開
        switch (mode)
        {
            case ExperienceMode.SunIntro:
                // 共用：離開魚境時停止所有魚境行為
                if (fishSpawner != null)
                {
                    fishSpawner.ClearAll();
                    fishSpawner.enabled = false;
                }

                if (noseRayProcessor != null)
                    noseRayProcessor.DisableFishMode();

                if (fishModeController != null)
                    fishModeController.ResetTimer();
                if (book != null) book.SetActive(true);
                PlayBGM(sunIntroBGM);

                break;

            case ExperienceMode.NoisyFish:
                // UI
                if (bg != null) bg.SetActive(true);
                if (parentPanel != null) parentPanel.SetActive(true);
                if (mask != null) mask.SetActive(true);
                if (maskText != null) maskText.SetActive(true);

                // --- Fish logic ---
                if (fishSpawner != null)
                {
                    fishSpawner.ClearAll();
                    fishSpawner.enabled = true;
                }

                if (noseRayProcessor != null)
                    noseRayProcessor.EnableFishMode();

                if (handRayProcessor != null)
                    handRayProcessor.enabled = false; // 魚境不需要手部擊星星

                if (fishModeController != null)
                    fishModeController.StartTimer(fishDuration, OnFishTimeout);
                PlayBGM(noisyFishBGM);

                break;

            case ExperienceMode.CalmWater:
                // ---- 離開魚境（保持原本的） ----
                if (fishSpawner != null)
                {
                    fishSpawner.ClearAll();
                    fishSpawner.enabled = false;
                }

                if (noseRayProcessor != null)
                    noseRayProcessor.DisableFishMode();

                if (fishModeController != null)
                    fishModeController.ResetTimer();

                // ---- 啟動靜水映月 ----

                // 啟用星星
                if (starSpawner != null)
                {
                    starSpawner.ClearAll();
                    starSpawner.enabled = true;
                }

                // 啟動星星擊中的手部輸入
                if (handRayProcessor != null)
                    handRayProcessor.enabled = true;

                // 鼻子模式切到「星星模式」
                if (noseRayProcessor != null)
                    noseRayProcessor.EnableStarMode();

                // 啟動水波控制器（重設成初始狀態）
                if (waterWave != null)
                    waterWave.ResetToInitial();

                // UI 顯示
                if (parentPanel != null) parentPanel.SetActive(true);
                if (mask != null) mask.SetActive(true);
                if (maskText != null) maskText.SetActive(true);
                PlayBGM(calmWaterBGM);

                break;
        }

        Debug.Log($"[NewExperienceFlowController] Switched to mode: {mode}");
    }

    // 對外切換 API
    public void EnterSunIntro() => SwitchMode(ExperienceMode.SunIntro);
    public void EnterNoisyFish() => SwitchMode(ExperienceMode.NoisyFish);
    public void EnterCalmWater() => SwitchMode(ExperienceMode.CalmWater);

    /// <summary>
    /// 當前情境完成時呼叫，會啟動通用的結尾流程。
    /// 目前先只處理結尾演出；各模式的重設邏輯會在下一步補上。
    /// </summary>
    public void CompleteCurrentExperience()
    {
        if (endingController == null)
        {
            Debug.LogWarning("[NewExperienceFlowController] EndingController 尚未指定。");
            return;
        }

        string endText = "";
        switch (currentMode)
        {
            case ExperienceMode.SunIntro:
                endText = sunIntroEndingText;
                break;
            case ExperienceMode.NoisyFish:
                endText = noisyFishEndingText;
                break;
            case ExperienceMode.CalmWater:
                endText = calmWaterEndingText;
                break;
        }

        // 暫時只把 Reset 回呼留空，下一步我們會依模式填進對應的 Reset 方法
        endingController.PlayEnding(endText, OnAfterEndingReset);
    }

    /// <summary>
    /// 這個方法會在黑幕仍存在時被呼叫，用來重設場景。
    /// 目前先留空，下一步會依模式實作（清魚、清星星、重設水波等）。
    /// </summary>
    private void OnAfterEndingReset()
    {
        if (currentMode == ExperienceMode.NoisyFish)
        {
            // 清空魚境所有資料
            if (fishSpawner != null)
                fishSpawner.ClearAll();

            if (fishModeController != null)
                fishModeController.ResetTimer();

            if (noseRayProcessor != null)
                noseRayProcessor.DisableFishMode();
        }
        if (currentMode == ExperienceMode.CalmWater)
        {
            // 清星星
            if (starSpawner != null)
                starSpawner.ClearAll();

            // 停止手部事件
            if (handRayProcessor != null)
                handRayProcessor.enabled = false;

            // 鼻子回到預設（星星用不到魚模式）
            if (noseRayProcessor != null)
                noseRayProcessor.EnableStarMode();

            // 重設水波
            if (waterWave != null)
                waterWave.ResetToInitial();
        }

        Debug.Log($"[NewExperienceFlowController] OnAfterEndingReset for mode: {currentMode}");

        // 下一步我們會在這裡依照 currentMode 實作：
        // - 清除魚與詩句
        // - 清除星星
        // - 重設水波
        // - 切回起始模式或等待外部指令
    }
    private void OnFishTimeout()
    {
        Debug.Log("[Flow] Fish mode timed out → Ending");

        CompleteCurrentExperience();
    }
    public void OnWaterCompleted()
    {
        Debug.Log("[Flow] Calm Water completed → Ending");
        CompleteCurrentExperience();
    }

    private void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;
        if (clip == null) return;

        if (bgmRoutine != null)
            StopCoroutine(bgmRoutine);

        bgmRoutine = StartCoroutine(FadeToClip(clip));
    }

    private IEnumerator FadeToClip(AudioClip newClip)
    {
        float t = 0f;
        float startVolume = bgmSource.volume;

        // Fade out
        while (t < bgmFadeDuration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / bgmFadeDuration);
            yield return null;
        }

        // Swap clip
        bgmSource.clip = newClip;
        bgmSource.Play();

        // Fade in
        t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVolume, t / bgmFadeDuration);
            yield return null;
        }

        bgmSource.volume = startVolume;
    }
}
