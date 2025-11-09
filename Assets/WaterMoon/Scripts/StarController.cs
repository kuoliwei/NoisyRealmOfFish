using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class StarController : MonoBehaviour
{
    private Material mat;

    // Shader property IDs
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int CrossIntensityID = Shader.PropertyToID("_CrossIntensity");
    private static readonly int GlowScaleXID = Shader.PropertyToID("_GlowScaleX");
    private static readonly int GlowScaleYID = Shader.PropertyToID("_GlowScaleY");
    private static readonly int CrossScaleXID = Shader.PropertyToID("_CrossScaleX");
    private static readonly int CrossScaleYID = Shader.PropertyToID("_CrossScaleY");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int CrossColorID = Shader.PropertyToID("_CrossColor");

    [Header("Glow Settings")]
    [Range(0, 5)] public float glowIntensity = 1f;
    [Range(0.1f, 3f)] public float glowScaleX = 1f;
    [Range(0.1f, 3f)] public float glowScaleY = 1f;
    public Color glowColor = Color.white;

    [Header("Cross Settings")]
    [Range(0, 5)] public float crossIntensity = 1f;
    [Range(0.1f, 3f)] public float crossScaleX = 1f;
    [Range(0.1f, 3f)] public float crossScaleY = 1f;
    public Color crossColor = Color.white;

    private Coroutine coroutine;

    [Header("Sine Control Settings")]
    public float glowSineAmplitude = 0.5f;
    public float crossSineAmplitude = 0.8f;
    public float glowBaseIntensity = 1.5f;
    public float crossBaseIntensity = 0.05f;
    public float glowSineFrequency = 1f;
    public float crossSineFrequency = 2f;

    [Header("Lifetime Settings")]
    [SerializeField] private float lifetime = 3f; // 星星存活時間（秒）

    private StarSpawnerUI spawner;
    public void SetSpawner(StarSpawnerUI spawnerRef)
    {
        spawner = spawnerRef;
    }
    void Start()
    {
        // 唯一改動：從 Graphic 而非 Renderer 取得材質
        var graphic = GetComponent<Graphic>();
        mat = Instantiate(graphic.material);
        graphic.material = mat;

        // 初始化 shader 顏色
        mat.SetColor(BaseColorID, glowColor);
        mat.SetColor(CrossColorID, crossColor);

        // 自動銷毀星星
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        UpdateGlowBySine();
        UpdateCrossBySine();
    }

    private void SetAllParameter()
    {
        mat.SetFloat(GlowIntensityID, glowIntensity);
        mat.SetFloat(CrossIntensityID, crossIntensity);
        mat.SetFloat(GlowScaleXID, glowScaleX);
        mat.SetFloat(GlowScaleYID, glowScaleY);
        mat.SetFloat(CrossScaleXID, crossScaleX);
        mat.SetFloat(CrossScaleYID, crossScaleY);
        mat.SetColor(BaseColorID, glowColor);
        mat.SetColor(CrossColorID, crossColor);
    }
    // 新增：讓外部可指定顏色（Spawner 會呼叫）
    public void SetColors(Color baseColor, Color crossColor)
    {
        glowColor = baseColor;
        this.crossColor = crossColor;
        if (mat == null)
        {
            var graphic = GetComponent<UnityEngine.UI.Graphic>();
            mat = Instantiate(graphic.material);
            graphic.material = mat;
        }
        mat.SetColor(BaseColorID, glowColor);
        mat.SetColor(CrossColorID, crossColor);
    }
    public void FlashGlow(float peak = 3f, float duration = 0.5f)
    {
        StartCoroutine(FlashGlowRoutine(peak, duration));
    }

    private IEnumerator FlashGlowRoutine(float peak, float duration)
    {
        float timer = 0f;
        float original = glowIntensity;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Sin(timer / duration * Mathf.PI);
            mat.SetFloat(GlowIntensityID, Mathf.Lerp(original, peak, t));
            yield return null;
        }
        mat.SetFloat(GlowIntensityID, original);
    }

    public void FlashCross(float peak = 3f, float duration = 0.3f)
    {
        StartCoroutine(FlashCrossRoutine(peak, duration));
    }

    private IEnumerator FlashCrossRoutine(float peak, float duration)
    {
        float timer = 0f;
        float original = crossIntensity;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Sin(timer / duration * Mathf.PI);
            mat.SetFloat(CrossIntensityID, Mathf.Lerp(original, peak, t));
            yield return null;
        }
        mat.SetFloat(CrossIntensityID, original);
    }

    private void UpdateGlowBySine()
    {
        float sineValue = Mathf.Sin(2f * Mathf.PI * glowSineFrequency * Time.time);
        sineValue = (sineValue + 1f) * 0.5f;
        glowIntensity = glowBaseIntensity + sineValue * glowSineAmplitude;
        mat.SetFloat(GlowIntensityID, glowIntensity);
    }

    private void UpdateCrossBySine()
    {
        float sineValue = Mathf.Sin(2f * Mathf.PI * crossSineFrequency * Time.time);
        sineValue = (sineValue + 1f) * 0.5f;
        crossIntensity = crossBaseIntensity + sineValue * crossSineAmplitude;
        mat.SetFloat(CrossIntensityID, crossIntensity);
    }
    private void OnDestroy()
    {
        if (spawner != null)
            spawner.UnregisterStar(gameObject);
    }
}
