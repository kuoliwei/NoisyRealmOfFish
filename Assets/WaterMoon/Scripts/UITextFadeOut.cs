using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UITextFadeOut : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private float fadeTime;
    private float elapsed;
    private bool fading = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void BeginFadeOut(float duration)
    {
        fadeTime = Mathf.Max(0.01f, duration);
        elapsed = 0f;
        fading = true;
    }

    private void Update()
    {
        if (!fading) return;

        elapsed += Time.deltaTime;
        float t = elapsed / fadeTime;
        canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
