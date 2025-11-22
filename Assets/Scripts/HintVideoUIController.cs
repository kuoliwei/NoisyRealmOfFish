using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 專用於提示影片的 UI 控制器：
/// - 自動依據影片解析度建立 RenderTexture
/// - 使用指定的 Shader（HintChromaKey）
/// - 在 RawImage 上顯示透明背景影片
/// - 完整自動初始化與清理
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class HintVideoUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage targetImage;

    [Header("Video")]
    [SerializeField] private VideoClip hintClip;

    [Header("Shader")]
    [SerializeField] private Shader hintChromaShader;
    [SerializeField] private float cutThreshold = 0.05f;

    private VideoPlayer player;
    private RenderTexture rt;
    private Material hintMat;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();

        if (hintClip == null)
        {
            Debug.LogError("[HintVideo] No VideoClip assigned!");
            return;
        }

        if (hintChromaShader == null)
        {
            Debug.LogError("[HintVideo] No hintChromaShader assigned!");
            return;
        }

        // ★ 1. 依照影片原生解析度建立 RT
        int w = (int)hintClip.width;
        int h = (int)hintClip.height;

        if (w <= 0 || h <= 0)
        {
            Debug.LogWarning("[HintVideo] Clip resolution invalid, fallback to 1024x1024.");
            w = 1024;
            h = 1024;
        }

        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        rt.name = "HintVideoRT";
        rt.Create();

        // ★ 2. 建立材質並套上 Shader
        hintMat = new Material(hintChromaShader);
        hintMat.SetFloat("_BlackThreshold", cutThreshold);
        hintMat.mainTexture = rt;

        // ★ 3. 設定 VideoPlayer → RT
        player.playOnAwake = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;
        player.clip = hintClip;
        player.isLooping = true;

        // ★ 4. 設定 RawImage 顯示影片材質
        if (targetImage != null)
        {
            targetImage.texture = rt;
            targetImage.material = hintMat;

            // 保持 RawImage 比例與影片一致
            AdjustRawImageAspect(targetImage.rectTransform, w, h);
        }
    }

    private void Start()
    {
        //if (player != null && hintClip != null)
        //    player.Play();
    }
    private void OnEnable()
    {
        if (player == null)
            player = GetComponent<VideoPlayer>();

        // 確保 clip 正確
        if (hintClip != null)
            player.clip = hintClip;

        // **重新播放 (最重要)**
        player.Play();
    }

    /// <summary>
    /// 自動調整 RawImage 的 RectTransform 使其與來源影片保持等比。
    /// 不改變寬，只調整高，避免 UI 拉伸造成變形。
    /// </summary>
    private void AdjustRawImageAspect(RectTransform rect, float w, float h)
    {
        float width = rect.sizeDelta.x;
        float newHeight = width * (h / w);
        //rect.sizeDelta = new Vector2(width, newHeight);
    }

    private void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }

        if (hintMat != null)
            Destroy(hintMat);
    }
}
