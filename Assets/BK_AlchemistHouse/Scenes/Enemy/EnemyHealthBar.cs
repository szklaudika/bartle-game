using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    public EnemyHealth enemyHealth;
    public Image fillImage;

    [Header("Billboard Settings")]
    public bool faceCamera = true;

    private Camera mainCam;

    void Start()
    {
        if (enemyHealth == null)
        {
            // Try to auto-assign if placed as child of enemy root
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        if (fillImage == null)
        {
            Debug.LogWarning("EnemyHealthBar: Fill Image not assigned.");
        }

        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (enemyHealth == null || fillImage == null) return;

        // 1) Update fill based on health
        float hpPercent = Mathf.Clamp01(enemyHealth.HealthPercent);
        fillImage.fillAmount = hpPercent;

        // 2) Update color (Green -> Yellow -> Red)
        fillImage.color = GetHealthColor(hpPercent);

        // 3) Make the bar face the camera (billboard)
        if (faceCamera && mainCam != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - mainCam.transform.position
            );
        }
    }

    private Color GetHealthColor(float t)
    {
        // t is 0..1 (0 = dead, 1 = full)
        // 1 -> 0.5: Green -> Yellow
        // 0.5 -> 0: Yellow -> Red
        if (t > 0.5f)
        {
            float lerp = (t - 0.5f) / 0.5f; // 0..1
            return Color.Lerp(Color.yellow, Color.green, lerp);
        }
        else
        {
            float lerp = t / 0.5f; // 0..1
            return Color.Lerp(Color.red, Color.yellow, lerp);
        }
    }
}
