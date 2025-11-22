using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NoseRayProcessor;

public class NewExperienceFlowController : MonoBehaviour
{
    //[Header("Sun Intro – Next Experience Preview Images")]
    //[SerializeField] private Sprite fishIntroSprite;
    //[SerializeField] private Sprite calmWaterIntroSprite;

    [Header("SunIntro Page Materials")]
    [SerializeField] private Sprite transparentPageSprite; // 用於 page6/page7
    [SerializeField] private Sprite calmWaterIntroSprite;  // 用於 page6
    [SerializeField] private Sprite grayPageSprite;        // 用於 page7（或你需要的地方）

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
    [SerializeField] private GameObject flipHint;

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
    [SerializeField] private float fishDuration = 60f;

    [Header("Calm Water References")]
    [SerializeField] private StarSpawnerUI starSpawner;
    [SerializeField] private WaterWaveDualController waterWave;

    [Header("Book / SunIntro")]
    [SerializeField] private Book sunBook;
    [SerializeField] private AutoFlip sunBookAutoFlip;
    [SerializeField] private SwipeDetector swipeDetector;

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

    [Header("SunIntro 首次進場延遲")]
    [SerializeField] private float allowFlipDelay = 40f;

    Coroutine delayedEnableSunIntroSwipeCoroutine;

    [Header("CalmWater 無人自動結束延遲（秒）")]
    [SerializeField] private float calmWaterAutoEndDelay = 40f;
    private Coroutine calmWaterMonitorCoroutine = null;
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
        // 離開 CalmWater 時停止監控
        if (calmWaterMonitorCoroutine != null)
        {
            StopCoroutine(calmWaterMonitorCoroutine);
            calmWaterMonitorCoroutine = null;
        }
        if (delayedEnableSunIntroSwipeCoroutine != null)
        {
            StopCoroutine(delayedEnableSunIntroSwipeCoroutine);
            delayedEnableSunIntroSwipeCoroutine = null;
        }
        currentMode = mode;
        if (swipeDetector != null)
            swipeDetector.ActivateSunIntroSwipe(false);
        // 全部先關掉
        //if (book != null) book.SetActive(false);
        //if (bg != null) bg.SetActive(false);
        if (parentPanel != null) parentPanel.SetActive(false);
        if (mask != null) mask.SetActive(false);
        if (maskText != null) maskText.SetActive(false);
        if (flipHint != null) flipHint.SetActive(false);

        // 依模式開
        switch (mode)
        {
            case ExperienceMode.SunIntro:
                Debug.Log("[Flow] Entering SunIntro");
                Debug.Log($"[DEBUG] SwitchMode(SunIntro) 開始 → currentPage={sunBook.currentPage}");

                //// 1. 重設書頁
                //if (sunBook != null)
                //    sunBook.ResetToFirstPage();

                //// 2. 顯示書本 UI
                //if (book != null)
                //    book.SetActive(true);

                // 3. AutoFlip 必須重新啟用（這個很重要）
                if (sunBookAutoFlip != null)
                {
                    sunBookAutoFlip.enabled = true;
                    sunBookAutoFlip.ResetFlipState(); // 我下面會給你這個函式
                }

                // 4. 翻頁鎖定旗標重置
                isSunPageFlipping = false;

                //// 5. 設定下一段介紹頁（page4）
                //int nextIndex = flowIndex + 1;
                //if (nextIndex >= flowSequence.Length)
                //    nextIndex = 0;

                //ExperienceMode nextMode = flowSequence[nextIndex];

                //if (nextMode == ExperienceMode.NoisyFish && fishIntroSprite != null)
                //    sunBook.bookPages[4] = fishIntroSprite;
                //else if (nextMode == ExperienceMode.CalmWater && calmWaterIntroSprite != null)
                //    sunBook.bookPages[4] = calmWaterIntroSprite;
                // 只有 flowIndex == 0 才重置
                if (flowIndex == 0)
                {
                    if (sunBook != null)
                        sunBook.ResetToFirstPage();

                    if (bg != null) bg.SetActive(true);
                    // 第一次 SunIntro：延遲啟用提示與滑動偵測

                }

                // flowIndex == 2（第二次 SunIntro）
                else if (flowIndex == 2)
                {

                }
                // 立即強制刷新 UI
                //sunBook.RefreshPageVisual();
                // 6. 停止魚境相關行為
                if (fishSpawner != null)
                {
                    fishSpawner.ClearAll();
                    fishSpawner.enabled = false;
                }

                if (noseRayProcessor != null)
                    noseRayProcessor.DisableFishMode();

                if (noseRayProcessor != null)
                    noseRayProcessor.EnableNeutralMode();

                if (fishModeController != null)
                    fishModeController.ResetTimer();

                Debug.Log($"[DEBUG] SwitchMode(SunIntro) 結束 → currentPage={sunBook.currentPage}");

                // 7. SunIntro 的背景音樂
                bgmController.PlayBGM(sunIntroBGM);

                delayedEnableSunIntroSwipeCoroutine = StartCoroutine(DelayedEnableSunIntroSwipe());
                break;

            case ExperienceMode.NoisyFish:
                // UI
                //if (bg != null) bg.SetActive(true);
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
                // 啟動 CalmWater 人數監控
                if (calmWaterMonitorCoroutine != null)
                    StopCoroutine(calmWaterMonitorCoroutine);

                calmWaterMonitorCoroutine = StartCoroutine(CalmWaterUserMonitor());
                break;
        }

