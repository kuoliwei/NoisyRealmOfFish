using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 控制遮罩面板（Image + TMP_Text）的淡入淡出效果。
/// 可供外部呼叫 FadeIn / FadeOut。
/// </summary>
public class MaskPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image maskImage;
    [SerializeField] private TMP_Text maskText;
    public TMP_Text MaskText => maskText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    public float FadeDuration => fadeDuration;
    private Coroutine fadeCoroutine;

    /// <summary>
    /// 淡入：讓 Image 與 Text 從透明變成可見。
    /// </summary>
    public void FadeIn()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade(0f, 1f));
    }

    /// <summary>
    /// 淡出：讓 Image 與 Text 從可見變成透明。
    /// </summary>
    public void FadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;

        // 取出原始顏色
        Color imageColor = maskImage != null ? maskImage.color : Color.white;
        Color textColor = maskText != null ? maskText.color : Color.white;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);

            if (maskImage != null)
                maskImage.color = new Color(imageColor.r, imageColor.g, imageColor.b, currentAlpha);

            if (maskText != null)
                maskText.color = new Color(textColor.r, textColor.g, textColor.b, currentAlpha);

            yield return null;
        }

        // 最終確保精準值
        if (maskImage != null)
            maskImage.color = new Color(imageColor.r, imageColor.g, imageColor.b, endAlpha);

        if (maskText != null)
            maskText.color = new Color(textColor.r, textColor.g, textColor.b, endAlpha);

        fadeCoroutine = null;
    }

    /// <summary>
    /// 立即設定透明度（無漸變）
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (maskImage != null)
        {
            Color c = maskImage.color;
            maskImage.color = new Color(c.r, c.g, c.b, alpha);
        }

        if (maskText != null)
        {
            Color c = maskText.color;
            maskText.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
    public void FadeInTextOnly()
    {
        //if (maskText != null)
        //    StartCoroutine(FadeText(maskText.color.a, 1f));
        if (maskText != null)
            StartCoroutine(FadeText(0, 1));
    }

    public void FadeOutTextOnly()
    {
        //if (maskText != null)
        //    StartCoroutine(FadeText(maskText.color.a, 0f));
        if (maskText != null)
            StartCoroutine(FadeText(1, 0));
    }
    public void FadeInImageOnly()
    {
        //if (maskImage != null)
        //    StartCoroutine(FadeImage(maskImage.color.a, 1f));
        if (maskImage != null)
            StartCoroutine(FadeImage(0, 1));
    }

    public void FadeOutImageOnly()
    {
        //if (maskImage != null)
        //    StartCoroutine(FadeImage(maskImage.color.a, 0f));
        if (maskImage != null)
            StartCoroutine(FadeImage(1, 0));
    }
    private IEnumerator FadeText(float start, float end)
    {
        float timer = 0f;
        Color color = maskText.color;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float a = Mathf.Lerp(start, end, t);
            maskText.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }
    private IEnumerator FadeImage(float start, float end)
    {
        float timer = 0f;
        Color color = maskImage.color;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float a = Mathf.Lerp(start, end, t);
            maskImage.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
    }
}
