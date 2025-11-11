using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UITextPulse : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float minScale = 1.5f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private float frequency = 0.025f;          // 基準頻率（每秒幾次循環）
    [Range(0f, 0.5f)]
    [SerializeField] private float frequencyJitterPercent = 0.1f; // 每個實例的頻率抖動比例（0~50%）

    [Header("Transparency Settings")]
    [Range(0f, 1f)][SerializeField] private float alphaAtMin = 0.1f;
    [Range(0f, 1f)][SerializeField] private float alphaAtMax = 1.0f;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = false;     // 需要不受 timeScale 影響就勾起來

    private CanvasGroup canvasGroup;
    private Vector3 baseScale;         // 保留 Prefab 原本的 localScale，縮放在其基礎上做
    private float birthTime;           // 每個實例自己的「出生時間」
    private float instFrequency;       // 每個實例自己的頻率（含抖動）
    private const float TWO_PI = Mathf.PI * 2f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseScale = transform.localScale;

        // 每個實例有自己的出生時間 & 頻率抖動（確保後續脫同步）
        birthTime = CurrentTime;
        float jitter = 1f + Random.Range(-frequencyJitterPercent, frequencyJitterPercent);
        instFrequency = Mathf.Max(0f, frequency * jitter);

        // 立刻套用一次，保證「剛生成就是最小值」
        ApplyPulse(0f);
    }

    private void Update()
    {
        float elapsed = CurrentTime - birthTime; // 出生後經過時間（每個實例不同）
        ApplyPulse(elapsed);
    }

    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void ApplyPulse(float elapsed)
    {
        // 讓 elapsed=0 時位於最小值：sin(x)=-1 → x = -π/2
        float phase = (elapsed * instFrequency * TWO_PI) - (Mathf.PI * 0.5f);
        float s = (Mathf.Sin(phase) + 1f) * 0.5f;   // 映射到 0..1

        float k = Mathf.Lerp(minScale, maxScale, s);
        float a = Mathf.Lerp(alphaAtMin, alphaAtMax, s);

        transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        canvasGroup.alpha = a;
    }
}
