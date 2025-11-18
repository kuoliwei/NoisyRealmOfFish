//The implementation is based on this article:http://rbarraza.com/html5-canvas-pageflip/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public enum FlipMode
{
    RightToLeft,
    LeftToRight
}

[ExecuteInEditMode]
public class Book : MonoBehaviour
{
    public Canvas canvas;
    [SerializeField] RectTransform BookPanel;

    public Sprite background;
    public Sprite[] bookPages;

    public bool interactable = true;
    public bool enableShadowEffect = true;

    // index of sprite shown on right page
    public int currentPage = 0;
    public int TotalPageCount => bookPages.Length;

    // ★ 你原始版本就有的三個屬性（必要給 AutoFlip）
    public Vector3 EndBottomLeft => ebl;
    public Vector3 EndBottomRight => ebr;
    public float Height => BookPanel.rect.height;

    // Page UI elements
    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image LeftNext;
    public Image Right;
    public Image RightNext;

    public UnityEvent OnFlip;

    float radius1, radius2;

    // spine / edges / corners
    Vector3 sb;    // Spine bottom
    Vector3 st;    // Spine top
    Vector3 c;     // page corner
    Vector3 ebr;   // Edge bottom right
    Vector3 ebl;   // Edge bottom left
    Vector3 f;     // follow point

    bool pageDragging = false;
    FlipMode mode;

    // ========================================================================
    // Start
    // ========================================================================
    void Start()
    {
        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);

