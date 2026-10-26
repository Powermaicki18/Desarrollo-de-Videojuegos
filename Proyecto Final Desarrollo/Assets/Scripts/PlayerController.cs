using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    public float turnSpeed = 720f;
    public float gravity = -25f;

    [Header("Ataques")]
    public float attackLockTime = 0.5f;
    public float superLockTime = 1.0f;
    public bool moveWhileAttacking = true;
    [Range(0f, 1f)] public float attackMoveMultiplier = 0.6f;
    [Tooltip("Enemigos a menos de esta distancia del borde siempre reciben el golpe, sin importar el ángulo")]
    public float meleeAlwaysHitRadius = 1.2f;

    [Header("Golpe: daño")]
    public float attackDamage = 20f;
    public float attackRange = 2.6f;
    public float attackAngle = 140f;
    public float attackHitDelay = 0.1f;
    public float attackHitDuration = 0.25f;
    public float attackTickInterval = 0f;

    [Header("Súper golpe: daño")]
    public float superDamage = 50f;
    public float superRange = 5f;
    public float superAngle = 360f;
    public float superHitDelay = 0.1f;
    public float superHitDuration = 0.6f;
    public float superTickInterval = 0f;
    public float superInvulnerableTime = 1.0f;

    [Header("Súper golpe: recarga")]
    [Tooltip("Segundos de recarga DESPUÉS de que termina la animación del súper. 0 = sin recarga")]
    public float superCooldown = 8f;

    [Header("Referencias")]
    public Animator animator;
    public Transform cameraTransform;

    [Header("Debug")]
    public bool debugLog = false;

    public Vector2 MoveInput { get; set; }
    public Vector3 AimDirection { get; set; }
    public bool IsAiming { get; set; }
    public bool IsBusy => Time.time < lockUntil;
    public bool IsDead => health && health.IsDead;

    // --- Para la barra de recarga ---
    public bool SuperReady => Time.time >= superReadyAt;
    public float SuperCooldownRemaining =>
        Mathf.Min(Mathf.Max(0f, superReadyAt - Time.time), Mathf.Max(0.0001f, superCooldown));
    public float SuperCharge01 =>
        superCooldown <= 0f ? 1f : 1f - SuperCooldownRemaining / superCooldown;
    public float LastSuperDeniedTime { get; private set; } = -99f;   // último intento con el súper sin cargar

    CharacterController cc;
    Health health;
    Vector3 startPos;
    float verticalVel, lockUntil, superReadyAt;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int SuperHash = Animator.StringToHash("Super");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        startPos = transform.position;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    public void Attack()
    {
        if (!DoAction(AttackHash, attackLockTime)) return;
        StartCoroutine(HitWindow(attackHitDelay, attackHitDuration, attackTickInterval,
                                 attackDamage, attackRange, attackAngle));
    }

    public void SuperAttack()
    {
        if (IsDead) return;
        if (!SuperReady)                       // aún recargando
        {
            LastSuperDeniedTime = Time.time;
            return;
        }
        if (!DoAction(SuperHash, superLockTime)) return;

        // La recarga empieza cuando termina la animación del súper
        superReadyAt = Time.time + superLockTime + superCooldown;

        if (health) health.GrantInvulnerability(superInvulnerableTime);
        StartCoroutine(HitWindow(superHitDelay, superHitDuration, superTickInterval,
                                 superDamage, superRange, superAngle));
    }

    bool DoAction(int trigger, float lockTime)
    {
        if (IsBusy || IsDead) return false;
        lockUntil = Time.time + lockTime;
        if (animator) animator.SetTrigger(trigger);
        return true;
    }

    // Radio horizontal del enemigo (para medir hasta su borde y no hasta su centro)
    float BodyRadius(Health h)
    {
        var ecc = h.GetComponent<CharacterController>();
        if (!ecc) return 0.3f;
        Vector3 s = h.transform.lossyScale;
        return ecc.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
    }

    // Durante "duration" segundos revisa todos los enemigos vivos y daña a los que estén en la zona
    IEnumerator HitWindow(float delay, float duration, float tick,
                          float damage, float range, float angle)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var lastHit = new Dictionary<Health, float>();
        float end = Time.time + duration;
        int totalHits = 0;

        do
        {
            if (IsDead) yield break;

            for (int i = Health.All.Count - 1; i >= 0; i--)
            {
                if (i >= Health.All.Count) continue;
                var h = Health.All[i];
                if (h == null || h.gameObject == gameObject || h.IsDead) continue;
                if (!h.CompareTag("Enemy")) continue;

                if (lastHit.TryGetValue(h, out float t))
                {
                    if (tick <= 0f || Time.time - t < tick) continue;
                }

                Vector3 to = h.transform.position - transform.position;
                to.y = 0f;
                float edgeDist = to.magnitude - BodyRadius(h);
                if (edgeDist > range) continue;

                if (angle < 359f && edgeDist > meleeAlwaysHitRadius && to.sqrMagnitude > 0.0001f &&
                    Vector3.Angle(transform.forward, to) > angle * 0.5f) continue;

                lastHit[h] = Time.time;
                h.TakeDamage(damage);
                totalHits++;
            }
            yield return null;
        }
        while (Time.time < end);

        if (debugLog)
            Debug.Log($"Ataque terminado: {totalHits} golpes (daño {damage}, rango {range}, ángulo {angle})");
    }

    void Update()
    {
        bool canMove = !IsDead && (moveWhileAttacking || !IsBusy);
        Vector2 input = canMove ? MoveInput : Vector2.zero;

        Vector3 fwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        fwd.y = 0; right.y = 0; fwd.Normalize(); right.Normalize();

        Vector3 dir = fwd * input.y + right * input.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = moveSpeed * (IsBusy ? attackMoveMultiplier : 1f);

        if (cc.isGrounded && verticalVel < 0) verticalVel = -2f;
        verticalVel += gravity * Time.deltaTime;
        cc.Move((dir * speed + Vector3.up * verticalVel) * Time.deltaTime);

        Vector3 face = (IsAiming && AimDirection.sqrMagnitude > 0.01f) ? AimDirection : dir;
        if (face.sqrMagnitude > 0.01f && canMove)
        {
            Quaternion t = Quaternion.LookRotation(face.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, t, turnSpeed * Time.deltaTime);
        }

        if (animator)
            animator.SetFloat(SpeedHash, dir.magnitude * speed, 0.1f, Time.deltaTime);

        if (transform.position.y < -20f)
        {
            cc.enabled = false; transform.position = startPos; cc.enabled = true; verticalVel = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, superRange);
        Gizmos.color = Color.white; Gizmos.DrawWireSphere(transform.position, meleeAlwaysHitRadius);
    }
}