using System.Collections.Generic;
using UnityEngine;
using PoseTypes;

public class SwipeDetector : MonoBehaviour
{
    [Header("來源 Processor")]
    public SkeletonDataProcessor skeletonProcessor;

    [Header("Flow Controller")]
    public NewExperienceFlowController flowController;

    [System.Serializable]
    public class SwipeEvent : UnityEngine.Events.UnityEvent { }

    [Header("向左滑動事件")]
    public SwipeEvent OnSwipeDetected = new();

    [Header("滑動距離（公尺）")]
    public float minSwipeDistanceY = 0.50f;

    [Header("滑動時間限制（秒）")]
    public float minSwipeTime = 0.10f; // ★ 新增：最短滑動時間
    public float maxSwipeTime = 0.30f; // 原本存在，但保持在這裡

    [Header("判斷頻率（秒）")]
    public float minInterval = 0.02f;

    [Header("除錯")]
    public bool debugLog = false;

    private bool allowSwipe = false;

    private class HandState
    {
        public bool tracking = false;
        public float startY;
        public float lastY;
        public float lastUpdateTime;
        public float startTime;
    }

    private Dictionary<int, Dictionary<int, HandState>> personHandStates = new();


    void Start()
    {
        if (skeletonProcessor == null)
        {
            Debug.LogError("[SwipeDetector] skeletonProcessor = null");
            enabled = false;
            return;
        }

        skeletonProcessor.OnPersonsCloseEnough.AddListener(OnPersonsNearby);
    }

    void OnDestroy()
    {
        if (skeletonProcessor != null)
            skeletonProcessor.OnPersonsCloseEnough.RemoveListener(OnPersonsNearby);
    }

    public void ActivateSunIntroSwipe(bool active)
    {
        allowSwipe = active;

        if (!active)
            ResetAllTracking();
    }

    private void OnPersonsNearby(List<PersonSkeleton> persons)
    {
        if (!allowSwipe)
            return;

        if (persons == null || persons.Count == 0)
            return;

        float now = Time.time;

        for (int i = 0; i < persons.Count; i++)
        {
            var person = persons[i];
            int pid = i;

            if (!personHandStates.TryGetValue(pid, out var handDict))
            {
                handDict = new Dictionary<int, HandState>();
                personHandStates[pid] = handDict;
            }

            var joints = person.joints;
            if (joints == null || joints.Length < PoseSchema.JointCount)
                continue;

            TryDetectSwipeForHand(pid, 0, joints[(int)JointId.LeftWrist].y, now);
            TryDetectSwipeForHand(pid, 1, joints[(int)JointId.RightWrist].y, now);
        }
    }


    private void TryDetectSwipeForHand(int personId, int handIndex, float currY, float now)
    {
        var handDict = personHandStates[personId];

        if (!handDict.TryGetValue(handIndex, out var state))
        {
            state = new HandState();
            handDict[handIndex] = state;
        }

        if (state.tracking && (now - state.lastUpdateTime < minInterval))
            return;

        state.lastUpdateTime = now;

        if (!state.tracking)
        {
            state.tracking = true;
            state.startY = currY;
            state.lastY = currY;
            state.startTime = now;
            return;
        }

        float elapsed = now - state.startTime;

        // 超時 → 清除
        if (elapsed > maxSwipeTime)
        {
            ResetHandTracking(personId, handIndex);
            return;
        }

        // 方向錯誤（應該要一直往左，所以 currY < lastY）
        if (currY > state.lastY)
        {
            ResetHandTracking(personId, handIndex);
            return;
        }

        // 累計距離
        float deltaY = state.startY - currY;

        if (deltaY >= minSwipeDistanceY)
        {
            // ★ 新增：檢查是否太快
            if (elapsed < minSwipeTime)
            {
                // 滑得太快，不算 → 重置
                if (debugLog)
                    Debug.Log($"[SwipeDetector] 太快滑動（{elapsed:F3}s）→ 不算");
                ResetHandTracking(personId, handIndex);
                return;
            }

            // 時間剛好在 min~max 之間 → 成功觸發
            if (debugLog)
                Debug.Log($"[SwipeDetector] 左滑成功: PID={personId} Hand={handIndex} (time {elapsed:F3}s)");

            OnSwipeDetected.Invoke();

            if (flowController != null)
                flowController.TryFlipSunPage();

            ResetHandTracking(personId, handIndex);
            return;
        }

        // 更新
        state.lastY = currY;
    }


    private void ResetHandTracking(int personId, int handIndex)
    {
        if (personHandStates.TryGetValue(personId, out var handDict))
        {
            if (handDict.TryGetValue(handIndex, out var s))
            {
                s.tracking = false;
            }
        }
    }

    private void ResetAllTracking()
    {
        foreach (var kv in personHandStates)
        {
            foreach (var h in kv.Value)
                h.Value.tracking = false;
        }
    }
}
