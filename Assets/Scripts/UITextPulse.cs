using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UITextPulse : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float minScale = 1.5f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private float frequency = 0.025f;
    [Range(0f, 0.5f)][SerializeField] private float frequencyJitterPercent = 0.1f;

    [Header("Transparency Settings")]
    [Range(0f, 1f)][SerializeField] private float alphaAtMin = 0.1f;
    [Range(0f, 1f)][SerializeField] private float alphaAtMax = 1.0f;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Speed Offset Settings")]
    [Tooltip("人物最低速度（靜止）")]
    [SerializeField] private float minSpeed = 20f;
    [Tooltip("人物最高速度（奔跑）")]
    [SerializeField] private float maxSpeed = 500f;
    [Tooltip("當速度最低時頻率最大偏移倍數，例如1.0代表+100%頻率")]
    [SerializeField] private float maxOffsetPercent = 1.0f;

    private CanvasGroup canvasGroup;
    private Vector3 baseScale;
    private float birthTime;
    private float instFrequency;
    private const float TWO_PI = Mathf.PI * 2f;
    private bool initialized = false;

    private float currentSpeed = 0f; // 外部傳入速度

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseScale = transform.localScale;

        // 初始化為最小狀態
        float k = minScale;
        float a = alphaAtMin;
        transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        canvasGroup.alpha = a;

        StartCoroutine(InitPulseNextFrame());
    }

    private System.Collections.IEnumerator InitPulseNextFrame()
    {
        yield return null;
        birthTime = CurrentTime;
        float jitter = 1f + Random.Range(-frequencyJitterPercent, frequencyJitterPercent);
        instFrequency = Mathf.Max(0f, frequency * jitter);
        initialized = true;
    }

    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void Update()
    {
        if (!initialized)
            return;

        float elapsed = CurrentTime - birthTime;
        ApplyPulse(elapsed);
    }

    private void ApplyPulse(float elapsed)
    {
        // 根據速度調整頻率偏移
        float speedT = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        float offsetFactor = 1f + speedT * maxOffsetPercent;
        float dynamicFreq = instFrequency * offsetFactor;

        float phase = (elapsed * dynamicFreq * TWO_PI) - (Mathf.PI * 0.5f);
        float s = (Mathf.Sin(phase) + 1f) * 0.5f;

        float k = Mathf.Lerp(minScale, maxScale, s);
        float a = Mathf.Lerp(alphaAtMin, alphaAtMax, s);

        //transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        //canvasGroup.alpha = a;

        // === 對縮放與透明度做平滑 ===
        Vector3 targetScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 0.2f);

        float currentAlpha = canvasGroup.alpha;
        canvasGroup.alpha = Mathf.Lerp(currentAlpha, a, 0.2f);
        // === End ===
    }

    // 由外部呼叫，更新人物移動速度
    public void SetSpeed(float speed)
    {
        float target = Mathf.Max(0f, speed);
        currentSpeed = Mathf.Lerp(currentSpeed, target, 0.2f); // 0.1f 表示平滑10%
    }
}
