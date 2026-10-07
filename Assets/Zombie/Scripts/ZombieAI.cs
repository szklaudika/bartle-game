using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Distances")]
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackLeeway = 0.2f;

    [Header("Attack")]
    [SerializeField] private float timeBetweenAttacks = 1.2f;
    [SerializeField] private float attackLockTime = 1.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip walkLoopSound;
    [SerializeField] private AudioClip attackSound;

    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private AudioSource audioSource;

    private int speedHash;
    private int isAttackingHash;
    private int attackTriggerHash;

    private float lastAttackTime;
    private bool isAttackingState = false;
    private float attackStateEndTime;

    private void Awake()
    {
        agent       = GetComponent<NavMeshAgent>();
        animator    = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        agent.updatePosition = true;
        agent.updateRotation = false;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        speedHash         = Animator.StringToHash("Speed");
        isAttackingHash   = Animator.StringToHash("IsAttacking");
        attackTriggerHash = Animator.StringToHash("AttackTrigger");

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; 
            audioSource.volume = 1f;       
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (isAttackingState)
        {
            agent.isStopped      = true;
            agent.updatePosition = false;
            agent.ResetPath();
            animator.SetFloat(speedHash, 0f);
            FacePlayer();


            if (Time.time >= attackStateEndTime)
            {
                isAttackingState = false;
                animator.SetBool(isAttackingHash, false);
                agent.updatePosition = true;
            }

            return;
        }

        float distance        = Vector3.Distance(transform.position, player.position);
        float attackThreshold = attackRange + attackLeeway;

        if (distance > chaseRange)
        {
            Idle();
        }
        else if (distance > attackThreshold)
        {
            Chase();
        }
        else
        {
            TryStartAttack();
        }
    }

    private void Idle()
    {
        agent.isStopped = true;
        animator.SetBool(isAttackingHash, false);
        animator.SetFloat(speedHash, 0f);

        StopWalkSound();
    }

    private void Chase()
    {
        agent.isStopped      = false;
        agent.updatePosition = true;

        agent.SetDestination(player.position);

        float speed = agent.velocity.magnitude;
        animator.SetBool(isAttackingHash, false);
        animator.SetFloat(speedHash, speed);

        FacePlayer();

        if (speed > 0.1f)
            PlayWalkSound();
        else
            StopWalkSound();
    }

    private void TryStartAttack()
    {
        if (Time.time < lastAttackTime + timeBetweenAttacks)
        {
            agent.isStopped      = true;
            agent.updatePosition = false;
            agent.ResetPath();
            animator.SetBool(isAttackingHash, false);
            animator.SetFloat(speedHash, 0f);
            FacePlayer();

            StopWalkSound();
            return;
        }

        isAttackingState   = true;
        attackStateEndTime = Time.time + attackLockTime;
        lastAttackTime     = Time.time;

        agent.isStopped      = true;
        agent.updatePosition = false;
        agent.ResetPath();
        animator.SetFloat(speedHash, 0f);
        animator.SetBool(isAttackingHash, true);
        animator.SetTrigger(attackTriggerHash);

        StopWalkSound();
        PlayAttackSound();   

        FacePlayer();
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    private void PlayWalkSound()
    {
        if (audioSource == null || walkLoopSound == null)
            return;

        if (audioSource.isPlaying && audioSource.clip == walkLoopSound)
            return;

        audioSource.clip = walkLoopSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopWalkSound()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying && audioSource.clip == walkLoopSound)
            audioSource.Stop();
    }

    private void PlayAttackSound()
    {
        Debug.Log("PlayAttackSound HÍVVA");
        if (audioSource == null || attackSound == null)
            return;

        audioSource.PlayOneShot(attackSound, 1f);
    }
}
