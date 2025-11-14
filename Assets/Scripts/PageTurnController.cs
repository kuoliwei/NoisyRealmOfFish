using UnityEngine;

public class PageTurnController : MonoBehaviour
{
    // 連接 mesh 的材質
    [SerializeField] private Material pageMaterial;

    // 翻頁速度
    [SerializeField] private float turnSpeed = 1.0f;

    // 當前翻頁進度 0~1
    private float currentTurn = 0f;

    // 是否正在翻頁
    private bool isTurning = false;

    // 翻到哪裡
    private float targetTurn = 0f;


    private void Reset()
    {
        // 自動抓 mesh renderer 的材質
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            pageMaterial = mr.material;
        }
    }

    private void Update()
    {
        if (isTurning)
        {
            // 插值翻頁
            currentTurn = Mathf.MoveTowards(currentTurn, targetTurn, Time.deltaTime * turnSpeed);

            // 寫回 shader
            pageMaterial.SetFloat("_Turn", currentTurn);

            // 結束翻頁
            if (Mathf.Abs(currentTurn - targetTurn) < 0.001f)
            {
                isTurning = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
            TurnForward();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            TurnBackward();

    }


    // ======== 對外 API ========

    // 手動設置翻頁進度（例如拖曳控制）
    public void SetTurn(float value)
    {
        currentTurn = Mathf.Clamp01(value);
        pageMaterial.SetFloat("_Turn", currentTurn);
    }

    // 自動播放翻頁到 1
    public void TurnForward()
    {
        targetTurn = 1f;
        isTurning = true;
    }

    // 翻回上一頁（回到 0）
    public void TurnBackward()
    {
        targetTurn = 0f;
        isTurning = true;
    }

    // 設置前後圖（可動態換）
    public void SetFrontTexture(Texture tex)
    {
        pageMaterial.SetTexture("_FrontTex", tex);
    }

    public void SetBackTexture(Texture tex)
    {
        pageMaterial.SetTexture("_BackTex", tex);
    }
}
