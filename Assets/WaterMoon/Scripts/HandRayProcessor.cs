using System.Collections.Generic;
using UnityEngine;

public class HandRayProcessor : MonoBehaviour
{
    [Header("篈砞﹚")]
    [SerializeField] private float loseTrackThreshold = 0.2f;
    [SerializeField] private float hitRadiusMultiplier = 50f;

    [Header("References")]
    [SerializeField] private StarSpawnerUI starSpawnerUI;
    [SerializeField] private WaterWaveDualController waterWaveController;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = true;

    [SerializeField] private ExperienceFlowController flowController;

    private bool isHandActive = false;
    private bool lastState = false;
    private float lastReceivedTime = -999f;
    private List<Vector2> latestHandHits = new();

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
                Debug.Log(isHandActive ? "も场筽甮币笆" : "も场筽甮い耞");
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

                float dist = Vector2.Distance(canvasPos, data.screenPos);
                float hitRadius = data.scale * hitRadiusMultiplier;

                if (dist < hitRadius)
                {
                    if (enableDebug)
                        Debug.Log($"も场阑い琍琍{starObj.name}");

                    if (starObj != null)
                    {
                        starSpawnerUI.SpawnHitText(data.screenPos);
                        Destroy(starObj);

                        // 硄猧北竟糤は甮ゑㄒ
                        if (waterWaveController != null)
                        {
                            waterWaveController.IncreaseReflectionScale();

                            if (waterWaveController.IsExperienceCompleted())
                                flowController.OnExperienceCompleted();
                        }
                    }
                }
            }
        }
    }
}
