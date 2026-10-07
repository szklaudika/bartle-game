using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    private bool isDead = false;

    [Header("UI Feedback")]
    public DamageFlash damageFlash;   // you already set this up earlier

    [Header("UI")]
    public GameObject crosshair;


    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {

        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.LogGameplayEvent(
                "takedamage",
                transform.position,
                gameObject.name,
                Mathf.RoundToInt(amount)
            );
        }


        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {amount} damage, remaining health: {currentHealth}");

        if (damageFlash != null)
        {
            damageFlash.Flash();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public float CurrentHealthPercent()
    {
        if (maxHealth <= 0f) return 0f;
        return currentHealth / maxHealth;
    }

    private void Die()
{
    if (isDead) return;
    isDead = true;

    Debug.Log("Player died!");

    // 🔻 Hide crosshair on Game Over
    if (crosshair != null)
        crosshair.SetActive(false);

    // 🔻 Hide the player health bar on Game Over
    var healthUI = FindObjectOfType<PlayerHealthUI>();
    if (healthUI != null)
        healthUI.gameObject.SetActive(false);

    GameOverManager.Instance.ShowGameOver();

    PlayerShooting shooting = GetComponent<PlayerShooting>();
    if (shooting != null) shooting.enabled = false;
}

}
