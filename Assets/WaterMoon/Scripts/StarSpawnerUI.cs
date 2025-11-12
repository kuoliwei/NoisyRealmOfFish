using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StarSpawnerUI : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform parentRect;
    [SerializeField] private GameObject starPrefab;

    [Header("Canvas Settings")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera uiCamera;

    [Header("Verses")]
    [SerializeField] private string[] verses;

    [System.Serializable]
    public class StarData
    {
        public Vector2 screenPos; // Canvas 座標
        public float scale;
    }

    private Dictionary<GameObject, StarData> activeStars = new();
    public IReadOnlyDictionary<GameObject, StarData> ActiveStars => activeStars;

    public GameObject SpawnStar(Vector2 canvasPos, Color baseColor, Color crossColor, float scale)
    {
        if (starPrefab == null || parentRect == null)
        {
            Debug.LogWarning("StarSpawnerUI: prefab or parentRect not assigned.");
            return null;
        }

        GameObject star = Instantiate(starPrefab, parentRect);
        RectTransform starRect = star.GetComponent<RectTransform>();
        starRect.anchoredPosition = canvasPos;
        starRect.localScale = new Vector3(scale, scale, 1f);
        starRect.localRotation = Quaternion.identity;

        var sc = star.GetComponent<StarController>();
        if (sc != null)
            sc.SetColors(baseColor, crossColor);

        activeStars.Add(star, new StarData { screenPos = canvasPos, scale = scale });

        StarController controller = star.GetComponent<StarController>();
        if (controller != null)
            controller.SetSpawner(this);

        return star;
    }

    public void UnregisterStar(GameObject star)
    {
        if (activeStars.ContainsKey(star))
            activeStars.Remove(star);
    }

    public Canvas GetTargetCanvas() => targetCanvas;
    public Camera GetUICamera() => uiCamera;

    // === UI Text 相關設定 ===
    [Header("Hit Text Settings")]
    [SerializeField] private GameObject textPrefab;   // 指向你的 UI Text 預置物
    [SerializeField] private float textLifetime = 1.5f; // 文字存在時間

    /// <summary>
    /// 生成擊中星星後出現的 UI 文字（位置與星星重疊）
    /// 隨機從 verses 中選一行詩句顯示。
    /// </summary>
    public void SpawnHitText(Vector2 canvasPos)
    {
        if (textPrefab == null || parentRect == null)
            return;

        GameObject textObj = Instantiate(textPrefab, parentRect);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchoredPosition = canvasPos;
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;

        // 從 verses 隨機選一句
        string verseToShow = "";
        if (verses != null && verses.Length > 0)
            verseToShow = verses[Random.Range(0, verses.Length)];

        var text = textObj.GetComponent<TMP_Text>();
        if (text != null)
            text.text = verseToShow;

        Destroy(textObj, textLifetime);
    }
}
