using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    private bool isDead = false;
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;

    // PlayerHUD listens to these (health bar, red flash, death screen)
    public event System.Action<float> Damaged;
    public event System.Action Died;

    [Header("UI Feedback")]
    public DamageFlash damageFlash;   // you already set this up earlier

    [Header("UI")]
    public GameObject crosshair;


    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // health bar + death screen build themselves at runtime
        if (FindFirstObjectByType<PlayerHUD>() == null)
            new GameObject("PlayerHUD").AddComponent<PlayerHUD>().Init(this);

        // money, seed shop and tree planting
        if (FindFirstObjectByType<PlayerWallet>() == null)
            new GameObject("PlayerWallet").AddComponent<PlayerWallet>().Init(this);
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

        Damaged?.Invoke(amount);

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

    if (GameOverManager.Instance != null)
        GameOverManager.Instance.ShowGameOver();

    PlayerShooting shooting = GetComponent<PlayerShooting>();
    if (shooting != null) shooting.enabled = false;

    Died?.Invoke();
}

}
