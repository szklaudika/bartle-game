using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f;
    public float damage = 20f;
    public float lifeTime = 2f;

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;  
    public float explosionRadius = 5f;
    public float explosionDamage = 20f;
    public float explosionForce = 500f;  // Force for shockwave
    public bool applyShockwave = true;

    private Rigidbody rb;
    private bool hasHit = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        rb.useGravity = false;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (other.CompareTag("Player")) return;

        hasHit = true;

        // Trigger explosion effects
        Explode();

        Destroy(gameObject);
    }

    private void Explode()
{
    // Spawn explosion visual FX
    if (explosionPrefab != null)
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

    // Get all colliders within the blast radius
    Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

    foreach (Collider hit in hits)
    {
        // EXCLUDE PLAYER from AoE
        if (hit.CompareTag("Player")) 
            continue;

        // 1. DAMAGE enemies inside radius
        EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(explosionDamage);
        }

        // 2. SHOCKWAVE force on rigidbodies
        if (applyShockwave)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(
                    explosionForce, 
                    transform.position, 
                    explosionRadius, 
                    1f, 
                    ForceMode.Impulse
                );
            }
        }
    }
}


    private void OnDrawGizmosSelected()
    {
        // Draw explosion radius in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
