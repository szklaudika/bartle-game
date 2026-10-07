using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    public float speed = 3f;
    public float minIntensity = 0f;
    public float maxIntensity = 5f;

    Material mat;
    Color baseColor;

    void Start()
    {
        Renderer r = GetComponentInChildren<Renderer>(); // Automaticky nájde renderer
        mat = r.material;
        baseColor = mat.GetColor("_EmissionColor");
        mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        float emission = Mathf.PingPong(Time.time * speed, maxIntensity - minIntensity) + minIntensity;
        mat.SetColor("_EmissionColor", baseColor * emission);
    }
}
