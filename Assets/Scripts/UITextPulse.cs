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

    private CanvasGroup canvasGroup;
    private Vector3 baseScale;
    private float birthTime;
    private float instFrequency;
    private const float TWO_PI = Mathf.PI * 2f;
    private bool initialized = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseScale = transform.localScale;

        // 初始化為最小狀態（這一幀立即可見）
        float k = minScale;
        float a = alphaAtMin;
        transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        canvasGroup.alpha = a;

        // 延後一幀再啟動動畫，避免第一幀畫面出現錯誤大小
        StartCoroutine(InitPulseNextFrame());
    }

    private System.Collections.IEnumerator InitPulseNextFrame()
    {
        yield return null; // 等待一幀
        birthTime = CurrentTime;
        float jitter = 1f + Random.Range(-frequencyJitterPercent, frequencyJitterPercent);
        instFrequency = Mathf.Max(0f, frequency * jitter);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        float elapsed = CurrentTime - birthTime;
        ApplyPulse(elapsed);
    }

    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void ApplyPulse(float elapsed)
    {
        float phase = (elapsed * instFrequency * TWO_PI) - (Mathf.PI * 0.5f);
        float s = (Mathf.Sin(phase) + 1f) * 0.5f;

        float k = Mathf.Lerp(minScale, maxScale, s);
        float a = Mathf.Lerp(alphaAtMin, alphaAtMax, s);

        transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        canvasGroup.alpha = a;
    }
}
