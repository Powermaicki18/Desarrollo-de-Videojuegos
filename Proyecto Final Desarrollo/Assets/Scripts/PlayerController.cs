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

    [Header("Animación del golpe (evita que se corte al hacer spam)")]
    [Tooltip("Ajusta solo el tiempo de bloqueo para que nunca sea menor que la duración del clip de animación")]
    public bool autoLockFromClip = true;
    [Tooltip("Parte del nombre del clip de golpe en el Animator (ej. 'attack', 'golpe', 'punch')")]
    public string attackClipKeyword = "attack";
    [Tooltip("Parte del nombre del clip del súper (ej. 'super', 'spin')")]
    public string superClipKeyword = "super";
    [Tooltip("Si tocas el botón durante el golpe, el siguiente golpe sale en cuanto termine (segundos de memoria)")]
    public float attackQueueWindow = 0.35f;
    [Tooltip("Duración real del clip de golpe en segundos (Punch.fbx dura ~1.2 s)")]
    public float attackAnimLength = 1.2f;
    [Tooltip("Si el clip dura más que Attack Lock Time, acelera la animación para que el golpe se vea COMPLETO en ese tiempo")]
    public bool fitAnimationToLock = true;

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
    float verticalVel, lockUntil, superReadyAt, queuedAttackUntil;
    bool queuedAttack, wasBusy;
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
        SyncLockTimesWithClips();
    }

    // Que el bloqueo dure al menos lo que dura la animación, para que un golpe no corte al anterior
    void SyncLockTimesWithClips()
    {
        if (!animator || !animator.runtimeAnimatorController) return;
        string atk = (attackClipKeyword ?? "").ToLowerInvariant();
        string sup = (superClipKeyword ?? "").ToLowerInvariant();
        bool foundAtk = false, foundSup = false;

        foreach (var c in animator.runtimeAnimatorController.animationClips)
        {
            string n = c.name.ToLowerInvariant();
            if (debugLog) Debug.Log("Clip del Animator: " + c.name + " (" + c.length.ToString("0.00") + " s)");
            if (!autoLockFromClip) continue;

            bool isSuper = sup.Length > 0 && n.Contains(sup);
            if (isSuper && !foundSup)
            {
                superLockTime = Mathf.Max(superLockTime, c.length);
                foundSup = true;
            }
            else if (!isSuper && !foundAtk && ((atk.Length > 0 && n.Contains(atk)) || n.Contains("punch") || n.Contains("golpe")))
            {
                if (fitAnimationToLock) attackAnimLength = c.length;                    // se acelera el clip, el bloqueo no cambia
                else attackLockTime = Mathf.Max(attackLockTime, c.length);              // se alarga el bloqueo
                foundAtk = true;
            }
        }
        if (debugLog)
            Debug.Log("Bloqueo golpe: " + attackLockTime.ToString("0.00") + " s, súper: " + superLockTime.ToString("0.00") + " s");
    }

    public void Attack()
    {
        if (IsDead) return;
        if (IsBusy)
        {
            // el golpe actual no se interrumpe: el siguiente queda en cola y sale al terminar
            queuedAttack = true;
            queuedAttackUntil = Time.time + attackQueueWindow;
            return;
        }
        if (!DoAction(AttackHash, attackLockTime)) return;
        ApplyAnimSpeed(attackAnimLength, attackLockTime);
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
        if (animator) animator.speed = 1f;

        // La recarga empieza cuando termina la animación del súper
        superReadyAt = Time.time + superLockTime + superCooldown;

        if (health) health.GrantInvulnerability(superInvulnerableTime);
        StartCoroutine(HitWindow(superHitDelay, superHitDuration, superTickInterval,
                                 superDamage, superRange, superAngle));
    }

    // Hace que el clip completo quepa en el tiempo de bloqueo (si dura más, se reproduce más rápido)
    void ApplyAnimSpeed(float clipLen, float lockTime)
    {
        if (!animator) return;
        animator.speed = (fitAnimationToLock && lockTime > 0.05f && clipLen > lockTime) ? clipLen / lockTime : 1f;
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
        // al terminar una acción: limpiar triggers viejos para que no se reproduzcan tarde y a medias
        bool busyNow = IsBusy;
        if (wasBusy && !busyNow && animator)
        {
            animator.ResetTrigger(AttackHash);
            animator.ResetTrigger(SuperHash);
            animator.speed = 1f;
        }
        wasBusy = busyNow;

        if (queuedAttack && !busyNow)
        {
            queuedAttack = false;
            if (Time.time <= queuedAttackUntil) Attack();
        }

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