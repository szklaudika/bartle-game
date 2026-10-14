using UnityEngine;
using UnityEngine.AI;

// Drevorubač s motorovou pílou: chodí od stromu k stromu a rúbe les.
// Ak sa hráč priblíži (alebo ho trafí), zaútočí na neho pílou.
// Ak je v scéne NavMesh, použije NavMeshAgent, inak kráča priamo po teréne.
[RequireComponent(typeof(EnemyHealth))]
public class TreeCutterAI : MonoBehaviour
{
    public enum State { Searching, GoingToTree, Cutting, Chasing, Attacking, Dead }

    [Header("Stromy")]
    public float treeSearchRadius = 60f;
    public float cutDistance = 1.6f;          // ako ďaleko od kmeňa stojí pri rúbaní
    public float cutDuration = 6f;
    public float pauseAfterCut = 2f;

    [Header("Hráč")]
    [SerializeField] private PlayerHealth player;
    public float aggroRadius = 12f;
    public float loseInterestRadius = 25f;
    public float aggroAfterHitTime = 15f;     // ako dlho prenasleduje hráča po zásahu
    public int killReward = 25;               // peniaze pre hráča za porazenie

    [Header("Útok")]
    public float attackRange = 1.8f;
    public float damagePerTick = 6f;
    public float damageInterval = 0.4f;

    [Header("Pohyb")]
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public float turnSpeed = 6f;

    [Header("Zvuk")]
    [Tooltip("Slučka motorovej píly. Ak je prázdne, vygeneruje sa procedurálne.")]
    public AudioClip chainsawLoop;
    public float idlePitch = 0.8f;
    public float revPitch = 1.45f;

    public State CurrentState { get; private set; } = State.Searching;
    public bool IsRevving => CurrentState == State.Cutting || CurrentState == State.Attacking;
    public float CurrentSpeed { get; private set; }

    private EnemyHealth _health;
    private NavMeshAgent _agent;
    private AudioSource _audio;
    private Animator _animator;
    private readonly System.Collections.Generic.HashSet<int> _animatorParams = new();

