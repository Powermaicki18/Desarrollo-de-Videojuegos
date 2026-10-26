using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float turnSpeed = 360f;
    public float gravity = -25f;

    [Header("IA")]
    public float detectRange = 12f;
    public float attackRange = 1.6f;        // se mide hasta el borde del hurtbox del jugador
    public float attackCooldown = 1.5f;
    public float attackLockTime = 1.0f;     // duración del clip de ataque

    [Header("NavMesh")]
    public bool useNavMesh = true;
    public float repathInterval = 0.25f;    // cada cuánto recalcula la ruta
    public float cornerReachDistance = 0.4f;

    [Header("Daño")]
    public float damage = 10f;
    public float hitDelay = 0.4f;           // segundos desde que empieza el ataque hasta que pega
    public float hitRangeExtra = 0.5f;      // margen extra por si el jugador se mueve

    [Header("Muerte")]
    public float destroyDelay = 1.5f;       // segundos hasta desaparecer tras morir

    [Header("Referencias")]
    public Animator animator;
    public Transform target;

    CharacterController cc;
    Health health;
    Health targetHealth;
    float verticalVel, lockUntil, nextAttack, hitAt;
    bool hitPending, dead;

    NavMeshPath path;
    Vector3[] corners;
    int cornerIndex;
    float repathAt;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        path = new NavMeshPath();
        repathAt = Time.time + Random.value * repathInterval;   // reparte el cálculo entre enemigos
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator) animator.applyRootMotion = false;
    }

    void Update()
    {
        if (dead) return;

        // Muerte
        if (health && health.IsDead)
        {
            dead = true;
            hitPending = false;
            if (animator) animator.SetFloat(SpeedHash, 0f);
            cc.enabled = false;
            Destroy(gameObject, destroyDelay);   // luego se cambia por una animación de muerte
            return;
        }

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (target && !targetHealth)
            targetHealth = target.GetComponent<Health>();

        Vector3 move = Vector3.zero;
        bool busy = Time.time < lockUntil;
        bool targetAlive = target && (targetHealth == null || !targetHealth.IsDead);

        if (targetAlive)
        {
            Vector3 to = target.position - transform.position;
            to.y = 0;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.01f ? to / dist : Vector3.zero;

            // distancia hasta el borde del jugador (la caja ancha con los brazos)
            float edgeDist = EdgeDistance();

            if (!busy)
            {
                if (edgeDist <= attackRange)
                {
                    Face(dir);
                    if (Time.time >= nextAttack)
                    {
                        nextAttack = Time.time + attackCooldown;
                        lockUntil = Time.time + attackLockTime;
                        hitAt = Time.time + hitDelay;
                        hitPending = true;
                        if (animator) animator.SetTrigger(AttackHash);
                    }
                }
                else if (dist <= detectRange)
                {
                    Vector3 moveDir = useNavMesh ? PathDirection(target.position, dir) : dir;
                    Face(moveDir);
                    move = moveDir * moveSpeed;
                }
            }
        }

        // Momento del golpe
        if (hitPending && Time.time >= hitAt)
        {
            hitPending = false;
            DealDamage();
        }

        if (cc.isGrounded && verticalVel < 0) verticalVel = -2f;
        verticalVel += gravity * Time.deltaTime;
        cc.Move((move + Vector3.up * verticalVel) * Time.deltaTime);

        if (animator)
            animator.SetFloat(SpeedHash, move.magnitude, 0.1f, Time.deltaTime);
    }

    // Distancia horizontal hasta el punto más cercano del jugador (hurtbox o centro)
    float EdgeDistance()
    {
        Vector3 edge = targetHealth ? targetHealth.ClosestPoint(transform.position) : target.position;
        edge.y = transform.position.y;
        return (edge - transform.position).magnitude;
    }

    // Dirección hacia la siguiente esquina del camino del NavMesh.
    // Si no hay ruta (NavMesh sin hornear, fuera de la malla), va en línea recta.
    Vector3 PathDirection(Vector3 targetPos, Vector3 directDir)
    {
        if (Time.time >= repathAt)
        {
            repathAt = Time.time + repathInterval;
            corners = null;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit a, 3f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(targetPos, out NavMeshHit b, 3f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) &&
                path.corners.Length > 1)
            {
                corners = path.corners;
                cornerIndex = 1;
            }
        }

        if (corners == null) return directDir;

        while (cornerIndex < corners.Length - 1)
        {
            Vector3 d = corners[cornerIndex] - transform.position;
            d.y = 0;
            if (d.magnitude > cornerReachDistance) break;
            cornerIndex++;
        }

        Vector3 toCorner = corners[cornerIndex] - transform.position;
        toCorner.y = 0;
        return toCorner.sqrMagnitude > 0.001f ? toCorner.normalized : directDir;
    }

    // También se puede llamar desde un Animation Event del clip de ataque
    public void DealDamage()
    {
        if (dead || !target || targetHealth == null || targetHealth.IsDead) return;
        if (EdgeDistance() <= attackRange + hitRangeExtra)
            targetHealth.TakeDamage(damage);
    }

    void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion t = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, t, turnSpeed * Time.deltaTime);
    }
}