using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NoseRayProcessor;

public class NewExperienceFlowController : MonoBehaviour
{
    [Header("Sun Intro – Next Experience Preview Images")]
    [SerializeField] private Sprite fishIntroSprite;
    [SerializeField] private Sprite calmWaterIntroSprite;

    // ===== Flow Sequence =====
    private ExperienceMode[] flowSequence = new ExperienceMode[]
    {
    ExperienceMode.SunIntro,
    ExperienceMode.NoisyFish,
    ExperienceMode.SunIntro,
    ExperienceMode.CalmWater,
    };

    private int flowIndex = 0;

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

    [Header("Book / SunIntro")]
    [SerializeField] private Book sunBook;
    [SerializeField] private AutoFlip sunBookAutoFlip;

    // 翻頁時是否在動畫中
    private bool isSunPageFlipping = false;

    // 翻頁冷卻
    private float sunFlipCooldown = 0f;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip sunIntroBGM;
    [SerializeField] private AudioClip noisyFishBGM;
    [SerializeField] private AudioClip calmWaterBGM;

    [SerializeField] private float bgmFadeDuration = 1.2f;

    private Coroutine bgmRoutine;

    [SerializeField] private BGMController bgmController;

    private void Start()
    {
        flowIndex = 0;
        SwitchMode(flowSequence[flowIndex]);
    }
    private void Update()
    {
        // 減少冷卻
        if (sunFlipCooldown > 0)
            sunFlipCooldown -= Time.deltaTime;

        // 按空白鍵測試翻頁
        if (Input.GetKeyDown(KeyCode.Space))
            TryFlipSunPage();

        //// 熱切換：按 F 進喧囂魚境
        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    SwitchMode(ExperienceMode.NoisyFish);
        //}

        //// 熱切換：按 W 進靜水映月
        //if (Input.GetKeyDown(KeyCode.W))
        //{
        //    SwitchMode(ExperienceMode.CalmWater);
        //}
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
                Debug.Log("[Flow] Entering SunIntro");

                // 1. 重設書頁
                if (sunBook != null)
                    sunBook.ResetToFirstPage();

                // 2. 顯示書本 UI
                if (book != null)
                    book.SetActive(true);

                // 3. ★★ AutoFlip 必須重新啟用（這個很重要）
                if (sunBookAutoFlip != null)
                {
                    sunBookAutoFlip.enabled = true;
                    sunBookAutoFlip.ResetFlipState(); // 我下面會給你這個函式
                }

                // 4. ★★ 翻頁鎖定旗標重置
                isSunPageFlipping = false;

                // 5. 設定下一段介紹頁（page4）
                int nextIndex = flowIndex + 1;
                if (nextIndex >= flowSequence.Length)
                    nextIndex = 0;

                ExperienceMode nextMode = flowSequence[nextIndex];

                if (nextMode == ExperienceMode.NoisyFish && fishIntroSprite != null)
                    sunBook.bookPages[4] = fishIntroSprite;
                else if (nextMode == ExperienceMode.CalmWater && calmWaterIntroSprite != null)
                    sunBook.bookPages[4] = calmWaterIntroSprite;

                // 6. 停止魚境相關行為
                if (fishSpawner != null)
                {
                    fishSpawner.ClearAll();
                    fishSpawner.enabled = false;
                }

                if (noseRayProcessor != null)
                    noseRayProcessor.DisableFishMode();

                if (fishModeController != null)
                    fishModeController.ResetTimer();

                // 7. SunIntro 的背景音樂
                bgmController.PlayBGM(sunIntroBGM);
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
                bgmController.PlayBGM(noisyFishBGM);

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
                bgmController.PlayBGM(calmWaterBGM);

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
            if (fishSpawner != null)
                fishSpawner.ClearAll();
            if (fishModeController != null)
                fishModeController.ResetTimer();
            if (noseRayProcessor != null)
                noseRayProcessor.EnableNeutralMode();
        }

        if (currentMode == ExperienceMode.CalmWater)
        {
            if (starSpawner != null)
            {
                starSpawner.ClearAll();
                starSpawner.enabled = false;
            }

            if (handRayProcessor != null)
                handRayProcessor.enabled = false;

            if (waterWave != null)
                waterWave.ResetToInitial();

            if (noseRayProcessor != null)
                noseRayProcessor.EnableNeutralMode();
        }
        // ====== FLOW: 前往下一段 ======
        flowIndex++;
        if (flowIndex >= flowSequence.Length)
            flowIndex = 0;

        // 加一點點延遲，避免同一幀就切換
        StartCoroutine(SwitchNextSequence());
        Debug.Log("[Flow] Experience finished → Enter Next Mode");
    }
    private IEnumerator SwitchNextSequence()
    {
        yield return new WaitForSeconds(0.5f);
        SwitchMode(flowSequence[flowIndex]);
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

        bgmSource.volume = 1f;

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
    private void TryFlipSunPage()
    {
        // 只在 SunIntro 模式下允許翻頁
        if (currentMode != ExperienceMode.SunIntro)
            return;

        if (sunBook == null || sunBookAutoFlip == null)
            return;

        // 翻頁動畫中的期間不允許再翻
        if (isSunPageFlipping)
            return;

        // 這一行如果你還有「冷卻時間」也可以保留，這邊先略過

        // 開始翻頁動畫
        sunBookAutoFlip.FlipRightPage();

        // 設定旗標：正在翻頁中
        isSunPageFlipping = true;

        // 啟動：等 PageFlipTime 秒後再檢查是否要切換情境
        StartCoroutine(WaitForSunPageFlipEnd());
    }
    private IEnumerator WaitForSunPageFlipEnd()
    {
        if (sunBookAutoFlip == null || sunBook == null)
            yield break;

        // 等待翻頁動畫撥完（你 AutoFlip 上面的 PageFlipTime）
        yield return new WaitForSeconds(sunBookAutoFlip.PageFlipTime);

        // 翻頁動畫已經完成
        isSunPageFlipping = false;

        // 觀察一下 currentPage 的變化，先印出來一次
        Debug.Log($"[SunIntro] After flip, sunBook.currentPage = {sunBook.currentPage}");

        // 這裡先用你目前的推測條件：翻三次後結束
        // 你觀察到第三次翻頁完成會是「左5右6」
        // 依 Book 的慣例，currentPage 通常會是左頁 index：也就是 5
        // 如果實測 Log 出來不是 5，你可以把這個數字改成 Log 顯示的值
        if (sunBook.currentPage >= 4)
        {
            Debug.Log("[SunIntro] Reached last page group → Switch to next experience");

            // SunIntro 不走黑幕結尾，直接切下一個流程情境

            flowIndex++;
            if (flowIndex >= flowSequence.Length)
                flowIndex = 0;

            SwitchMode(flowSequence[flowIndex]);
        }
        //// 若翻完 -> 是最後一頁 → 自動重置
        //if (sunBook.currentPage >= sunBook.TotalPageCount - 1)
        //{
        //    sunBook.ResetToFirstPage();
        //}
    }
    private ExperienceMode GetNextFlowMode()
    {
        int next = flowIndex + 1;
        if (next >= flowSequence.Length)
            next = 0;
        return flowSequence[next];
    }
}