    private TreeTarget _tree;
    private float _stateTimer;
    private float _damageTimer;
    private float _aggroTimer;
    private float _repathTimer;
    private Vector3 _lastPosition;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsCuttingHash = Animator.StringToHash("IsCutting");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null && _animator.runtimeAnimatorController != null)
            foreach (AnimatorControllerParameter p in _animator.parameters)
                _animatorParams.Add(p.nameHash);
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
    }

    private void OnDisable()
    {
        _health.Damaged -= OnDamaged;
        _health.Died -= OnDied;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();

        // NavMeshAgent pridáme len vtedy, keď je pod drevorubačom NavMesh
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null && NavMesh.SamplePosition(transform.position, out _, 2f, NavMesh.AllAreas))
        {
            _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.radius = 0.4f;
            _agent.height = 1.8f;
            _agent.acceleration = 12f;
        }
        if (_agent != null) _agent.updateRotation = false;

        _audio.clip = chainsawLoop != null ? chainsawLoop : ChainsawSound.Clip;
        _audio.loop = true;
        _audio.spatialBlend = 1f;
        _audio.rolloffMode = AudioRolloffMode.Linear;
        _audio.minDistance = 3f;
        _audio.maxDistance = 45f;
        _audio.pitch = idlePitch;
        _audio.Play();

        _lastPosition = transform.position;
    }

    private void Update()
    {
        if (CurrentState == State.Dead) return;

        _stateTimer -= Time.deltaTime;
        _aggroTimer -= Time.deltaTime;
        _damageTimer -= Time.deltaTime;

        UpdatePlayerAwareness();

        switch (CurrentState)
        {
            case State.Searching:   UpdateSearching(); break;
            case State.GoingToTree: UpdateGoingToTree(); break;
            case State.Cutting:     UpdateCutting(); break;
            case State.Chasing:     UpdateChasing(); break;
            case State.Attacking:   UpdateAttacking(); break;
        }

        CurrentSpeed = (transform.position - _lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPosition = transform.position;

        UpdateAudio();
        UpdateAnimator();
    }

    // ---------- Stavy ----------

    private void UpdatePlayerAwareness()
    {
        bool playerAlive = player != null && player.CurrentHealthPercent() > 0f;
        bool hunting = CurrentState == State.Chasing || CurrentState == State.Attacking;

        if (!playerAlive)
        {
            if (hunting) SetState(State.Searching);
            return;
        }

        float dist = DistanceToPlayer();
        if (!hunting && (dist < aggroRadius || _aggroTimer > 0f))
        {
            ReleaseTree();
            SetState(State.Chasing);
        }
        else if (hunting && dist > loseInterestRadius && _aggroTimer <= 0f)
        {
            SetState(State.Searching);
        }
    }

    private void UpdateSearching()
    {
        Stop();
        if (_stateTimer > 0f) return;

        _tree = TerrainTreeCutting.Instance.ClaimNearestTree(transform.position, treeSearchRadius);
        if (_tree != null)
            SetState(State.GoingToTree);
        else
            _stateTimer = 3f; // v okolí nie je strom, skúsi neskôr
    }

    private void UpdateGoingToTree()
    {
        Vector3 toTree = Flat(_tree.worldPosition - transform.position);
        if (toTree.magnitude <= cutDistance + 0.2f)
        {
            SetState(State.Cutting);
            _stateTimer = cutDuration;
            return;
        }

        // cieľ je bod pred kmeňom, na strane drevorubača
        Vector3 standPoint = _tree.worldPosition - toTree.normalized * cutDistance;
        MoveTo(standPoint, walkSpeed);

        // ak sa k stromu dlho nevie dostať, vyberie si iný
        if (_stateTimer <= 0f)
        {
            ReleaseTree();
            SetState(State.Searching);
        }
    }

    private void UpdateCutting()
    {
        Stop();
        Face(_tree.worldPosition);
        if (_stateTimer > 0f) return;

        Vector3 fallDir = Flat(_tree.worldPosition - transform.position);
        Vector3 pos = _tree.worldPosition;
        if (TerrainTreeCutting.Instance.CutTree(_tree, fallDir))
            Log("tree_cut", pos, name);

        _tree = null;
        SetState(State.Searching);
        _stateTimer = pauseAfterCut;
    }

    private void UpdateChasing()
    {
        if (DistanceToPlayer() <= attackRange)
        {
            SetState(State.Attacking);
            return;
        }
        MoveTo(player.transform.position, runSpeed);
    }

    private void UpdateAttacking()
    {
        Stop();
        Face(player.transform.position);

        if (DistanceToPlayer() > attackRange + 0.4f)
        {
            SetState(State.Chasing);
            return;
        }

        if (_damageTimer <= 0f)
        {
            player.TakeDamage(damagePerTick);
            _damageTimer = damageInterval;
        }
    }

    private void SetState(State state)
    {
        CurrentState = state;
        // pri ceste k stromu slúži časovač ako limit na dosiahnutie stromu
        _stateTimer = state == State.GoingToTree ? 30f : 0f;
    }

    // ---------- Reakcie na zdravie ----------

    private void OnDamaged(float amount)
    {
        _aggroTimer = aggroAfterHitTime;
    }

    private void OnDied()
    {
        ReleaseTree();
        Stop();
        CurrentState = State.Dead;
        CurrentSpeed = 0f;

        if (_agent != null) _agent.enabled = false;
        if (_animator != null && HasParameter(DieHash)) _animator.SetTrigger(DieHash);
        UpdateAnimator();

        _audio.Stop();
        Log("enemy_kill", transform.position, name);
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddMoney(killReward, transform.position, name);
        if (QuestManager.Instance != null)
            QuestManager.Instance.LumberjackKilled(name, transform.position);
        enabled = false;
    }

    private void OnDestroy() => ReleaseTree();

    private void ReleaseTree()
    {
        if (_tree != null && TerrainTreeCutting.HasInstance)
            TerrainTreeCutting.Instance.Release(_tree);
        _tree = null;
    }

    // ---------- Pohyb ----------

    private void MoveTo(Vector3 target, float speed)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.speed = speed;
            _agent.isStopped = false;
            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                _agent.SetDestination(target);
                _repathTimer = 0.25f;
            }
            if (_agent.desiredVelocity.sqrMagnitude > 0.01f)
                Face(transform.position + _agent.desiredVelocity);
            return;
        }

        // bez NavMeshu: priamo k cieľu a výška podľa terénu
        Vector3 dir = Flat(target - transform.position);
        if (dir.sqrMagnitude < 0.01f) return;

        Vector3 next = transform.position + dir.normalized * Mathf.Min(speed * Time.deltaTime, dir.magnitude);
        if (TerrainTreeCutting.TryGetGroundHeight(next, out float ground))
            next.y = ground;
        transform.position = next;
        Face(target);
    }

    private void Stop()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    private void Face(Vector3 target)
    {
        Vector3 dir = Flat(target - transform.position);
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
    }

    private float DistanceToPlayer() => Flat(player.transform.position - transform.position).magnitude;

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ---------- Zvuk a animácie ----------

    private void UpdateAudio()
    {
        float target = IsRevving ? revPitch : idlePitch;
        if (IsRevving) target += Mathf.PerlinNoise(Time.time * 3f, 0f) * 0.15f; // kolísanie otáčok
        _audio.pitch = Mathf.Lerp(_audio.pitch, target, Time.deltaTime * 5f);
    }

    // Animator je voliteľný – parametre sa nastavia, len ak v controlleri existujú.
    private void UpdateAnimator()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return;
        if (HasParameter(SpeedHash)) _animator.SetFloat(SpeedHash, CurrentSpeed);
        if (HasParameter(IsCuttingHash)) _animator.SetBool(IsCuttingHash, CurrentState == State.Cutting);
        if (HasParameter(IsAttackingHash)) _animator.SetBool(IsAttackingHash, CurrentState == State.Attacking);
    }

    private bool HasParameter(int hash) => _animatorParams.Contains(hash);

    private static void Log(string eventName, Vector3 pos, string target)
    {
        if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
            FirebaseGameAnalytics.Instance.LogGameplayEvent(eventName, pos, target);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
