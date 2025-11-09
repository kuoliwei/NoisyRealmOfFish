using UnityEngine;

public class StarRippleController_Debug : MonoBehaviour
{
    [SerializeField] private Material rippleMat;

    public float frequency = 30f;
    public float amplitude = 0.05f;
    public float phaseSpeed = 4f;
    public float expandSpeed = 0.5f;

    private float radius = 0f;
    private float phase = 0f;

    void Start()
    {
        if (rippleMat == null)
            rippleMat = GetComponent<Renderer>().sharedMaterial;

        rippleMat.SetFloat("_Radius", radius);
        rippleMat.SetFloat("_Phase", phase);
        rippleMat.SetFloat("_Frequency", frequency);
        rippleMat.SetFloat("_Amplitude", amplitude);
        rippleMat.SetFloat("_Alpha", 1f);
    }

    void Update()
    {
        // 半徑持續擴散
        radius += Time.deltaTime * expandSpeed;
        rippleMat.SetFloat("_Radius", radius);

        // 相位持續推進
        phase += Time.deltaTime * phaseSpeed;
        rippleMat.SetFloat("_Phase", phase);

        // 實時更新波形參數
        rippleMat.SetFloat("_Frequency", frequency);
        rippleMat.SetFloat("_Amplitude", amplitude);
    }
}
