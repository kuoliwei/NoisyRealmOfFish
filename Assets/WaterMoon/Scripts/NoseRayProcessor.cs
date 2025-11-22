using System.Collections.Generic;
using UnityEngine;
using static SkeletonDataProcessor;

public class NoseRayProcessor : MonoBehaviour
{
    public enum SpawnMode { None, Stars, Fish }

    [Header("Fish spawner")]
    [SerializeField] private FishSpawnerUI fishSpawnerUI;

    [Header("Fish settings")]
    [SerializeField] private float loseTrackSeconds = 0.2f; // 一人一魚失聯多久後移除

    // 一人一魚暫存
    private readonly Dictionary<int, Vector2> latestNoseByPerson = new();
    private readonly Dictionary<int, float> lastSeenByPerson = new();

    // === Public API: 回傳目前有被鼻子射線追蹤到的體驗者數量 ===
    // Star 模式（靜水映月）用 latestNoseHits 判斷人數
    // Fish 模式（魚境）才用 latestNoseByPerson
    public int PersonInRangeCount
    {
        get
        {
            if (currentMode == SpawnMode.Stars)
                return latestNoseHits.Count;

            return latestNoseByPerson.Count;
        }
    }

    [SerializeField] private SpawnMode currentMode = SpawnMode.Stars;

    [Header("狀態設定")]
    [SerializeField] private float loseTrackThreshold = 0.2f;
    [SerializeField] private float idleResetTime = 120f;

    [Header("Star spawner")]
    [SerializeField] private StarSpawnerUI starSpawnerUI;
    [SerializeField] private WaterWaveDualController waterWaveController;
    [SerializeField] private ExperienceFlowController flowController;

    [Header("Star Spawn Range")]
    [SerializeField] private float starSpawnRange = 450f;
    [SerializeField] private float starNotSpawnRange = 225f; // 中心禁止生成區域

    [Header("Star Spawn Gap Settings")]
    [SerializeField] private float gapCenterAngle = 270f;      // 缺口中心角度（面向右方為0度，逆時針）
    [SerializeField] private float gapAngleRange = 60f;      // 缺口的半寬角度（例如40 = 整個缺口80度）

    [Header("Star Spawn Speed")]
    [SerializeField] private float StarSpawnSpeed = 0.5f;

    [Header("Color brightness range")]
    [SerializeField] private float colorBrightnessRangeLow = 0.5f;
    [SerializeField] private float colorBrightnessRangeHigh = 1.0f;

    [Header("Scale range")]
    [SerializeField] private float scaleRangeLow = 0.5f;
    [SerializeField] private float scaleRangeHigh = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    private bool isInsideRange = false;
    private bool lastState = false;
    private float lastReceivedTime = -999f;
    private List<NoseHitData> latestNoseHits = new();
    private float spawnTimer = 0f;
    private float lastActiveTime = -999f;
    private Canvas targetCanvas;
    private Camera uiCamera;

    // === 新增區塊：魚模式詩句生成設定 ===
    [Header("Verse (Fish Mode) Settings")]
    [SerializeField] private float verseRange = 150f;   // 圓形隨機範圍半徑
    [SerializeField] private float verseOffsetY = 150f; // 魚上方中心距離
    [SerializeField] private float verseSpeed = 0.5f;   // 每秒幾句

    [Header("Verse Layout Settings")]
    [SerializeField] private float verseSpacing = 60f; // 每句詩之間的垂直距離

    private float verseSpawnTimer = 0f;
    // === End ===

    private void Start()
    {
        if (starSpawnerUI != null)
        {
            targetCanvas = starSpawnerUI.GetTargetCanvas();
            uiCamera = starSpawnerUI.GetUICamera();
        }
        lastActiveTime = Time.time;
    }

    public void OnReceiveNoseHits(List<NoseHitData> noseHitList)
    {
        if (noseHitList != null && noseHitList.Count > 0)
        {
            lastReceivedTime = Time.time;
            latestNoseHits.Clear();
            latestNoseHits.AddRange(noseHitList);

            // 同步 Fish 的人員資料
            for (int i = 0; i < noseHitList.Count; i++)
            {
                var d = noseHitList[i];
                latestNoseByPerson[d.personId] = d.uv;
                lastSeenByPerson[d.personId] = Time.time;
            }

            lastActiveTime = Time.time;
        }
    }

