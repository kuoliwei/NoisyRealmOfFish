using System.Collections.Generic;
using UnityEngine;

public class HandRayProcessor : MonoBehaviour
{
    [Header("狀態設定")]
    [SerializeField] private float loseTrackThreshold = 0.2f;
    [SerializeField] private float hitRadiusMultiplier = 50f;

    [Header("References")]
    [SerializeField] private StarSpawnerUI starSpawnerUI;
    [SerializeField] private WaterWaveDualController waterWaveController;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    // 原本是 ExperienceFlowController，現在改成 NewExperienceFlowController
    [SerializeField] private NewExperienceFlowController flowController;

    private bool isHandActive = false;
    private bool lastState = false;
    private float lastReceivedTime = -999f;
    private List<Vector2> latestHandHits = new List<Vector2>();

    private Canvas targetCanvas;
    private Camera uiCamera;

    private void Start()
    {
        if (starSpawnerUI != null)
        {
            targetCanvas = starSpawnerUI.GetTargetCanvas();
            uiCamera = starSpawnerUI.GetUICamera();
        }
    }

    public void OnReceiveHandHits(List<Vector2> handHitList)
    {
        if (handHitList != null && handHitList.Count > 0)
        {
            lastReceivedTime = Time.time;
            latestHandHits.Clear();
            latestHandHits.AddRange(handHitList);
        }
    }

    private void Update()
    {
        bool hasRecentData = (Time.time - lastReceivedTime) <= loseTrackThreshold;
        isHandActive = hasRecentData;

        if (isHandActive != lastState)
        {
            if (enableDebug)
                Debug.Log(isHandActive ? "手部雷射啟動" : "手部雷射中斷");
            lastState = isHandActive;
        }

        if (isHandActive && latestHandHits.Count > 0 && starSpawnerUI != null)
        {
            CheckHandHitStars();
        }
    }

    private void CheckHandHitStars()
    {
        foreach (var uv in latestHandHits)
        {
            Vector2 screenPos = new Vector2(uv.x * Screen.width, uv.y * Screen.height);

            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetCanvas.transform as RectTransform,
                screenPos,
                targetCanvas.renderMode == RenderMode.ScreenSpaceCamera ? uiCamera : null,
                out canvasPos
            );

            foreach (var kvp in starSpawnerUI.ActiveStars)
            {
                GameObject starObj = kvp.Key;
                var data = kvp.Value;

                // 星星如果已經被 Destroy 變成 fake null，直接略過，避免後面存取 name 出錯
                if (starObj == null)
                    continue;

                float dist = Vector2.Distance(canvasPos, data.screenPos);
                float hitRadius = data.scale * hitRadiusMultiplier;

                if (dist < hitRadius)
                {
                    if (enableDebug)
                        Debug.Log("手部擊中星星：" + starObj.name);

                    if (starObj != null)
                    {
                        starSpawnerUI.SpawnHitText(data.screenPos);
                        Destroy(starObj);

                        // 通知水波控制器增加反射比例
                        if (waterWaveController != null)
                        {
                            waterWaveController.IncreaseReflectionScale();

                            // 這裡改成呼叫 NewExperienceFlowController 的 OnWaterCompleted
                            if (waterWaveController.IsExperienceCompleted() && flowController != null)
                                flowController.OnWaterCompleted();
                        }
                    }
                }
            }
        }
    }
}
