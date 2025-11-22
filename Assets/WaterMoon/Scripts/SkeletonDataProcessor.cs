// SkeletonDataProcessor.cs (安全修正版 - 多人獨立 HandSmoother 版本)
using PoseTypes; // JointId / FrameSample / PersonSkeleton
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class SkeletonDataProcessor : MonoBehaviour
{
    [Header("可視化")]
    public GameObject jointPrefab;
    public Transform skeletonParent;
    public Vector3 jointScale = Vector3.one;

    [Header("座標轉換（資料 -> 本地座標）")]
    public Vector3 positionScale = Vector3.one;
    public Vector3 positionOffset = Vector3.zero;

    [Header("顯示條件")]
    public bool hideWhenLowConfidence = false;
    public float minConfidence = 0f;

    [Header("Console 列印")]
    public bool enableConsoleLog = true;
    public bool logOnlyWhenSomeonePresent = true;

    [Header("銀幕 Collider（單一）")]
    [SerializeField] private Collider screenCollider;

    [SerializeField] private LayerMask canvasLayer;
    [SerializeField] private float rayLength;

    [Header("骨架頻率統計")]
    [SerializeField] private bool logFpsEachSecond = true;
    [SerializeField] private bool logOnlyWhenValid = true;

    private int _recvFramesThisSec = 0;
    private int _validFramesThisSec = 0;
    private float _fpsWindowStart = 0f;
    private float _lastValidFrameTime = -1f;
    private readonly List<float> _validIntervals = new List<float>();

    // 每人左右手獨立平滑器
    private readonly Dictionary<int, HandSmoother> leftSmoothers = new();
    private readonly Dictionary<int, HandSmoother> rightSmoothers = new();

    // 每人鼻子獨立平滑器
    private readonly Dictionary<int, HandSmoother> noseSmoothers = new();

    // 專門給 SwipeDetector 使用：鼻子射線命中的人物清單
    [System.Serializable]
    public class PersonListEvent : UnityEvent<List<PersonSkeleton>> { }

    [Header("足夠靠近畫面的人的事件")]
    public PersonListEvent OnPersonsCloseEnough = new();

    // 鼻子射線事件
    [System.Serializable]
    public class NoseHitEvent : UnityEvent<List<NoseHitData>> { }

    [Header("鼻子射線事件")]
    public NoseHitEvent OnNoseHitProcessed = new();

    [System.Serializable]
    public class HandHitEvent : UnityEvent<List<Vector2>> { }

    [Header("手部射線事件")]
    public HandHitEvent OnHandHitProcessed = new();
    class SkeletonVisual
    {
        public int personId;
        public GameObject root;
        public Transform[] joints = new Transform[PoseSchema.JointCount];
        public Renderer[] renderers = new Renderer[PoseSchema.JointCount];
    }

    private readonly Dictionary<int, SkeletonVisual> visuals = new Dictionary<int, SkeletonVisual>();
    private readonly List<int> _tmpToRemove = new List<int>();

    [SerializeField] Vector2[] startPositions;

    [System.Serializable]
    public struct NoseHitData
    {
        public int personId;
        public Vector2 uv;
        public NoseHitData(int personId, Vector2 uv)
        {
            this.personId = personId;
            this.uv = uv;
        }
    }
    private void Update()
    {
        //// 測試：按 S 鍵生成一顆星星
        //if (Input.GetKey(KeyCode.S))
        //{
        //    var demoList = new List<NoseHitData>();
        //    // 給一個假 personId=0
        //    for (int i = 0; i < startPositions.Length; i++)
        //    {
        //        demoList.Add(new NoseHitData(0, startPositions[i]));
        //    }

        //    OnNoseHitProcessed?.Invoke(demoList);
        //}
    }

    public void HandleSkeletonFrame(FrameSample frame)
    {
        if (frame == null || frame.persons == null)
            return;

        var seen = new HashSet<int>();
        var noseHitList = new List<NoseHitData>();
        var handHitList = new List<Vector2>();
        var closePersons = new List<PersonSkeleton>();

        _recvFramesThisSec++;
        bool anyPerson = frame.persons.Count > 0;
        if (anyPerson)
        {
            _validFramesThisSec++;
            if (_lastValidFrameTime > 0f)
                _validIntervals.Add(Time.time - _lastValidFrameTime);
            _lastValidFrameTime = Time.time;
        }

        if (logFpsEachSecond && Time.time - _fpsWindowStart >= 1f)
        {
            _validIntervals.Clear();
            _fpsWindowStart += 1f;
            _recvFramesThisSec = 0;
            _validFramesThisSec = 0;
        }

        for (int p = 0; p < frame.persons.Count; p++)
        {
            var person = frame.persons[p];
            if (person == null || person.joints == null || person.joints.Length < PoseSchema.JointCount)
                continue;

            seen.Add(p);

            if (!visuals.TryGetValue(p, out var vis))
            {
                vis = CreateVisualForPerson(p);
                visuals.Add(p, vis);
            }
            // === 鼻子射線（固定方向） ===
            Transform nose = vis.joints[(int)JointId.Nose];
            if (nose != null)
            {
                if (!noseSmoothers.TryGetValue(p, out var noseSm))
                    noseSmoothers[p] = noseSm = new HandSmoother(0.2f, 0.002f);

                Vector3 dir = skeletonParent != null
                    ? skeletonParent.TransformDirection(Vector3.left)
                    : Vector3.left;

                Ray ray = new Ray(nose.position, dir.normalized);
                if (Physics.Raycast(ray, out RaycastHit hit, rayLength) && hit.collider == screenCollider)
                {
                    Vector2 uvRaw = hit.textureCoord;
                    Vector2 uvSmoothed = noseSm.Smooth(uvRaw);
                    noseHitList.Add(new NoseHitData(p, uvSmoothed));
                    closePersons.Add(person);

                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow, 0.1f);
                }
                else
                {
                    Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 0.1f);
                }
            }
            // 【新增】確保每個人都有獨立 smoother
            if (!leftSmoothers.TryGetValue(p, out var smootherL))
                leftSmoothers[p] = smootherL = new HandSmoother(0.2f, 0.002f);
            if (!rightSmoothers.TryGetValue(p, out var smootherR))
                rightSmoothers[p] = smootherR = new HandSmoother(0.2f, 0.002f);

            for (int j = 0; j < PoseSchema.JointCount; j++)
            {
                var data = person.joints[j];
                Vector3 pos = new Vector3(
                    data.x * positionScale.x,
                    data.z * positionScale.z,
                    data.y * positionScale.y
                ) + positionOffset;
                vis.joints[j].localPosition = pos;

                var r = vis.renderers[j];
                if (r != null)
                    r.enabled = !hideWhenLowConfidence || data.conf > minConfidence;
            }

            if (vis != null)
            {
                if (person.TryGet(JointId.LeftHip, out var leftHip) &&
                    person.TryGet(JointId.RightHip, out var rightHip))
                {
                    float hipZ = ((leftHip.z + rightHip.z) / 2f) * 0f;
                    var lw = person.joints[(int)JointId.LeftWrist];
                    var rw = person.joints[(int)JointId.RightWrist];

                    // 左手
                    if (lw.z > hipZ)
                    {
                        List<Vector2> uvResultsL = new();
                        int hitsL = TryGetWristUVs(vis.joints[(int)JointId.LeftWrist], uvResultsL);
                        for (int i = 0; i < hitsL; i++)
                        {
                            var uv = smootherL.Smooth(uvResultsL[i]); // 平滑單螢幕 UV
                            handHitList.Add(uv);
                        }
                    }

                    // 右手
                    if (rw.z > hipZ)
                    {
                        List<Vector2> uvResultsR = new();
                        int hitsR = TryGetWristUVs(vis.joints[(int)JointId.RightWrist], uvResultsR);
                        for (int i = 0; i < hitsR; i++)
                        {
                            var uv = smootherR.Smooth(uvResultsR[i]); // 平滑單螢幕 UV
                            handHitList.Add(uv);
                        }
                    }
                }
            }
        }

        PruneMissingPersons(seen);

        if (closePersons.Count > 0)
        {
            OnPersonsCloseEnough.Invoke(closePersons);
        }

        if (noseHitList.Count > 0)
        {
            noseHitList.Sort((a, b) => a.uv.x.CompareTo(b.uv.x));
            OnNoseHitProcessed?.Invoke(noseHitList);
        }

        if (handHitList.Count > 0)
        {
            handHitList.Sort((a, b) => a.x.CompareTo(b.x));

            OnHandHitProcessed.Invoke(handHitList);

            //StringBuilder sb = new StringBuilder("[HitList Sorted] ");
            //foreach (var uv in handHitList)
            //    sb.Append($"({uv.x:F3},{uv.y:F3}) ");
            //Debug.Log(sb.ToString());
        }
    }

    private SkeletonVisual CreateVisualForPerson(int personId)
    {
        var vis = new SkeletonVisual { personId = personId };
        vis.root = new GameObject($"Person_{personId}");
        if (skeletonParent != null)
            vis.root.transform.SetParent(skeletonParent, worldPositionStays: false);

        for (int j = 0; j < PoseSchema.JointCount; j++)
        {
            string jointName = ((JointId)j).ToString();
            GameObject go = jointPrefab != null
                ? Instantiate(jointPrefab, vis.root.transform)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            go.name = $"j_{j}_{jointName}";
            go.transform.localScale = jointScale;
            vis.joints[j] = go.transform;
            vis.renderers[j] = go.GetComponent<Renderer>();
        }
        return vis;
    }

    private void PruneMissingPersons(HashSet<int> seen)
    {
        _tmpToRemove.Clear();
        foreach (var kv in visuals)
            if (!seen.Contains(kv.Key)) _tmpToRemove.Add(kv.Key);

        foreach (var id in _tmpToRemove)
        {
            var vis = visuals[id];
            if (vis != null && vis.root != null)
                Destroy(vis.root);
            visuals.Remove(id);

            // 【新增】人物離場時清除其 smoother
            leftSmoothers.Remove(id);
            rightSmoothers.Remove(id);
            noseSmoothers.Remove(id);
        }
    }
    // 單一螢幕版本：不再回傳 quads，只收打到 screenCollider 的 UV
    private int TryGetWristUVs(Transform wrist, List<Vector2> uvs)
    {
        if (wrist == null) return 0;

        int hitCount = 0;
        RaycastHit hit;

        //// [Modified] 射線方向：固定朝前（Z+）
        //Vector3 dir = skeletonParent != null
        //    ? skeletonParent.TransformDirection(Vector3.forward)
        //    : Vector3.forward;

        Vector3 dir = skeletonParent != null
            ? skeletonParent.TransformDirection(Vector3.left)
            : Vector3.left;

        Ray ray = new Ray(wrist.position, dir.normalized);

        if (Physics.Raycast(ray, out hit, rayLength) && hit.collider == screenCollider)
        {
            uvs.Add(hit.textureCoord);
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.cyan, 0.1f);
            hitCount++;
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 0.1f);
        }

        return hitCount;
    }
    public Vector3[] GetLatestJoints(int personId)
    {
        if (visuals.TryGetValue(personId, out var vis))
        {
            Vector3[] arr = new Vector3[vis.joints.Length];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = vis.joints[i].position;
            return arr;
        }
        return null;
    }

}