    private void Update()
    {
        // === 新增：Star 模式的鼻子失聯清除 ===
        if (currentMode == SpawnMode.Stars)
        {
            if (Time.time - lastReceivedTime > loseTrackSeconds)
            {
                latestNoseHits.Clear();   // 關鍵：清掉星星模式的鼻子資料
                if (enableDebug)
                    Debug.Log("[NoseRayProcessor] Star mode lost track → no person");
            }
        }

        // ---- 這段每幀都要跑（不受 isInsideRange 限制）----
        if (currentMode == SpawnMode.Fish)
        {
            PruneTimedOutFish(); // 新增的方法，見下方
                                 // 若完全沒新資料超過 loseTrackSeconds，保底全部清空
            if (Time.time - lastReceivedTime > loseTrackSeconds)
            {
                if (fishSpawnerUI != null)
                    fishSpawnerUI.ClearAll();
                latestNoseByPerson.Clear();
                lastSeenByPerson.Clear();
            }
        }
        // ------------------------------------------------------
        bool hasRecentData = (Time.time - lastReceivedTime) <= loseTrackThreshold;

        // 修正：Star 模式若 latestNoseHits 已空 → 必須視為不在範圍內
        if (currentMode == SpawnMode.Stars && latestNoseHits.Count == 0)
        {
            isInsideRange = false;
        }
        else
        {
            isInsideRange = hasRecentData;
        }

        if (isInsideRange != lastState)
        {
            if (enableDebug)
                Debug.Log(isInsideRange ? "進入體驗範圍" : "離開體驗範圍");
            lastState = isInsideRange;
        }

        if (currentMode == SpawnMode.Stars && isInsideRange && latestNoseHits.Count > 0)
        {
            spawnTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(StarSpawnSpeed, 0.01f);
            while (spawnTimer >= interval)
            {
                spawnTimer -= interval;
                GenerateStars();
            }
        }
        else if (currentMode == SpawnMode.Fish && isInsideRange)
        {
            UpdateFishPositions();

            // === 新增區塊：魚模式詩句生成 ===
            verseSpawnTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(verseSpeed, 0.01f);
            while (verseSpawnTimer >= interval)
            {
                verseSpawnTimer -= interval;
                GenerateVerses();
            }
            // === End ===
        }
    }

