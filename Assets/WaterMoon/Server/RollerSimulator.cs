using UnityEngine;
using Newtonsoft.Json.Linq; // 用來處理 JSON 結構

public class RollerSimulator : MonoBehaviour
{
    public WebSocketServerManager manager;

    [Header("傳送控制")]
    [Tooltip("每秒最多傳送幾筆資料")]
    public int sendRatePerSecond = 15;

    private float sendInterval;
    private float sendTimer = 0f;
    private bool allowSending = false;

    // 切換開關：是否要改變手部高度
    private bool modifyHandHeight = false;

    // 切換開關：是否要改變鼻子座標
    private bool modifyNoseCoordinate = false;
    [SerializeField] private Vector2 newNoseCoordinate;

    private void Start()
    {
        sendInterval = 1f / Mathf.Max(1, sendRatePerSecond);
    }

    private void Update()
    {
        // 按下 P 鍵開關 Pose 傳送
        if (Input.GetKeyDown(KeyCode.P))
        {
            allowSending = !allowSending;
            Debug.Log($"[Simulator] Pose 傳送 {(allowSending ? "啟動" : "停止")}");
        }

        // 按下 U 鍵切換手部高度修改模式
        if (Input.GetKeyDown(KeyCode.U))
        {
            modifyHandHeight = !modifyHandHeight;
            Debug.Log($"[Simulator] 手部高度修改 {(modifyHandHeight ? "啟用" : "關閉")}");
        }

        // 按下 N 鍵切換鼻子座標修改模式
        if (Input.GetKeyDown(KeyCode.N))
        {
            modifyNoseCoordinate = !modifyNoseCoordinate;
            Debug.Log($"[Simulator] 鼻子座標修改 {(modifyNoseCoordinate ? "啟用" : "關閉")}");
        }

        // 定時送出
        if (allowSending)
        {
            sendTimer += Time.deltaTime;
            if (sendTimer >= sendInterval)
            {
                SendModifiedPose();
                sendTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 根據 modifyHandHeight 狀態修改 poseJsonRaw，然後送出
    /// </summary>
    private void SendModifiedPose()
    {
        if (string.IsNullOrWhiteSpace(manager.poseJsonRaw))
            return;

        try
        {
            var root = JObject.Parse(manager.poseJsonRaw);

            // 取出 frame key（例如 "0"）
            var enumerator = root.Properties().GetEnumerator();
            if (!enumerator.MoveNext())
            {
                Debug.LogWarning("[Simulator] JSON 結構中沒有 frame 資料");
                return;
            }

            var frameProp = enumerator.Current;

            // 對應可庫美學格式: frameProp.Value 就是人物陣列
            var personsArray = frameProp.Value as JArray;
            if (personsArray == null || personsArray.Count == 0)
            {
                Debug.LogWarning("[Simulator] 無人物資料");
                return;
            }

            // 第一個人
            var person = personsArray[0] as JArray;
            if (person == null)
                return;

            const int LeftShoulder = 5;
            const int RightShoulder = 6;
            const int LeftWrist = 9;
            const int RightWrist = 10;
            const int Nose = 0;

            if (person.Count > RightWrist)
            {
                float lShoulderZ = (float)person[LeftShoulder][2];
                float rShoulderZ = (float)person[RightShoulder][2];
                float lWristZ_before = (float)person[LeftWrist][2];
                float rWristZ_before = (float)person[RightWrist][2];
                Vector2 noseCoordinate_before = new Vector2((float)person[Nose][0], (float)person[Nose][1]);
                if (modifyHandHeight)
                {
                    float delta = 0.3f;
                    person[LeftWrist][2] = lShoulderZ + delta;
                    person[RightWrist][2] = rShoulderZ + delta;
                }

                if (modifyNoseCoordinate)
                {
                    person[Nose][0] = newNoseCoordinate.x;
                    person[Nose][1] = newNoseCoordinate.y;
                }

                //float lWristZ_after = (float)person[LeftWrist][2];
                //float rWristZ_after = (float)person[RightWrist][2];

                //Debug.Log(
                //    $"[Simulator] U={modifyHandHeight} | " +
                //    $"LShoulderZ={lShoulderZ:F3}, LWristZ(before)={lWristZ_before:F3}→(after)={lWristZ_after:F3} | " +
                //    $"RShoulderZ={rShoulderZ:F3}, RWristZ(before)={rWristZ_before:F3}→(after)={rWristZ_after:F3}"
                //);
            }

            string modifiedJson = root.ToString();
            manager.server.SendMessageToClient(modifiedJson);
            //Debug.Log($"[Simulator] 已送出骨架資料（U模式:{modifyHandHeight}）");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Simulator] 修改 Pose JSON 失敗：{e.Message}");
            manager.SendPoseOnce();
        }
    }
}
