using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishSpawnerUI : MonoBehaviour
{
    [Header("Canvas References (與星星一致)")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform parentRect;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera uiCamera;

    [Header("Fish Prefab")]
    [SerializeField] private GameObject fishPrefab; // UI Image + chroma key shader

    [SerializeField] private bool invertDirection = false;

    // 一人一魚
    private readonly Dictionary<int, RectTransform> fishByPerson = new();
    private readonly Dictionary<int, Vector2> lastPosByPerson = new();

    public RectTransform ParentRect => parentRect;
    public Canvas GetTargetCanvas() => targetCanvas;
    public Camera GetUICamera() => uiCamera;

    // ===== 詩句設定（保留純 UI 層級控制） =====
    [Header("Verse Settings (UI Only)")]
    [SerializeField] private GameObject verseTextPrefab;   // 詩句 Text (建議 TMP_Text)
    [SerializeField, TextArea] private string[] verses;    // 可顯示的詩句清單
    [SerializeField] private float verseLifetime = 3f;     // 詩句顯示秒數

    // ===== 魚的生成/更新 =====
    public void SpawnOrUpdateFish(int personId, Vector2 canvasPos)
    {
        if (fishPrefab == null || parentRect == null) return;

        // 首次生成
        if (!fishByPerson.TryGetValue(personId, out var rt) || rt == null)
        {
            var go = Instantiate(fishPrefab, parentRect);
            rt = go.transform as RectTransform;
            fishByPerson[personId] = rt;

            // 初次位置
            rt.anchoredPosition = canvasPos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            lastPosByPerson[personId] = canvasPos;
            return;
        }

        // 更新位置
        Vector2 prev = lastPosByPerson.TryGetValue(personId, out var lp) ? lp : rt.anchoredPosition;
        rt.anchoredPosition = canvasPos;

        // 第一次不翻面防護
        if (!lastPosByPerson.ContainsKey(personId))
        {
            lastPosByPerson[personId] = canvasPos;
            return;
        }

        // 翻面判定（x 方向）
        float dx = canvasPos.x - prev.x;
        if (Mathf.Abs(dx) > 0.001f)
        {
            float dirSign = (dx >= 0 ? 1f : -1f) * (invertDirection ? -1f : 1f);
            Vector3 s = rt.localScale;
            s.x = Mathf.Abs(s.x) * dirSign;
            rt.localScale = s;
        }

        lastPosByPerson[personId] = canvasPos;
    }

    // ===== 詩句生成（由 NoseRayProcessor 呼叫） =====
    public void SpawnVerse(Vector2 canvasPos)
    {
        if (verseTextPrefab == null || verses == null || verses.Length == 0)
            return;

        GameObject verseObj = Instantiate(verseTextPrefab, parentRect);
        RectTransform verseRect = verseObj.GetComponent<RectTransform>();

        verseRect.anchoredPosition = canvasPos;
        verseRect.localScale = Vector3.one;
        verseRect.localRotation = Quaternion.identity;

        var tmp = verseObj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            string verseToShow = verses[Random.Range(0, verses.Length)];
            tmp.text = verseToShow;
        }
        else
        {
            var uiText = verseObj.GetComponent<Text>();
            if (uiText != null)
                uiText.text = verses[Random.Range(0, verses.Length)];
        }

        Destroy(verseObj, verseLifetime);
    }

    // ===== 移除魚 =====
    public void DespawnFish(int personId)
    {
        if (fishByPerson.TryGetValue(personId, out var rt) && rt != null)
            Destroy(rt.gameObject);

        fishByPerson.Remove(personId);
        lastPosByPerson.Remove(personId);
    }

    // ===== 清空全部魚 =====
    public void ClearAll()
    {
        foreach (var kv in fishByPerson)
            if (kv.Value) Destroy(kv.Value.gameObject);

        fishByPerson.Clear();
        lastPosByPerson.Clear();
    }
}
