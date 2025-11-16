using System.Collections.Generic;
using UnityEngine;
using static SkeletonDataProcessor;

public class NoseRayProcessor : MonoBehaviour
{
    public enum SpawnMode { Stars, Fish }

    [Header("Fish spawner")]
    [SerializeField] private FishSpawnerUI fishSpawnerUI;

    [Header("Fish settings")]
    [SerializeField] private float loseTrackSeconds = 0.2f; // 一人一魚失聯多久後移除

    // 一人一魚暫存
    private readonly Dictionary<int, Vector2> latestNoseByPerson = new();
    private readonly Dictionary<int, float> lastSeenByPerson = new();

    [SerializeField] private SpawnMode currentMode = SpawnMode.Stars;

    [Header("狀態設定")]
    [SerializeField] private float loseTrackThreshold = 0.2f;
    [SerializeField] private float idleResetTime = 120f;

    [Header("Star spawner")]
    [SerializeField] private StarSpawnerUI starSpawnerUI;
    [SerializeField] private WaterWaveDualController waterWaveController;
    [SerializeField] private ExperienceFlowController flowController;

    [Header("Range")]
    [SerializeField] private float range = 150f;

    [Header("Speed")]
    [SerializeField] private float speed = 0.5f;

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
        isInsideRange = hasRecentData;

        if (isInsideRange != lastState)
        {
            if (enableDebug)
                Debug.Log(isInsideRange ? "進入體驗範圍" : "離開體驗範圍");
            lastState = isInsideRange;
        }

        if (currentMode == SpawnMode.Stars && isInsideRange && latestNoseHits.Count > 0)
        {
            spawnTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(speed, 0.01f);
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
            Vector2 screenCenter = new Vector2(d.uv.x * Screen.width, d.uv.y * Screen.height);
            Vector2 offset = Random.insideUnitCircle * range;
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

    // === End ===
}
