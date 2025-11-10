using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class UITextPulse : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float minScale;
    [SerializeField] private float maxScale;
    [SerializeField] private float frequency; // 每秒幾次完整放大縮小循環

    [Header("Transparency Settings")]
    [Range(0f, 1f)][SerializeField] private float alphaAtMin;
    [Range(0f, 1f)][SerializeField] private float alphaAtMax;

    private CanvasGroup canvasGroup;
    private float timeOffset;

    private void Awake()
    {
        // 確保有 CanvasGroup（用來控制透明度）
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 每個物件的動畫略有不同相位，避免同步
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // 計算當前時間點的 Sine 值（介於 -1~1）
        float t = (Mathf.Sin((Time.time + timeOffset) * frequency * Mathf.PI * 2f) + 1f) / 2f;

        // 插值縮放與透明度
        float scale = Mathf.Lerp(minScale, maxScale, t);
        float alpha = Mathf.Lerp(alphaAtMin, alphaAtMax, t);

        transform.localScale = new Vector3(scale, scale, 1f);
        canvasGroup.alpha = alpha;
    }
}
