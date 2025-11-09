using System.Collections;
using UnityEngine;

public class WebSocketServerManager : MonoBehaviour
{
    public WebSocketServer.WebSocketServer server;

    // ====== 人體骨架測試版 ======
    [Header("Pose JSON（原樣重送）")]
    [TextArea(6, 18)]
    public string poseJsonRaw; // 把 3DPOSE_Output.txt 的內容整段貼進來

    [Header("Pose JSON 來源 (Resources)")]
    [Tooltip("放在 Assets/Resources/ 下的檔名（不含副檔名）")]
    public string poseResourceName = "3DPOSE_Output";

    [Tooltip("啟動時自動從 Resources 載入")]
    public bool autoLoadPoseFromResources = true;

    [Header("送出頻率")]
    public float poseFps = 15f;

    private Coroutine poseLoopCo;

    private void Awake()
    {
        if (autoLoadPoseFromResources)
            LoadPoseFromResources();
    }

    /// <summary>
    /// 從 Assets/Resources/{poseResourceName}.txt 載入並填入 poseJsonRaw
    /// </summary>
    public void LoadPoseFromResources()
    {
        var ta = Resources.Load<TextAsset>(poseResourceName);
        if (ta == null)
        {
            Debug.LogError($"[Server] 找不到 Resources/{poseResourceName}.txt 或 .json");
            return;
        }
        poseJsonRaw = ta.text;
        Debug.Log($"[Server] 已載入 Pose JSON（{poseResourceName}，{poseJsonRaw.Length} chars）");
    }

    /// <summary>
    /// 單次送出（原樣）
    /// </summary>
    public void SendPoseOnce()
    {
        if (string.IsNullOrWhiteSpace(poseJsonRaw))
        {
            Debug.LogWarning("[Server] poseJsonRaw 是空的，無法送出");
            return;
        }
        server.SendMessageToClient(poseJsonRaw);
    }

    /// <summary>
    /// 連續送出（原樣）
    /// </summary>
    public void StartPoseLoop()
    {
        if (poseLoopCo != null) StopCoroutine(poseLoopCo);
        poseLoopCo = StartCoroutine(CoPoseLoop());
    }

    public void StopPoseLoop()
    {
        if (poseLoopCo != null) StopCoroutine(poseLoopCo);
        poseLoopCo = null;
    }

    private IEnumerator CoPoseLoop()
    {
        if (string.IsNullOrWhiteSpace(poseJsonRaw))
        {
            Debug.LogWarning("[Server] poseJsonRaw 是空的，無法啟動連續送出");
            yield break;
        }

        var wait = new WaitForSeconds(1f / Mathf.Max(1f, poseFps));
        while (true)
        {
            server.SendMessageToClient(poseJsonRaw);
            yield return wait;
        }
    }
}
