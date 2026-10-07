using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private PlayerHealth playerHealth;

    [Header("Distances (Scaled for tiny characters ~0.1 units)")]
    public float walkDistance = 1.2f;       // starts walking toward player
    public float runDistance = 0.6f;        // starts running/chasing player
    public float attackDistance = 0.15f;    // distance to attack player

    [Header("Movement Speeds")]
    public float walkSpeed = 0.3f;
    public float runSpeed = 0.6f;

    [Header("Random Walk Settings")]
    public float wanderRadius = 0.8f;
    public float wanderInterval = 2f;
    private float wanderTimer = 0f;

    [Header("Attack Settings")]
    public float attackDamage = 20f;
    public float attackCooldown = 1.2f;
    private float attackTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        playerHealth = player.GetComponent<PlayerHealth>();

        if (!agent.isOnNavMesh)
            Debug.LogError($"{name} is NOT on NavMesh at start!");
    }

    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);
        attackTimer -= Time.deltaTime;

        bool isWalking = false;
        bool isRunning = false;
        bool isAttacking = false;

        if (dist < attackDistance)
        {
            agent.isStopped = true;
            isAttacking = true;
            agent.SetDestination(transform.position);

            // 🔥 Deal damage if cooldown finished
            if (attackTimer <= 0f && playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                attackTimer = attackCooldown;
            }
        }
        else if (dist < runDistance)
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(player.position);
            isRunning = true;
        }
        else if (dist < walkDistance)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(player.position);
            isWalking = true;
        }
        else
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f)
            {
                Vector3 newPos = RandomNavmeshPoint(transform.position, wanderRadius);
                if (agent.isOnNavMesh) agent.SetDestination(newPos);
                wanderTimer = wanderInterval;
            }

            isWalking = agent.velocity.magnitude > 0.01f;
        }

        if (agent.isStopped || agent.velocity.magnitude < 0.005f)
        {
            isWalking = false;
            isRunning = false;
        }

        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsAttacking", isAttacking);
    }

    Vector3 RandomNavmeshPoint(Vector3 origin, float dist)
    {
        Vector3 rand = origin + Random.insideUnitSphere * dist;
        rand.y = origin.y;

        NavMeshHit hit;
        NavMesh.SamplePosition(rand, out hit, dist, NavMesh.AllAreas);

        return hit.position;
    }
}
