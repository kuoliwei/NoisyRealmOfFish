using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class FishUIController : MonoBehaviour
{
    [Header("UI Components")]
    public RawImage targetImage;      // 把你的 UI Image 改成 RawImage 會更方便
    public Shader chromaKeyShader;    // 指到 FishChromaKey.shader
    public VideoClip[] fishVideos;    // 放所有魚影片

    private VideoPlayer player;
    private Material fishMaterial;
    private RenderTexture fishRT;

    void Awake()
    {
        player = GetComponent<VideoPlayer>();

        // 每條魚建立自己的 RenderTexture
        fishRT = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
        fishRT.name = "FishRT_" + GetInstanceID();

        // 每條魚建立自己的 Material 實例
        fishMaterial = new Material(chromaKeyShader);

        // 綁定材質與 RenderTexture
        fishMaterial.mainTexture = fishRT;
        targetImage.material = fishMaterial;

        // 設定 VideoPlayer 輸出
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = fishRT;

        // 隨機選擇影片
        if (fishVideos != null && fishVideos.Length > 0)
        {
            player.clip = fishVideos[Random.Range(0, fishVideos.Length)];
        }

        player.isLooping = true;
    }

    void Start()
    {
        player.Play();
    }

    void OnDestroy()
    {
        // 清理資源避免記憶體殘留
        if (fishRT != null) fishRT.Release();
        if (fishMaterial != null) Destroy(fishMaterial);
    }
}
