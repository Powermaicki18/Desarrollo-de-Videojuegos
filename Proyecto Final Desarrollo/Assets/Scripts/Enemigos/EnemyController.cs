using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float turnSpeed = 360f;
    public float gravity = -25f;

    [Header("Vuelo")]
    public bool isFlying = false;
    public float flyHeight = 2f;            // altura sobre el jugador
    public float bobAmplitude = 0.25f;      // vaivén vertical
    public float bobSpeed = 2f;
    public float verticalSpeed = 3f;        // qué tan rápido sube o baja a su altura

    [Header("IA")]
    public float detectRange = 12f;
    public float attackRange = 1.6f;        // se mide hasta el borde del hurtbox del jugador
    public float attackCooldown = 1.5f;
    public float attackLockTime = 1.0f;     // duración del clip de ataque

    [Header("NavMesh (solo terrestres)")]
    public bool useNavMesh = true;
    public float repathInterval = 0.25f;
    public float cornerReachDistance = 0.4f;

    [Header("Daño")]
    public float damage = 10f;
    public float hitDelay = 0.4f;
    public float hitRangeExtra = 0.5f;

    [Header("Muerte")]
    public float destroyDelay = 2.5f;       // segundos que se ve el cuerpo (>= duración del clip)
    public float sinkTime = 0.8f;
    public float sinkDistance = 1.2f;

    [Header("Referencias")]
    public Animator animator;
    public Transform target;

    CharacterController cc;
    Health health;
    Health targetHealth;
    float verticalVel, lockUntil, nextAttack, hitAt, bobPhase, hoverY;
    bool hitPending, dead, hasHoverY;
    bool hasSpeed, hasAttack, hasDie;

    NavMeshPath path;
    Vector3[] corners;
    int cornerIndex;
    float repathAt;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        path = new NavMeshPath();
        repathAt = Time.time + Random.value * repathInterval;
        bobPhase = Random.value * 6.28f;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.applyRootMotion = false;
            foreach (var p in animator.parameters)          // solo usa los parámetros que existan
            {
                if (p.nameHash == SpeedHash) hasSpeed = true;
                if (p.nameHash == AttackHash) hasAttack = true;
                if (p.nameHash == DieHash) hasDie = true;
            }
        }
    }

    void Update()
    {
        if (dead) return;

        // Muerte
        if (health && health.IsDead)
        {
            dead = true;
            hitPending = false;
            if (!isFlying) cc.enabled = false;              // el volador sigue activo para caer al suelo
            if (animator)
            {
                if (hasSpeed) animator.SetFloat(SpeedHash, 0f);
                if (hasAttack) animator.ResetTrigger(AttackHash);
                if (hasDie) animator.SetTrigger(DieHash);
            }
            StartCoroutine(DeathRoutine());
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
                        if (animator && hasAttack) animator.SetTrigger(AttackHash);
                    }
                }
                else if (dist <= detectRange)
                {
                    bool useNav = useNavMesh && !isFlying;
                    Vector3 moveDir = useNav ? PathDirection(target.position, dir) : dir;
                    Face(moveDir);
                    move = moveDir * moveSpeed;
                }
            }
        }

        if (hitPending && Time.time >= hitAt)
        {
            hitPending = false;
            DealDamage();
        }

        Vector3 velocity = move;
        if (isFlying)
        {
            // Flota a flyHeight sobre el jugador, con un leve vaivén. Sin gravedad.
            if (target) { hoverY = target.position.y + flyHeight; hasHoverY = true; }
            if (!hasHoverY) { hoverY = transform.position.y; hasHoverY = true; }
            float desiredY = hoverY + Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
            velocity.y = Mathf.Clamp((desiredY - transform.position.y) * 3f, -verticalSpeed, verticalSpeed);
        }
        else
        {
            if (cc.isGrounded && verticalVel < 0) verticalVel = -2f;
            verticalVel += gravity * Time.deltaTime;
            velocity.y = verticalVel;
        }
        cc.Move(velocity * Time.deltaTime);

        if (animator && hasSpeed)
            animator.SetFloat(SpeedHash, move.magnitude, 0.1f, Time.deltaTime);
    }

    // Cae al suelo (si vuela), se queda tirado mientras suena la animación, se hunde y desaparece
    IEnumerator DeathRoutine()
    {
        if (isFlying)
        {
            float vy = 0f, limit = Time.time + 2f;
            while (!cc.isGrounded && Time.time < limit)
            {
                vy += gravity * Time.deltaTime;
                cc.Move(Vector3.up * vy * Time.deltaTime);
                yield return null;
            }
            cc.enabled = false;
        }

        yield return new WaitForSeconds(destroyDelay);

        Vector3 start = transform.position;
        float t = 0f;
        while (t < sinkTime)
        {
            t += Time.deltaTime;
            transform.position = start + Vector3.down * sinkDistance * Mathf.Clamp01(t / sinkTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    float EdgeDistance()
    {
        Vector3 edge = targetHealth ? targetHealth.ClosestPoint(transform.position) : target.position;
        edge.y = transform.position.y;
        return (edge - transform.position).magnitude;
    }

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