        Debug.Log($"[NewExperienceFlowController] Switched to mode: {mode}");
    }
    private IEnumerator DelayedEnableSunIntroSwipe()
    {
        // 延遲秒數來自 Inspector
        yield return new WaitForSeconds(allowFlipDelay);

        if (flipHint != null)
            flipHint.SetActive(true);

        if (swipeDetector != null)
            swipeDetector.ActivateSunIntroSwipe(true);

        Debug.Log($"[Flow] 首次 SunIntro 延遲 {allowFlipDelay}s → 啟用滑動與提示");
    }
    private IEnumerator CalmWaterUserMonitor()
    {
        float timer = 0f;

        //while (currentMode == ExperienceMode.CalmWater)
        while (true)
        {
            if (currentMode != ExperienceMode.CalmWater)
                yield break;
            int userCount = 0;

            if (noseRayProcessor != null)
                userCount = noseRayProcessor.PersonInRangeCount;

            if (userCount > 0)
            {
                // 有人 → 重置計時器
                timer = 0f;
            }
            else
            {
                // 無人 → 開始累積
                timer += Time.deltaTime;

                if (timer >= calmWaterAutoEndDelay)
                {
                    Debug.Log("[Flow] CalmWater 無人自動結束");
                    AutoEndCalmWater();
                    yield break;
                }
            }

            yield return null;
        }
    }
    private void AutoEndCalmWater()
    {
        Debug.Log("[Flow] CalmWater 無人自動結束 → 走標準結束流程 (CompleteCurrentExperience)");

        // 停止 CalmWater 無人監控協程
        if (calmWaterMonitorCoroutine != null)
        {
            StopCoroutine(calmWaterMonitorCoroutine);
            calmWaterMonitorCoroutine = null;
        }

        // 走通用結束流程：
        // - 由 EndingController 播放結尾（黑幕）
        // - OnAfterEndingReset() 做各模式的清場與重設
        // - PrepareNextSunIntroPages() 預先更新 SunIntro 的書頁並 ResetToFirstPage()
        // - flowIndex++，再 SwitchMode() 進入下一段
        CompleteCurrentExperience();
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
    public void TryFlipSunPage()
    {
        StopCoroutine(delayedEnableSunIntroSwipeCoroutine);
        // 只在 SunIntro 模式下允許翻頁
        if (currentMode != ExperienceMode.SunIntro)
            return;

        if (sunBook == null || sunBookAutoFlip == null)
            return;

        // 翻頁動畫中的期間不允許再翻
        if (isSunPageFlipping)
            return;

        if (flipHint != null)
            flipHint.SetActive(false);
        if (swipeDetector != null)
            swipeDetector.ActivateSunIntroSwipe(false);

        // 這一行如果你還有「冷卻時間」也可以保留，這邊先略過

        // 開始翻頁動畫
        sunBookAutoFlip.FlipRightPage();

        // 設定旗標：正在翻頁中
        isSunPageFlipping = true;

        if (flowIndex == 2)
        {
            // 啟動水波控制器（重設成初始狀態）
            if (waterWave != null)
                waterWave.ResetToInitial();
        }

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

        //// 這裡先用你目前的推測條件：翻三次後結束
        //// 你觀察到第三次翻頁完成會是「左5右6」
        //// 依 Book 的慣例，currentPage 通常會是左頁 index：也就是 5
        //// 如果實測 Log 出來不是 5，你可以把這個數字改成 Log 顯示的值
        //if (sunBook.currentPage >= 4)
        //{
        //    Debug.Log("[SunIntro] Reached last page group → Switch to next experience");

        //    // SunIntro 不走黑幕結尾，直接切下一個流程情境

        //    flowIndex++;
        //    if (flowIndex >= flowSequence.Length)
        //        flowIndex = 0;

        //    SwitchMode(flowSequence[flowIndex]);
        //}

        Debug.Log($"[FlowController] WaitForSunPageFlipEnd() executed → new currentPage={sunBook.currentPage}, flowIndex={flowIndex}");
        // 第一次 SunIntro：翻到 page4（currentPage == 4）→ 進魚境
        if (flowIndex == 0 && sunBook.currentPage == 4)
        {
            StopCoroutine(delayedEnableSunIntroSwipeCoroutine);
            if (flipHint != null)
                flipHint.SetActive(false);
            if (swipeDetector != null)
                swipeDetector.ActivateSunIntroSwipe(false);
            Debug.Log($"[FlowController] WaitForSunPageFlipEnd() executed → new currentPage={sunBook.currentPage}, flowIndex={flowIndex}");

            StartCoroutine(GoNextAfterAnimation());
            yield break;
        }

        // 第二次 SunIntro：翻到 page6（currentPage == 6）→ 進靜水映月
        if (flowIndex == 2 && sunBook.currentPage == 6)
        {
            StopCoroutine(delayedEnableSunIntroSwipeCoroutine);
            if (flipHint != null)
                flipHint.SetActive(false);
            if (swipeDetector != null)
                swipeDetector.ActivateSunIntroSwipe(false);
            Debug.Log($"[FlowController] WaitForSunPageFlipEnd() executed → new currentPage={sunBook.currentPage}, flowIndex={flowIndex}");
            StartCoroutine(GoNextAfterAnimation());
            yield break;
        }

        delayedEnableSunIntroSwipeCoroutine = StartCoroutine(DelayedEnableSunIntroSwipe());
        //// 翻頁完成後 → 若仍在 SunIntro 且還能翻 → 顯示 flipHint
        //if (currentMode == ExperienceMode.SunIntro)
        //{
        //    bool canFlip = sunBook.currentPage < 6; // flowIndex=0 要翻2次；flowIndex=2 要翻1次

        //    if (flipHint != null)
        //        flipHint.SetActive(canFlip);
        //}
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
    private void GoToNextExperience()
    {
        Debug.Log("[SunIntro] Page target reached → switching experience");

        // flowIndex 前進
        flowIndex++;
        if (flowIndex >= flowSequence.Length)
            flowIndex = 0;

        // 切換模式
        SwitchMode(flowSequence[flowIndex]);
    }
    private IEnumerator GoNextAfterAnimation()
    {
        // 讓 AutoFlip 的最後 TweenForward / UpdateSprites 完整跑完
        if (sunBookAutoFlip != null)
            yield return new WaitForSeconds(sunBookAutoFlip.PageFlipTime);

        // 再正式切到下一段
        GoToNextExperience();
    }
    // ============================================================
    //  公開方法：預先更新下一輪 SunIntro 的 page6 / page7 素材
    //  在黑幕 Fade-In 完成後由 ExperienceEndingController 呼叫
    // ============================================================
    public void PrepareNextSunIntroPages()
    {
        // 取得下一個模式的索引
        int next = flowIndex + 1;
        if (next >= flowSequence.Length)
            next = 0;

        ExperienceMode nextMode = flowSequence[next];

        if (sunBook == null)
        {
            Debug.LogWarning("[Flow] sunBook 未指定 → 無法預先更新書頁素材");
            return;
        }
        Debug.Log($"[FlowController] PrepareNextSunIntroPages() executed → nextMode={nextMode}, flowIndex={flowIndex}");
        // 根據下一模式決定 page6 / page7 的圖
        if (nextMode == ExperienceMode.SunIntro && flowIndex == 3)
        {
            // 第一次（魚境前的 SunIntro）
            sunBook.bookPages[6] = transparentPageSprite;
            sunBook.bookPages[7] = transparentPageSprite;
            // 重設書頁
            if (sunBook != null)
                sunBook.ResetToFirstPage();
        }
        else if (nextMode == ExperienceMode.SunIntro && flowIndex == 1)
        {
            // 第二次（靜水映月前的 SunIntro）
            sunBook.bookPages[6] = calmWaterIntroSprite;
            sunBook.bookPages[7] = grayPageSprite;
        }

        // 更新畫面
        sunBook.RefreshPageVisual();

        Debug.Log($"[Flow] 預先更新下一輪 SunIntro 書頁 → nextMode={nextMode}, 已刷新 page6/page7");
    }

}
