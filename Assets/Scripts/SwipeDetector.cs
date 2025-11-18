using System.Collections.Generic;
using UnityEngine;
using PoseTypes;

public class SwipeDetector : MonoBehaviour
{
    public SkeletonDataProcessor skeletonProcessor;
    public AutoFlip sunBookAutoFlip;
    public NewExperienceFlowController flowController;

    [Header("高度條件")]
    public float wristAboveHipThreshold = 0.1f;

    [Header("滑動偵測參數")]
    public float minSwipeDistanceX = 0.12f;
    public float maxSwipeTime = 0.5f;
    public float minInterval = 0.02f;

    private bool isSunIntroActive = true;

    // 每個人的滑動 tracking
    private class SwipeTrack
    {
        public bool tracking;
        public float startX;
        public float startTime;
        public float lastUpdateTime;
        public float lastRightMostX;
    }

    private Dictionary<int, SwipeTrack> tracks = new Dictionary<int, SwipeTrack>();

    void Start()
    {
        if (skeletonProcessor == null)
        {
            Debug.LogError("[SwipeDetector] skeletonProcessor is null");
            enabled = false;
            return;
        }

        skeletonProcessor.OnHandHitProcessed.AddListener(OnHandHits);
    }

    void OnDestroy()
    {
        if (skeletonProcessor != null)
            skeletonProcessor.OnHandHitProcessed.RemoveListener(OnHandHits);
    }

    public void ActivateSunIntroSwipe(bool active)
    {
        isSunIntroActive = active;

        // 每次重新啟動模式，重置所有 tracking
        if (!active)
            tracks.Clear();
    }

    private void OnHandHits(List<Vector2> hits)
    {
        if (!isSunIntroActive)
            return;

        if (sunBookAutoFlip == null)
            return;

        if (hits == null || hits.Count == 0)
            return;

        // 找最右側 UV.x
        float rightMostX = -999f;
        foreach (var uv in hits)
            if (uv.x > rightMostX)
                rightMostX = uv.x;

        float now = Time.time;

        // 逐一檢查 SkeletonDataProcessor 現存的 joints（多人的情況下）
        for (int personId = 0; personId < 10; personId++)
        {
            Vector3[] joints = skeletonProcessor.GetLatestJoints(personId);
            if (joints == null)
                continue; // 該人不存在，不視為錯誤

            // 準備 tracking 資料
            if (!tracks.ContainsKey(personId))
                tracks[personId] = new SwipeTrack();

            SwipeTrack t = tracks[personId];

            // 取得左右手腕與髖部高度
            float wristR = joints[(int)JointId.RightWrist].y;
            float wristL = joints[(int)JointId.LeftWrist].y;

            float hipL = joints[(int)JointId.LeftHip].y;
            float hipR = joints[(int)JointId.RightHip].y;
            float hipY = (hipL + hipR) * 0.5f;

            // 手必須舉高
            bool handHigh =
                (wristR >= hipY + wristAboveHipThreshold) ||
                (wristL >= hipY + wristAboveHipThreshold);

            if (!handHigh)
            {
                t.tracking = false;
                continue;
            }

            // 更新頻率限制
            if (now - t.lastUpdateTime < minInterval)
                continue;
            t.lastUpdateTime = now;

            // 尚未 tracking → 紀錄起始點
            if (!t.tracking)
            {
                t.tracking = true;
                t.startX = rightMostX;
                t.startTime = now;
                continue;
            }

            // 超時
            if (now - t.startTime > maxSwipeTime)
            {
                t.tracking = false;
                continue;
            }

            // 計算移動量
            float deltaX = rightMostX - t.startX;

            // 每一幀的移動方向必須是往左（避免往右一下又回來被當成左滑）
            bool movingLeft = rightMostX < t.lastRightMostX;

            // 儲存這一幀的位置
            t.lastRightMostX = rightMostX;

            // 判斷條件：必須連續往左 + deltaX 超過閾值
            if (movingLeft && deltaX <= -minSwipeDistanceX)
            {
                Debug.Log("[SwipeDetector] Person " + personId + " LEFT swipe detected");
                flowController.TryFlipSunPage();
                t.tracking = false;
            }
        }
    }
}
