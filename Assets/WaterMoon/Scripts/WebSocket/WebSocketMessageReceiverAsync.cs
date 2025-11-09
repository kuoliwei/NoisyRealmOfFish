using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Events;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Xml.Linq;

public class WebSocketMessageReceiverAsync : MonoBehaviour
{
    [Header("連接 UI 元件")]
    [SerializeField] private WebSocketConnectUI webSocketConnectUI;
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private ReconnectPanelController reconnectUI;

    [Header("WebSocket 客戶端")]
    [SerializeField] private WebSocketClient webSocketClient;

    [Header("是否允許處理 Pose 資料")]
    public bool CanReceivePoseMessage = true;

    [System.Serializable]
    public class PoseFrameEvent : UnityEvent<PoseTypes.FrameSample> { }

    [Header("接收 Pose 資料事件")]
    public PoseFrameEvent OnPoseFrameReceived = new();

    private readonly ConcurrentQueue<PoseTypes.FrameSample> poseMainThreadQueue = new();

    private float messageTimer = 0f;

    private void Start()
    {
        if (webSocketClient != null)
        {
            webSocketClient.OnMessageReceive.AddListener(message =>
            {
                ReceiveSkeletonMessage(message); // 僅接收骨架資料
            });
            webSocketClient.OnConnected.AddListener(OnWebSocketConnected);
            webSocketClient.OnConnectionError.AddListener(() =>
            {
                webSocketConnectUI.OnConnectionFaild("連線失敗");
            });
            webSocketClient.OnDisconnected.AddListener(OnWebSocketDisconnected);
        }
    }

    private void Update()
    {
        // 處理骨架 Pose 佇列
        int processedPose = 0;
        while (poseMainThreadQueue.TryDequeue(out var frame))
        {
            OnPoseFrameReceived.Invoke(frame);
            processedPose++;
        }

        // 每秒監控可開啟除錯顯示
        messageTimer += Time.deltaTime;
        if (messageTimer >= 1f)
        {
            // Debug.Log($"[監控] 每秒處理 {processedPose} 組骨架資料。Queue 剩餘：{poseMainThreadQueue.Count}");
            messageTimer = 0f;
        }
    }

    private void ReceiveSkeletonMessage(string messageContent)
    {
        //Debug.Log($"receive pose message：{messageContent}");
        if (!CanReceivePoseMessage)
        {
            Debug.LogWarning("忽略訊息：CanReceivePoseMessage 為 false");
            return;
        }

        try
        {
            // 格式: { "<frameIndex>": [ [ [x,y,z,conf],...17點 ],  [ ...人物1... ] ] }
            var root = JObject.Parse(messageContent);

            var enumerator = root.Properties().GetEnumerator();
            if (!enumerator.MoveNext())
            {
                Debug.LogWarning("[ReceiveMessage] JSON 解析成功但沒有任何 frame key");
                return;
            }

            var frameProp = enumerator.Current;
            if (!int.TryParse(frameProp.Name, out int frameIndex))
            {
                Debug.LogError($"[ReceiveMessage] frame key 無法轉成 int: {frameProp.Name}");
                return;
            }

            var personsArray = frameProp.Value as JArray;
            if (personsArray == null)
            {
                Debug.LogError("[ReceiveMessage] frame value 不是陣列 (persons)");
                return;
            }

            var frame = new PoseTypes.FrameSample(frameIndex);
            frame.recvTime = Time.realtimeSinceStartup;

            for (int personId = 0; personId < personsArray.Count; personId++)
            {
                var personJoints = personsArray[personId] as JArray;
                if (personJoints == null)
                {
                    Debug.LogWarning($"[ReceiveMessage] 人物 {personId} joints 不是陣列，略過");
                    continue;
                }

                var person = new PoseTypes.PersonSkeleton();
                int jointCount = Math.Min(personJoints.Count, PoseTypes.PoseSchema.JointCount);
                for (int j = 0; j < jointCount; j++)
                {
                    var jArr = personJoints[j] as JArray;
                    if (jArr == null || jArr.Count < 4)
                        continue;

                    float x = jArr[0]!.Value<float>();
                    float y = jArr[1]!.Value<float>();
                    float z = jArr[2]!.Value<float>();
                    float conf = jArr[3]!.Value<float>();

                    person.joints[j] = new PoseTypes.Joint(x, y, z, conf);
                }

                frame.persons.Add(person);
            }

            // 丟到主執行緒事件佇列
            poseMainThreadQueue.Enqueue(frame);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReceiveMessage] 解析 Pose JSON 失敗。Error: {e.Message}\n內容: {messageContent}");
        }
    }

    private void OnWebSocketDisconnected()
    {
        Debug.Log("呼叫 OnWebSocketDisconnected()");
        if (!connectPanel.activeSelf)
        {
            reconnectUI?.ShowFlicker();
            webSocketClient.allowReconnect = true;
            webSocketClient.isReconnectAttempt = true;
            Debug.Log("掉線中，自動啟用重連機制");
        }
        else
        {
            Debug.Log("ConnectPanel 開啟中，不自動重連");
        }
    }

    private void OnWebSocketConnected()
    {
        reconnectUI?.ShowSuccessAndHide();
        webSocketConnectUI?.OnConnectionSucceeded();
        webSocketClient.allowReconnect = false;

        if (webSocketClient.isReconnectAttempt)
        {
            Debug.Log("重新連線成功");
            webSocketClient.isReconnectAttempt = false;
        }
    }

    public void ConnectToServer(string ip, string port)
    {
        string address = $"ws://{ip}:{port}";
        webSocketClient.CloseConnection();
        webSocketClient.StartConnection(address);
    }
}
