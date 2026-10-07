using UnityEngine;
using UnityEngine.UI;

public class DamageFlash : MonoBehaviour
{
    [Header("References")]
    public Image flashImage;

    [Header("Settings")]
    public float maxAlpha = 0.5f;   // How strong the flash appears
    public float fadeSpeed = 2f;    // How fast it fades away

    private float currentAlpha = 0f;

    void Start()
    {
        if (flashImage == null)
            flashImage = GetComponent<Image>();
    }

    void Update()
    {
        if (currentAlpha > 0f)
        {
            currentAlpha -= fadeSpeed * Time.deltaTime;
            if (currentAlpha < 0f) currentAlpha = 0f;

            Color c = flashImage.color;
            c.a = currentAlpha;
            flashImage.color = c;
        }
    }

    public void Flash()
    {
        currentAlpha = maxAlpha;

        Color c = flashImage.color;
        c.a = currentAlpha;
        flashImage.color = c;
    }
}
