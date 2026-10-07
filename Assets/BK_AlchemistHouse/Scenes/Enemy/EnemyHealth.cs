using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 60f;  // Spider HP
    private float currentHealth;

    private Animator animator;
    private NavMeshAgent agent;
    private SpiderAI ai;

    private bool isDead = false;

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
        agent = GetComponent<NavMeshAgent>();
        ai = GetComponent<SpiderAI>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Spider took {amount} damage, remaining health: {currentHealth}");

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

        // stop AI
        if (agent != null) agent.isStopped = true;
        if (ai != null) ai.enabled = false;

        // disable collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // destroy the spider after a short delay (so animation can play)
        Destroy(gameObject, 1.5f); // 1.5 seconds delay
    }
}
