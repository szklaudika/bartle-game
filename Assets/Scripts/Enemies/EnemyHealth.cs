using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 60f;
    public float destroyDelay = 1.5f; // seconds before the body disappears
    private float currentHealth;

    private Animator animator;

    private bool isDead = false;
    public bool IsDead => isDead;

    // other enemy scripts (e.g. TreeCutterAI) react to these
    public event System.Action<float> Damaged;
    public event System.Action Died;

    // 🔹 Exposed health percentage for the UI (0–1)
    public float HealthPercent
    {
        get
        {
            if (maxHealth <= 0f) return 0f;
            return currentHealth / maxHealth;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{name} took {amount} damage, remaining health: {currentHealth}");

        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
        {
            FirebaseGameAnalytics.Instance.LogGameplayEvent(
                "hitenemy",
                transform.position,
                gameObject.name,
                Mathf.RoundToInt(amount)
            );
        }


        if (currentHealth <= 0f)
        {
            currentHealth = 0f;  // clamp so it never goes below 0
            Die();
        }
        else
        {
            Damaged?.Invoke(amount);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 🔹 Optional: hide the health bar immediately on death
        EnemyHealthBar bar = GetComponentInChildren<EnemyHealthBar>();
        if (bar != null)
        {
            bar.gameObject.SetActive(false);
        }

        // disable collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Died?.Invoke();

        // destroy the enemy after a short delay (so animation can play)
        Destroy(gameObject, destroyDelay);
    }
}
