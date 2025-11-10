using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class WaterWaveDualController : MonoBehaviour
{
    [SerializeField] private Material waterMat;

    [Range(1, 8)] public int waveCountX = 3;
    [Range(1, 8)] public int waveCountY = 3;

    [Header("X Direction Waves")]
    public float[] waveFrequenciesX = new float[8];
    public float[] waveSpeedsX = new float[8];
    public float[] waveStrengthsX = new float[8];
    public float[] ampVarSpeedsX = new float[8];

    [Header("Y Direction Waves")]
    public float[] waveFrequenciesY = new float[8];
    public float[] waveSpeedsY = new float[8];
    public float[] waveStrengthsY = new float[8];
    public float[] ampVarSpeedsY = new float[8];

    [Header("Randomization Ranges (X)")]
    public float waveFrequenciesX_Min;
    public float waveFrequenciesX_Max;
    public float waveSpeedsX_Min;
    public float waveSpeedsX_Max;
    public float waveStrengthsX_Min;
    public float waveStrengthsX_Max;
    public float ampVarSpeedsX_Min;
    public float ampVarSpeedsX_Max;

    [Header("Randomization Ranges (Y)")]
    public float waveFrequenciesY_Min;
    public float waveFrequenciesY_Max;
    public float waveSpeedsY_Min;
    public float waveSpeedsY_Max;
    public float waveStrengthsY_Min;
    public float waveStrengthsY_Max;
    public float ampVarSpeedsY_Min;
    public float ampVarSpeedsY_Max;

    [Header("Amplitude Pulse Control")]
    public bool enablePulse = true;
    public float pulseMultiplier = 1.0f;

    private static readonly int WaveCountXID = Shader.PropertyToID("_WaveCountX");
    private static readonly int WaveCountYID = Shader.PropertyToID("_WaveCountY");
    private static readonly int WaveFrequenciesXID = Shader.PropertyToID("_WaveFrequenciesX");
    private static readonly int WaveSpeedsXID = Shader.PropertyToID("_WaveSpeedsX");
    private static readonly int WaveStrengthsXID = Shader.PropertyToID("_WaveStrengthsX");
    private static readonly int WaveFrequenciesYID = Shader.PropertyToID("_WaveFrequenciesY");
    private static readonly int WaveSpeedsYID = Shader.PropertyToID("_WaveSpeedsY");
    private static readonly int WaveStrengthsYID = Shader.PropertyToID("_WaveStrengthsY");

    private float[] currentWaveStrengthsX = new float[8];
    private float[] currentWaveStrengthsY = new float[8];

    // -----------------------------------------------------------
    // Reflection Scale 控制邏輯
    // -----------------------------------------------------------
    [Header("Reflection Scale Control")]
    [SerializeField] private float reflectionScaleX = 0.8f;
    [SerializeField] private float reflectionScaleY = 1.2f;
    [SerializeField] private float maxScaleX = 6.5f;
    [SerializeField] private float maxScaleY = 3.0f;

    [Header("Touch Settings")]
    [Tooltip("完成體驗所需觸碰的星星數量")]
    [SerializeField] private int totalTouchesRequired = 20;

    private int currentTouchCount = 0;

    private static readonly int ReflectionScaleXID = Shader.PropertyToID("_ReflectionScaleX");
    private static readonly int ReflectionScaleYID = Shader.PropertyToID("_ReflectionScaleY");

    void Start()
    {
        RandomizeWaves();
        //UpdateReflectionScale();
    }

    void Update()
    {
        if (waterMat == null) return;

        if (enablePulse)
            UpdateWaveAmplitudePulse();

        // 更新波參數
        waterMat.SetInt(WaveCountXID, waveCountX);
        waterMat.SetFloatArray(WaveFrequenciesXID, waveFrequenciesX);
        waterMat.SetFloatArray(WaveSpeedsXID, waveSpeedsX);
        waterMat.SetFloatArray(WaveStrengthsXID, currentWaveStrengthsX);

        waterMat.SetInt(WaveCountYID, waveCountY);
        waterMat.SetFloatArray(WaveFrequenciesYID, waveFrequenciesY);
        waterMat.SetFloatArray(WaveSpeedsYID, waveSpeedsY);
        waterMat.SetFloatArray(WaveStrengthsYID, currentWaveStrengthsY);
    }
    public bool IsExperienceCompleted()
    {
        return currentTouchCount >= totalTouchesRequired;
    }
    [ContextMenu("Randomize Wave Parameters")]
    public void RandomizeWaves()
    {
        for (int i = 0; i < waveCountX; i++)
        {
            waveFrequenciesX[i] = Random.Range(waveFrequenciesX_Min, waveFrequenciesX_Max);
            waveSpeedsX[i] = Random.Range(waveSpeedsX_Min, waveSpeedsX_Max);
            waveStrengthsX[i] = Random.Range(waveStrengthsX_Min, waveStrengthsX_Max);
            ampVarSpeedsX[i] = Random.Range(ampVarSpeedsX_Min, ampVarSpeedsX_Max);
        }

        for (int j = 0; j < waveCountY; j++)
        {
            waveFrequenciesY[j] = Random.Range(waveFrequenciesY_Min, waveFrequenciesY_Max);
            waveSpeedsY[j] = Random.Range(waveSpeedsY_Min, waveSpeedsY_Max);
            waveStrengthsY[j] = Random.Range(waveStrengthsY_Min, waveStrengthsY_Max);
            ampVarSpeedsY[j] = Random.Range(ampVarSpeedsY_Min, ampVarSpeedsY_Max);
        }
    }

    private void UpdateWaveAmplitudePulse()
    {
        for (int i = 0; i < waveCountX; i++)
        {
            float sinValue = Mathf.Sin(2 * Mathf.PI * Time.time * ampVarSpeedsX[i]);
            currentWaveStrengthsX[i] = Mathf.Abs(sinValue) * pulseMultiplier * waveStrengthsX[i];
        }

        for (int j = 0; j < waveCountY; j++)
        {
            float sinValue = Mathf.Sin(2 * Mathf.PI * Time.time * ampVarSpeedsY[j]);
            currentWaveStrengthsY[j] = Mathf.Abs(sinValue) * pulseMultiplier * waveStrengthsY[j];
        }
    }

    /// <summary>
    /// 每次手部擊中星星時呼叫，使 ReflectionScaleX / Y 按比例成長。
    /// </summary>
    public void IncreaseReflectionScale()
    {
        if (currentTouchCount >= totalTouchesRequired)
            return;

        currentTouchCount++;

        float t = Mathf.Clamp01((float)currentTouchCount / totalTouchesRequired);
        reflectionScaleX = Mathf.Lerp(0.8f, maxScaleX, t);
        reflectionScaleY = Mathf.Lerp(1.2f, maxScaleY, t);

        UpdateReflectionScale();

        Debug.Log($"[WaterWaveDualController] Touch {currentTouchCount}/{totalTouchesRequired} → X={reflectionScaleX:F2}, Y={reflectionScaleY:F2}");
    }

    private void UpdateReflectionScale()
    {
        if (waterMat != null)
        {
            waterMat.SetFloat(ReflectionScaleXID, reflectionScaleX);
            waterMat.SetFloat(ReflectionScaleYID, reflectionScaleY);
        }
    }

    /// <summary>
    /// 將反射比例回復到初始狀態。
    /// </summary>
    public void ResetToInitial()
    {
        reflectionScaleX = 0.8f;
        reflectionScaleY = 1.2f;
        currentTouchCount = 0;

        if (waterMat != null)
        {
            waterMat.SetFloat(ReflectionScaleXID, reflectionScaleX);
            waterMat.SetFloat(ReflectionScaleYID, reflectionScaleY);
        }

        Debug.Log("[WaterWaveDualController] 已重設為初始值");
    }

}
