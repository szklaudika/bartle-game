using UnityEngine;

// Krátky záblesk po výbuchu ohnivej gule: guľa sa rýchlo zväčší a zmenší, svetlo zhasne.
public class FireballExplosion : MonoBehaviour
{
    public float duration = 0.45f;
    public float maxSize = 2.5f;

    private Light _light;
    private float _startIntensity;
    private float _time;

    private void Awake()
    {
        _light = GetComponentInChildren<Light>();
        if (_light != null) _startIntensity = _light.intensity;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / duration);

        // rýchly nárast, potom pomalšie zmiznutie
        float size = t < 0.3f ? Mathf.Lerp(0f, 1f, t / 0.3f) : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);
        transform.localScale = Vector3.one * (size * maxSize);
        if (_light != null) _light.intensity = _startIntensity * (1f - t);

        if (t >= 1f) Destroy(gameObject);
    }
}