        UpdateSprites();
        CalcCurlCriticalPoints();
        InitLayout();   // ★ 新增（修復回首頁後跑位）
    }

    // ========================================================================
    // ★ ClippingPlane / Shadow / NextPageClip 幾何重建（回 sunInfo 必須重跑）
    // ========================================================================
    private void InitLayout()
    {
        float pageWidth = BookPanel.rect.width / 2f;
        float pageHeight = BookPanel.rect.height;

        NextPageClip.rectTransform.sizeDelta =
            new Vector2(pageWidth, pageHeight * 3);

        ClippingPlane.rectTransform.sizeDelta =
            new Vector2(pageWidth * 2 + pageHeight, pageHeight * 3);

        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
        float shadowPageHeight = pageWidth / 2 + hyp;

        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);

        // ★ 回正（避免殘留角度）
        ClippingPlane.transform.localPosition = Vector3.zero;
        ClippingPlane.transform.localEulerAngles = Vector3.zero;

        NextPageClip.transform.localPosition = Vector3.zero;
        NextPageClip.transform.localEulerAngles = Vector3.zero;
    }

    // ========================================================================
    // ★ 回到第 0 頁（你的 sunInfo 需要）
    // ========================================================================
    public void ResetToFirstPage()
    {
        currentPage = 0;
        UpdateSprites();

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);

        // restore hierarchy
        Left.transform.SetParent(BookPanel.transform, true);
        Right.transform.SetParent(BookPanel.transform, true);
        LeftNext.transform.SetParent(BookPanel.transform, true);
        RightNext.transform.SetParent(BookPanel.transform, true);
        Shadow.transform.SetParent(BookPanel.transform, true);
        ShadowLTR.transform.SetParent(BookPanel.transform, true);

        pageDragging = false;
        f = Vector3.zero;

        CalcCurlCriticalPoints();
        InitLayout();   // ★ 最重要：重建布局避免跑位
    }

    // ========================================================================
    // Curl critical points
    // ========================================================================
    private void CalcCurlCriticalPoints()
    {
        sb = new Vector3(0, -BookPanel.rect.height / 2);
        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        st = new Vector3(0, BookPanel.rect.height / 2);

        radius1 = Vector2.Distance(sb, ebr);

        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;

        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
    }

    // ========================================================================
    // transform input
    // ========================================================================
    public Vector3 transformPoint(Vector3 mouseScreenPos)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector3 world = canvas.worldCamera.ScreenToWorldPoint(
                new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance));
            return BookPanel.InverseTransformPoint(world);
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane p = new Plane(
                transform.TransformPoint(ebr),
                transform.TransformPoint(ebl),
                transform.TransformPoint(st));

            float d;
            p.Raycast(ray, out d);

            return BookPanel.InverseTransformPoint(ray.GetPoint(d));
        }
        else
        {
            return BookPanel.InverseTransformPoint(mouseScreenPos);
        }
    }

    void Update()
    {
        if (pageDragging && interactable)
            UpdateBook();
    }

    // ========================================================================
    // Page update
    // ========================================================================
    public void UpdateBook()
    {
        f = Vector3.Lerp(f, transformPoint(Input.mousePosition), Time.deltaTime * 10);

        if (mode == FlipMode.RightToLeft)
            UpdateBookRTLToPoint(f);
        else
            UpdateBookLTRToPoint(f);
    }

    // ========================================================================
    // 以下全部完整保留你「原始版本」的內容
    // 不做任何破壞原始翻頁行為的更動
    // ========================================================================

    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
        f = followLocation;

        ShadowLTR.transform.SetParent(ClippingPlane.transform, true);
        ShadowLTR.transform.localEulerAngles = Vector3.zero;
        ShadowLTR.transform.localPosition = Vector3.zero;

        Left.transform.SetParent(ClippingPlane.transform, true);

        Right.transform.SetParent(BookPanel.transform, true);
        Right.transform.localEulerAngles = Vector3.zero;

        LeftNext.transform.SetParent(BookPanel.transform, true);

        c = Calc_C_Position(followLocation);
        Vector3 t1;

        float clipAngle = CalcClipAngle(c, ebl, out t1);
        clipAngle = (clipAngle + 180) % 180;

        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        Left.transform.position = BookPanel.TransformPoint(c);

        float ang =
            Mathf.Atan2(t1.y - c.y, t1.x - c.x) * Mathf.Rad2Deg;

        Left.transform.localEulerAngles = new Vector3(0, 0, ang - 90 - clipAngle);

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);

        LeftNext.transform.SetParent(NextPageClip.transform, true);

        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();

        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
    }

    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
        mode = FlipMode.RightToLeft;
        f = followLocation;

        Shadow.transform.SetParent(ClippingPlane.transform, true);
        Shadow.transform.localPosition = Vector3.zero;
        Shadow.transform.localEulerAngles = Vector3.zero;

        Right.transform.SetParent(ClippingPlane.transform, true);

        Left.transform.SetParent(BookPanel.transform, true);
        Left.transform.localEulerAngles = Vector3.zero;

        RightNext.transform.SetParent(BookPanel.transform, true);

        c = Calc_C_Position(followLocation);
        Vector3 t1;

        float clipAngle = CalcClipAngle(c, ebr, out t1);

        if (clipAngle > -90) clipAngle += 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        Right.transform.position = BookPanel.TransformPoint(c);

        float ang =
            Mathf.Atan2(t1.y - c.y, t1.x - c.x) * Mathf.Rad2Deg;

        Right.transform.localEulerAngles =
            new Vector3(0, 0, ang - (clipAngle + 90));

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);

        RightNext.transform.SetParent(NextPageClip.transform, true);

        Left.transform.SetParent(ClippingPlane.transform, true);
        Left.transform.SetAsFirstSibling();

        Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }

    private float CalcClipAngle(Vector3 c, Vector3 bookCorner, out Vector3 t1)
    {
        Vector3 t0 = (c + bookCorner) / 2;

        float ang =
            Mathf.Atan2(bookCorner.y - t0.y, bookCorner.x - t0.x);

        float T1x = t0.x - (bookCorner.y - t0.y) * Mathf.Tan(ang);
        T1x = normalizeT1X(T1x, bookCorner, sb);

        t1 = new Vector3(T1x, sb.y, 0);

        return Mathf.Atan2(t1.y - t0.y, t1.x - t0.x) * Mathf.Rad2Deg;
    }

    private float normalizeT1X(float x, Vector3 corner, Vector3 sb)
    {
        if (x > sb.x && sb.x > corner.x) return sb.x;
        if (x < sb.x && sb.x < corner.x) return sb.x;
        return x;
    }

    private Vector3 Calc_C_Position(Vector3 followLocation)
    {
        f = followLocation;

        float angFSB =
            Mathf.Atan2(f.y - sb.y, f.x - sb.x);

        Vector3 r1 =
            new Vector3(radius1 * Mathf.Cos(angFSB),
                        radius1 * Mathf.Sin(angFSB), 0) + sb;

        Vector3 ctemp =
            Vector2.Distance(f, sb) < radius1 ? f : r1;

        float angFST =
            Mathf.Atan2(ctemp.y - st.y, ctemp.x - st.x);

        Vector3 r2 =
            new Vector3(radius2 * Mathf.Cos(angFST),
                        radius2 * Mathf.Sin(angFST), 0) + st;

        if (Vector2.Distance(ctemp, st) > radius2)
            ctemp = r2;

        return ctemp;
    }

    // ========================================================================
    // dragging + releasing
    // ========================================================================
    public void DragRightPageToPoint(Vector3 point)
    {
        if (currentPage >= bookPages.Length) return;

        pageDragging = true;
        mode = FlipMode.RightToLeft;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(0, 0);
        Left.transform.position = RightNext.transform.position;
        Left.transform.eulerAngles = Vector3.zero;

        Left.sprite =
            (currentPage < bookPages.Length) ? bookPages[currentPage] : background;

        Left.transform.SetAsFirstSibling();

        Right.gameObject.SetActive(true);
        Right.transform.position = RightNext.transform.position;
        Right.transform.eulerAngles = Vector3.zero;

        Right.sprite =
            (currentPage < bookPages.Length - 1) ? bookPages[currentPage + 1] : background;

        RightNext.sprite =
            (currentPage < bookPages.Length - 2) ? bookPages[currentPage + 2] : background;

        LeftNext.transform.SetAsFirstSibling();

        if (enableShadowEffect)
            Shadow.gameObject.SetActive(true);

        UpdateBookRTLToPoint(f);
    }

    public void DragLeftPageToPoint(Vector3 point)
    {
        if (currentPage <= 0) return;

        pageDragging = true;
        mode = FlipMode.LeftToRight;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

        Right.gameObject.SetActive(true);
        Right.transform.position = LeftNext.transform.position;
        Right.transform.eulerAngles = Vector3.zero;

        Right.sprite = bookPages[currentPage - 1];
        Right.transform.SetAsFirstSibling();

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(1, 0);
        Left.transform.position = LeftNext.transform.position;
        Left.transform.eulerAngles = Vector3.zero;

        Left.sprite =
            (currentPage >= 2) ? bookPages[currentPage - 2] : background;

        LeftNext.sprite =
            (currentPage >= 3) ? bookPages[currentPage - 3] : background;

        RightNext.transform.SetAsFirstSibling();

        if (enableShadowEffect)
            ShadowLTR.gameObject.SetActive(true);

        UpdateBookLTRToPoint(f);
    }

    public void OnMouseDragRightPage()
    {
        if (interactable)
            DragRightPageToPoint(transformPoint(Input.mousePosition));
    }

    public void OnMouseDragLeftPage()
    {
        if (interactable)
            DragLeftPageToPoint(transformPoint(Input.mousePosition));
    }

    public void OnMouseRelease()
    {
        if (interactable)
            ReleasePage();
    }

    public void ReleasePage()
    {
        if (!pageDragging) return;

        pageDragging = false;

        float distLeft = Vector2.Distance(c, ebl);
        float distRight = Vector2.Distance(c, ebr);

        if (mode == FlipMode.RightToLeft)
        {
            if (distRight < distLeft) TweenBack();
            else TweenForward();
        }
        else
        {
            if (distRight > distLeft) TweenBack();
            else TweenForward();
        }
    }

    // ========================================================================
    // Tween
    // ========================================================================
    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        int steps = (int)(duration / 0.025f);
        Vector3 displacement = (to - f) / steps;

        for (int i = 0; i < steps - 1; i++)
        {
            if (mode == FlipMode.RightToLeft)
                UpdateBookRTLToPoint(f + displacement);
            else
                UpdateBookLTRToPoint(f + displacement);

            yield return new WaitForSeconds(0.025f);
        }

        onFinish?.Invoke();
    }

    public void TweenForward()
    {
        if (mode == FlipMode.RightToLeft)
            StartCoroutine(TweenTo(ebl, 0.15f, Flip));
        else
            StartCoroutine(TweenTo(ebr, 0.15f, Flip));
    }

    void Flip()
    {
        if (mode == FlipMode.RightToLeft)
            currentPage += 2;
        else
            currentPage -= 2;

        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.transform.SetParent(BookPanel.transform, true);
        Left.gameObject.SetActive(false);

        RightNext.transform.SetParent(BookPanel.transform, true);
        Right.transform.SetParent(BookPanel.transform, true);
        Right.gameObject.SetActive(false);

        UpdateSprites();

        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);

        OnFlip?.Invoke();
    }

    public void TweenBack()
    {
        if (mode == FlipMode.RightToLeft)
        {
            StartCoroutine(TweenTo(ebr, 0.15f, () =>
            {
                UpdateSprites();

                RightNext.transform.SetParent(BookPanel.transform);
                Right.transform.SetParent(BookPanel.transform);

                Left.gameObject.SetActive(false);
                Right.gameObject.SetActive(false);
                pageDragging = false;
            }));
        }
        else
        {
            StartCoroutine(TweenTo(ebl, 0.15f, () =>
            {
                UpdateSprites();

                LeftNext.transform.SetParent(BookPanel.transform);
                Left.transform.SetParent(BookPanel.transform);

                Left.gameObject.SetActive(false);
                Right.gameObject.SetActive(false);
                pageDragging = false;
            }));
        }
    }

    // ========================================================================
    // sprite update
    // ========================================================================
    void UpdateSprites()
    {
        LeftNext.sprite =
            (currentPage > 0 && currentPage <= bookPages.Length)
            ? bookPages[currentPage - 1]
            : background;

        RightNext.sprite =
            (currentPage >= 0 && currentPage < bookPages.Length)
            ? bookPages[currentPage]
            : background;
    }
}
