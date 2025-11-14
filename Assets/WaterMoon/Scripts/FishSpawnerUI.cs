using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishSpawnerUI : MonoBehaviour
{
    [Header("Canvas References (與星星一致)")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform parentRect;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera uiCamera;

    [Header("Fish Prefab")]
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private bool invertDirection = false;

    private readonly Dictionary<int, RectTransform> fishByPerson = new();
    private readonly Dictionary<int, Vector2> lastPosByPerson = new();

    public RectTransform ParentRect => parentRect;
    public Canvas GetTargetCanvas() => targetCanvas;
    public Camera GetUICamera() => uiCamera;

    // ===== 詩句設定（純 UI 層級控制） =====
    [Header("Verse Settings (UI Only)")]
    [SerializeField] private GameObject verseTextPrefab;   // 詩句 Text (TMP_Text 或 Text)
    [SerializeField, TextArea] private string[] verses;    // 詩句清單

    // ===== 每人詩句數量上限 =====
    [Header("Verse Limits")]
    //[SerializeField, Min(1)] private int maxVersesPerPerson = 3;
    private readonly Dictionary<int, List<GameObject>> versesByPerson = new();

    [Header("Verse Fade Settings")]
    [SerializeField] private float fadeOutDuration = 2f;   // 淡出秒數

    // 每句詩的相對偏移量（人ID → Verse物件 → X偏移）
    //private readonly Dictionary<int, Dictionary<GameObject, float>> verseOffsetX = new();
    private readonly Dictionary<int, Dictionary<GameObject, Vector2>> verseOffset = new();

    [Header("Fish Turn Smoothing")]
    [SerializeField, Range(0f, 1f)]
    private float smoothFactor = 0.2f;  // 低通濾波係數（0.05~0.3 都很好）

    private readonly Dictionary<int, float> smoothXByPerson = new();
    private readonly Dictionary<int, float> lastFlipTime = new();
    // ===== 魚生成 / 更新 =====
    public void SpawnOrUpdateFish(int personId, Vector2 canvasPos)
    {
        if (fishPrefab == null || parentRect == null) return;

        // ===== 如果是新的人，建立新的魚 =====
        if (!fishByPerson.TryGetValue(personId, out var rt) || rt == null)
        {
            var go = Instantiate(fishPrefab, parentRect);
            rt = go.transform as RectTransform;
            fishByPerson[personId] = rt;

            rt.anchoredPosition = canvasPos;
            rt.localRotation = Quaternion.identity;
            lastPosByPerson[personId] = canvasPos;

            // 初始化平滑資料
            smoothXByPerson[personId] = canvasPos.x;

            return;
        }

        // ===== 老人更新位置 =====
        Vector2 prev = lastPosByPerson.TryGetValue(personId, out var lp) ? lp : rt.anchoredPosition;
        rt.anchoredPosition = canvasPos;

        if (!lastPosByPerson.ContainsKey(personId))
        {
            lastPosByPerson[personId] = canvasPos;
            smoothXByPerson[personId] = canvasPos.x;
            return;
        }

        float prevX = prev.x;
        float currX = canvasPos.x;

        // ============================================================
        // 1. 取得上一幀平滑值（如果沒有就用 prevX）
        // ============================================================
        if (!smoothXByPerson.TryGetValue(personId, out float prevSmoothedX))
            prevSmoothedX = prevX;

        // ============================================================
        // 2. 用低通濾波平滑 X 訊號
        // ============================================================
        float smoothedX = Mathf.Lerp(prevSmoothedX, currX, smoothFactor);
        smoothXByPerson[personId] = smoothedX;

        //// ============================================================
        //// 3. 用平滑後位移判斷方向
        //// ============================================================
        //float delta = smoothedX - prevSmoothedX;

        //// 避免在 0 附近的小抖動造成方向翻來翻去
        //if (Mathf.Abs(delta) > 0.01f)
        //{
        //    bool movingRight = delta > 0f;

        //    // Inspector 反向開關（取決於你的魚素材朝向）
        //    if (invertDirection)
        //        movingRight = !movingRight;

        //    float targetScaleX = Mathf.Abs(rt.localScale.x) * (movingRight ? 1f : -1f);
        //    rt.localScale = new Vector3(targetScaleX, rt.localScale.y, rt.localScale.z);
        //}

        // ============================================================
        // 3. 改良版方向判斷（完全解決左右搖頭問題）
        // ============================================================

        float delta = smoothedX - prevSmoothedX;

        // 方向變化需要足夠幅度才允許翻轉
        float directionThreshold = 0.12f;    // 建議 0.10~0.15 之間

        // 翻轉冷卻：翻轉後至少等待一段時間才可再次翻面
        float flipCooldown = 0.25f;          // 越大越不敏感

        // 初始化翻轉時間
        if (!lastFlipTime.ContainsKey(personId))
            lastFlipTime[personId] = Time.time;

        bool inCooldown = (Time.time - lastFlipTime[personId]) < flipCooldown;

        // 目前方向
        bool facingRight = rt.localScale.x > 0;

        // 預測方向：由 delta 判斷
        bool wantRight = delta > 0;

        // invertDirection 支援
        if (invertDirection)
            wantRight = !wantRight;

        // 如果方向相同 → 不需要翻轉
        if (wantRight == facingRight)
        {
            // OK：維持方向，不翻面
        }
        else
        {
            // 若方向不同：檢查是否超過方向閾值
            if (Mathf.Abs(delta) > directionThreshold && !inCooldown)
            {
                // 符合條件 → 真正翻轉
                float targetScaleX = Mathf.Abs(rt.localScale.x) * (wantRight ? 1f : -1f);
                rt.localScale = new Vector3(targetScaleX, rt.localScale.y, rt.localScale.z);

                lastFlipTime[personId] = Time.time;
            }
        }


        // ============================================================
        // 4. 計算 X 速度提供給詩句動畫
        // ============================================================
        float deltaX = Mathf.Abs(canvasPos.x - prev.x);
        float currentSpeed = deltaX / Time.deltaTime;

        if (versesByPerson.TryGetValue(personId, out var verseList))
        {
            foreach (var v in verseList)
            {
                if (v == null) continue;
                var pulse = v.GetComponent<UITextPulse>();
                if (pulse != null)
                    pulse.SetSpeed(currentSpeed);
            }
        }

        lastPosByPerson[personId] = canvasPos;
    }


    // ===== 詩句生成（由 NoseRayProcessor 呼叫） =====
    public void SpawnVerse(Vector2 canvasPos, int personId = -1)
    {
        if (verseTextPrefab == null || verses == null || verses.Length == 0)
            return;

        // 檢查每人數量上限
        if (personId >= 0)
        {
            if (!versesByPerson.TryGetValue(personId, out var list))
            {
                list = new List<GameObject>();
                versesByPerson[personId] = list;
            }

            //// 若達上限就不再生成
            //if (list.Count >= maxVersesPerPerson)
            //    return;
        }

        GameObject verseObj = Instantiate(verseTextPrefab, parentRect);
        RectTransform verseRect = verseObj.GetComponent<RectTransform>();

        verseRect.anchoredPosition = canvasPos;
        //verseRect.localScale = Vector3.one;
        verseRect.localRotation = Quaternion.identity;

        // 隨機詩句內容
        var tmp = verseObj.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = verses[Random.Range(0, verses.Length)];
        else
        {
            var uiText = verseObj.GetComponent<Text>();
            if (uiText != null)
                uiText.text = verses[Random.Range(0, verses.Length)];
        }

        // 確保有 CanvasGroup，供淡出使用
        if (verseObj.GetComponent<CanvasGroup>() == null)
            verseObj.AddComponent<CanvasGroup>();

        // 登記進列表
        if (personId >= 0)
        {
            versesByPerson[personId].Add(verseObj);

            // 記錄詩句初始 X 偏移量
            if (fishByPerson.ContainsKey(personId))
            {
                Vector2 fishPos = fishByPerson[personId].anchoredPosition;
                Vector2 offset = canvasPos - fishPos;

                if (!verseOffset.ContainsKey(personId))
                    verseOffset[personId] = new Dictionary<GameObject, Vector2>();

                verseOffset[personId][verseObj] = offset; // ← 同時存 X / Y
            }
        }
    }

    // ===== 每幀更新：詩句跟隨魚 X 軸（保留初始偏移） =====
    private void Update()
    {
        foreach (var kv in versesByPerson)
        {
            int personId = kv.Key;

            // 該人有魚才更新
            if (!fishByPerson.TryGetValue(personId, out var fish) || fish == null)
                continue;

            Vector2 fishPos = fish.anchoredPosition;

            foreach (var verseObj in kv.Value)
            {
                if (verseObj == null) continue;

                RectTransform verseRect = verseObj.GetComponent<RectTransform>();
                if (verseRect == null) continue;

                // 抓取初始 offsetX / offsetY
                Vector2 offset = Vector2.zero;

                if (verseOffset.ContainsKey(personId) &&
                    verseOffset[personId].TryGetValue(verseObj, out var storedOffset))
                {
                    offset = storedOffset;
                }

                // 新的詩句座標
                Vector2 newPos = fishPos + offset;

                verseRect.anchoredPosition = newPos;
            }
        }
    }

    // ===== 移除魚（詩句淡出後銷毀） =====
    public void DespawnFish(int personId)
    {
        if (fishByPerson.TryGetValue(personId, out var rt) && rt != null)
            Destroy(rt.gameObject);

        fishByPerson.Remove(personId);
        lastPosByPerson.Remove(personId);

        // 詩句淡出
        if (versesByPerson.TryGetValue(personId, out var list))
        {
            foreach (var v in list)
            {
                if (v != null)
                {
                    var fade = v.GetComponent<UITextFadeOut>();
                    if (fade == null) fade = v.AddComponent<UITextFadeOut>();
                    fade.BeginFadeOut(fadeOutDuration);
                }
            }
            versesByPerson.Remove(personId);
        }

        // 清除對應的偏移紀錄
        verseOffset.Remove(personId);
    }

    // ===== 清空全部魚與詩句 =====
    public void ClearAll()
    {
        foreach (var kv in fishByPerson)
            if (kv.Value) Destroy(kv.Value.gameObject);
        fishByPerson.Clear();
        lastPosByPerson.Clear();

        foreach (var kv in versesByPerson)
        {
            foreach (var v in kv.Value)
                if (v != null) Destroy(v);
        }
        versesByPerson.Clear();

        verseOffset.Clear();
    }
    // 取得某位使用者目前已生成的詩句數量
    public bool TryGetVerseCount(int personId, out int count)
    {
        if (versesByPerson.TryGetValue(personId, out var list))
        {
            count = list.Count;
            return true;
        }
        count = 0;
        return false;
    }
}
