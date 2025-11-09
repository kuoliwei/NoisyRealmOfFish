using UnityEngine;
using System.Collections;

public class StarRippleAutoController : MonoBehaviour
{
    [SerializeField] private Material rippleMat;

    [Header("Ripple Animation Settings")]
    public float startAlpha = 1.0f;
    public float endAlpha = 0.0f;

    public float startFrequency = 10f;
    public float endFrequency = 40f;

    public float startWaveSpeed = 3.0f;
    public float endWaveSpeed = 0.5f;

    public float duration = 3.0f;

    private bool isPlaying = false;

    void Start()
    {
        // 預設為全透明
        rippleMat.SetFloat("_Alpha", 0f);
        rippleMat.SetFloat("_Frequency", startFrequency);
        rippleMat.SetFloat("_WaveSpeed", startWaveSpeed);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayRippleEffect();
        }
    }
    public void PlayRippleEffect()
    {
        if (!isPlaying)
            StartCoroutine(RippleRoutine());
    }

    private IEnumerator RippleRoutine()
    {
        isPlaying = true;

        float timer = 0f;

        // 開始時立即顯示波紋
        rippleMat.SetFloat("_Alpha", startAlpha);

        while (timer < duration)
        {
            float t = timer / duration;

            // 線性補間
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            float freq = Mathf.Lerp(startFrequency, endFrequency, t);
            float speed = Mathf.Lerp(startWaveSpeed, endWaveSpeed, t);

            rippleMat.SetFloat("_Alpha", alpha);
            rippleMat.SetFloat("_Frequency", freq);
            rippleMat.SetFloat("_WaveSpeed", speed);

            timer += Time.deltaTime;
            yield return null;
        }

        // 結尾時強制全透明
        rippleMat.SetFloat("_Alpha", 0f);
        rippleMat.SetFloat("_Frequency", endFrequency);
        rippleMat.SetFloat("_WaveSpeed", endWaveSpeed);

        isPlaying = false;
    }
}
