using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Image fillImage;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (fillImage == null)
            Debug.LogWarning("PlayerHealthUI: Fill Image not assigned!");
    }

    private void Update()
    {
        if (playerHealth == null || fillImage == null) return;

        float t = Mathf.Clamp01(playerHealth.CurrentHealthPercent());

        fillImage.fillAmount = t;
        fillImage.color = GetHealthColor(t);
    }

    private Color GetHealthColor(float t)
    {
        // 1→0.5: Green→Yellow, 0.5→0: Yellow→Red
        if (t > 0.5f)
        {
            float lerp = (t - 0.5f) / 0.5f;
            return Color.Lerp(Color.yellow, Color.green, lerp);
        }
        else
        {
            float lerp = t / 0.5f;
            return Color.Lerp(Color.red, Color.yellow, lerp);
        }
    }
}