    private void GenerateStars()
    {
        if (starSpawnerUI == null || targetCanvas == null)
            return;

        if (waterWaveController != null && waterWaveController.IsExperienceCompleted())
            return;

        foreach (var d in latestNoseHits)
        {
            Vector2 screenCenter = new Vector2(
                d.uv.x * Screen.width,
                d.uv.y * Screen.height
            );

            Vector2 offset;

            // --- 甜甜圈 + 缺口隨機 ---
            while (true)
            {
                // 隨機外圓內取點，但排除 starNotSpawnRange
                offset = Random.insideUnitCircle * starSpawnRange;
                if (offset.magnitude < starNotSpawnRange)
                    continue;   // 在內圈 → 重抽

                // 計算角度（0~360 度）
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                // 計算缺口上下界
                float halfGap = gapAngleRange;
                float minAngle = gapCenterAngle - halfGap;
                float maxAngle = gapCenterAngle + halfGap;

                // 處理跨 0° 的情況（例如缺口 350°~10°）
                bool inGap = false;
                if (minAngle < 0 || maxAngle >= 360)
                {
                    float m1 = (minAngle + 360) % 360;
                    float m2 = (maxAngle + 360) % 360;

                    if (angle >= m1 || angle <= m2)
                        inGap = true;
                }
                else
                {
                    if (angle >= minAngle && angle <= maxAngle)
                        inGap = true;
                }

                // 如果角度落在缺口 → 重抽
                if (!inGap)
                    break;
            }
            // --- 完成 offset ---

            Vector2 screenPos = screenCenter + offset;

            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetCanvas.transform as RectTransform,
                screenPos,
                targetCanvas.renderMode == RenderMode.ScreenSpaceCamera ? uiCamera : null,
                out canvasPos
            );

            float r = Random.Range(colorBrightnessRangeLow, colorBrightnessRangeHigh);
            float g = Random.Range(colorBrightnessRangeLow, colorBrightnessRangeHigh);
            float b = Random.Range(colorBrightnessRangeLow, colorBrightnessRangeHigh);
            Color baseColor = new Color(r, g, b, 1f);
            Color crossColor = Color.white;
            float scale = Random.Range(scaleRangeLow, scaleRangeHigh);

            starSpawnerUI.SpawnStar(canvasPos, baseColor, crossColor, scale);
        }
    }



    private void UpdateFishPositions()
    {
        if (fishSpawnerUI == null || targetCanvas == null)
            return;

        // === 修正重點：完全沒人時強制清空 ===
        if (latestNoseByPerson.Count == 0)
        {
            fishSpawnerUI.ClearAll();
            lastSeenByPerson.Clear();
            return;
        }
        // === End ===

        // 移除失聯者
        var toRemove = new List<int>();
        foreach (var kv in lastSeenByPerson)
        {
            if (Time.time - kv.Value > loseTrackSeconds)
                toRemove.Add(kv.Key);
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            int id = toRemove[i];
            lastSeenByPerson.Remove(id);
            latestNoseByPerson.Remove(id);
            fishSpawnerUI.DespawnFish(id);
        }

        // 逐人更新魚位置
        foreach (var kv in latestNoseByPerson)
        {
            int personId = kv.Key;
            Vector2 uv = kv.Value;

            Vector2 screenPos = new Vector2(uv.x * Screen.width, uv.y * Screen.height);
            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fishSpawnerUI.ParentRect,
                screenPos,
                targetCanvas.renderMode == RenderMode.ScreenSpaceCamera ? uiCamera : null,
                out canvasPos
            );

            fishSpawnerUI.SpawnOrUpdateFish(personId, canvasPos);
        }
    }
    private void PruneTimedOutFish()
    {
        if (fishSpawnerUI == null) return;

        // 依最後看見時間逐一淘汰
        var toRemove = new List<int>();
        foreach (var kv in lastSeenByPerson)
        {
            if (Time.time - kv.Value > loseTrackSeconds)
                toRemove.Add(kv.Key);
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            int id = toRemove[i];
            lastSeenByPerson.Remove(id);
            latestNoseByPerson.Remove(id);
            fishSpawnerUI.DespawnFish(id);
        }
    }


    // === 新增區塊：模仿星星的詩句生成 ===
    private void GenerateVerses()
    {
        if (fishSpawnerUI == null || targetCanvas == null)
            return;

        foreach (var kv in latestNoseByPerson)
        {
            int personId = kv.Key;
            Vector2 uv = kv.Value;
            Vector2 screenCenter = new Vector2(uv.x * Screen.width, uv.y * Screen.height);

            // 若該人已有幾句詩，從下往上排列
            int verseIndex = 0;
            if (fishSpawnerUI.TryGetVerseCount(personId, out int count))
                verseIndex = count;

            // 動態計算最大句數（直徑 / 間距）
            int maxVerses = Mathf.FloorToInt((verseRange * 2f) / verseSpacing);

            // 若超過允許數量 → 不生成
            if (verseIndex >= maxVerses)
                continue;

            // === 排列邏輯 ===
            float startOffsetY = verseOffsetY - verseRange + verseSpacing / 2f; // 由圓心下方起始
            float currentYOffset = startOffsetY + verseIndex * verseSpacing;

            // X 軸仍隨機
            float randomX = Random.Range(-verseRange, verseRange);
            Vector2 offset = new Vector2(randomX, currentYOffset);
            Vector2 screenPos = screenCenter + offset;

            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fishSpawnerUI.ParentRect,
                screenPos,
                targetCanvas.renderMode == RenderMode.ScreenSpaceCamera ? uiCamera : null,
                out canvasPos
            );

            // 呼叫 FishSpawnerUI 生成詩句
            fishSpawnerUI.SpawnVerse(canvasPos, personId);
        }
    }
    // ===============================================
    //  外部控制：切換到 Fish 模式
    // ===============================================
    public void EnableFishMode()
    {
        currentMode = SpawnMode.Fish;

        // 清空星星的資料（避免遺留）
        spawnTimer = 0f;

        // 清空魚境資料（乾淨開始）
        latestNoseByPerson.Clear();
        lastSeenByPerson.Clear();
        verseSpawnTimer = 0f;

        if (fishSpawnerUI != null)
            fishSpawnerUI.ClearAll();

        if (enableDebug)
            Debug.Log("[NoseRayProcessor] Fish Mode Enabled");
    }

    // ===============================================
    //  外部控制：退出 Fish 模式
    // ===============================================
    public void DisableFishMode()
    {
        // 清空魚境資料
        latestNoseByPerson.Clear();
        lastSeenByPerson.Clear();
        verseSpawnTimer = 0f;

        if (fishSpawnerUI != null)
            fishSpawnerUI.ClearAll();

        if (enableDebug)
            Debug.Log("[NoseRayProcessor] Fish Mode Disabled");
    }

    // ===============================================
    //  外部控制：切換到 Star 模式（靜水映月會用到）
    // ===============================================
    public void EnableStarMode()
    {
        currentMode = SpawnMode.Stars;

        // 清空魚境資料，以免影響星星模式
        latestNoseByPerson.Clear();
        lastSeenByPerson.Clear();
        verseSpawnTimer = 0f;

        if (fishSpawnerUI != null)
            fishSpawnerUI.ClearAll();

        if (enableDebug)
            Debug.Log("[NoseRayProcessor] Star Mode Enabled");
    }
    public void EnableNeutralMode()
    {
        currentMode = SpawnMode.None;

        // 不清空鼻子資料，因為下一輪進場會需要最新座標資料
        // 只清空魚境資料與星星資料由 FlowController 控制

        if (fishSpawnerUI != null)
            fishSpawnerUI.ClearAll();

        if (enableDebug)
            Debug.Log("[NoseRayProcessor] Neutral Mode Enabled");
    }
    // === End ===
}
