using UnityEngine;

public class StarRippleSineTester : MonoBehaviour
{
    [SerializeField] private Material rippleMat;
    public float frequency = 20f;
    public float phaseSpeed = 2f;

    private float phase = 0f;

    void Update()
    {
        phase += Time.deltaTime * phaseSpeed;

        rippleMat.SetFloat("_Frequency", frequency);
        rippleMat.SetFloat("_Phase", phase);
    }
}
