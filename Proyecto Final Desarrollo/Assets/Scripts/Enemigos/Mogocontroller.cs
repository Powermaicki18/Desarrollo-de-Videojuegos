using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Mogo: enemigo de emboscada.
// 1) Vive bajo tierra, marca en el suelo donde va a salir y emerge cerca de Tuko.
// 2) Despues se queda afuera y lo persigue a saltos. Si esta cerca, le da un golpe.
//    (Si activas "Burrow After Attack" vuelve a la version anterior: golpea y se entierra.)
public class MogoController : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public Transform target;
    [Tooltip("Hijo con el modelo. Si lo dejas vacio se hunde todo el objeto.")]
    public Transform visual;

    [Header("Comportamiento")]
    [Tooltip("OFF = sale una vez y persigue a saltos. ON = golpea, queda expuesto y se vuelve a enterrar.")]
    public bool burrowAfterAttack = false;

    [Header("Salida de la tierra")]
    public float detectRange = 16f;                  // solo sale si Tuko esta a esta distancia
    public float firstAppearDelay = 1.5f;
    public Vector2 hiddenTimeRange = new Vector2(1.5f, 3f);
    public float telegraphTime = 0.9f;               // aviso en el suelo antes de salir
    public float emergeTime = 0.8f;                  // duracion de la animacion de salir
    public float maxEmergeTime = 1.4f;               // si el clip dura mas, se acelera
    public float spawnDistance = 1.8f;               // a que distancia de Tuko aparece
    public float navSampleRadius = 2f;
    public int spawnTries = 8;
    public float groundOffset = 0.25f;               // sube a Mogo sobre el punto del NavMesh
    public float markerRadius = 0.9f;
    public GameObject markerPrefab;
    public Color markerColor = new Color(0.35f, 0.2f, 0.1f, 1f);

    [Header("Persecucion a saltos")]
    public float leapDistance = 4f;                  // distancia maxima de cada salto
    public float minLeapDistance = 1.2f;
    public float jumpWindupTime = 0.4f;              // agacharse antes de saltar (aviso para esquivar)
    public float jumpAirTime = 0.5f;                 // tiempo en el aire
    public float jumpTempo = 1f;                     // >1 = salta mas rapido y es mas dificil
    public float pauseBetweenActions = 0.2f;
    public float turnSpeed = 540f;
    public float landingDamage = 10f;                // dano si cae encima de Tuko (0 = sin dano)
    public float landingHitRadius = 1.6f;

    [Header("Golpe")]
    public float damage = 15f;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.0f;
    public float attackLockTime = 1.0f;              // se ajusta solo con el clip de golpe
    public float hitDelay = 0.5f;                    // momento del clip en que llega el puño
    public float hitRangeExtra = 0.5f;

    [Header("Version que se entierra (Burrow After Attack)")]
    public int attacksPerEmerge = 1;
    public float exposedTime = 2.0f;
    public float burrowTime = 0.5f;
    public float sinkDistance = 1.5f;

    [Header("Tiempos automaticos")]
    [Tooltip("Lee la duracion de los clips Emerging, Punch y Jump y ajusta los tiempos solo.")]
    public bool autoFromClips = true;

    [Header("Muerte")]
    public float deathWait = 0.6f;
    public float deathSinkTime = 0.8f;

    Health health, targetHealth;
    Collider[] cols;
    Renderer[] rends;
    Vector3 visualBaseLocalPos;
    GameObject marker;
    bool dead, warnedNoNav;
    bool hasEmerge, hasAttack, hasDie, hasJump;
    float emergeClipLen, punchClipLen, jumpClipLen;
    float nextAttack;

    static readonly int EmergeHash = Animator.StringToHash("Emerge");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int DieHash = Animator.StringToHash("Die");
    static readonly int JumpHash = Animator.StringToHash("Jump");

    void Awake()
    {
        health = GetComponent<Health>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.applyRootMotion = false;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == EmergeHash) hasEmerge = true;
                if (p.nameHash == AttackHash) hasAttack = true;
                if (p.nameHash == DieHash) hasDie = true;
                if (p.nameHash == JumpHash) hasJump = true;
            }
            if (!visual && animator.transform != transform) visual = animator.transform;
        }
        if (visual) visualBaseLocalPos = visual.localPosition;

        ReadClipLengths();

        cols = GetComponentsInChildren<Collider>(true);
        rends = GetComponentsInChildren<Renderer>(true);
        SetHidden(true);                              // empieza bajo tierra
    }

    void ReadClipLengths()
    {
        if (!autoFromClips || !animator || !animator.runtimeAnimatorController) return;
        foreach (var c in animator.runtimeAnimatorController.animationClips)
        {
            string n = c.name.ToLower();
            if (n.Contains("emerg")) emergeClipLen = c.length;
            else if (n.Contains("jump") || n.Contains("salt")) jumpClipLen = c.length;
            else if (n.Contains("punch") || n.Contains("attack")) punchClipLen = c.length;
        }
        if (punchClipLen > 0.1f)
        {
            attackLockTime = punchClipLen;
            hitDelay = Mathf.Min(hitDelay, attackLockTime * 0.9f);
        }
        if (emergeClipLen > 0.1f)
            emergeTime = Mathf.Min(emergeClipLen, maxEmergeTime);
    }

    void Start()
    {
        StartCoroutine(Cycle());
    }

    void Update()
    {
        if (dead) return;
        if (health && health.IsDead) Die();
    }

    // ---------------- CICLO PRINCIPAL ----------------
    IEnumerator Cycle()
    {
        yield return new WaitForSeconds(firstAppearDelay);

        while (!dead)
        {
            FindTarget();

            if (!TargetAlive() || FlatDistance() > detectRange)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // 1) Bajo tierra, esperando
            yield return new WaitForSeconds(Random.Range(hiddenTimeRange.x, hiddenTimeRange.y));
            if (dead) yield break;
            if (!TargetAlive()) continue;

            // 2) Elige donde salir y avisa en el suelo
            if (!PickSpawnPoint(out Vector3 p))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            transform.position = p + Vector3.up * groundOffset;
            marker = SpawnMarker(p + Vector3.up * groundOffset);
            float t = 0f;
            while (t < telegraphTime)
            {
                t += Time.deltaTime;
                if (marker)
                {
                    float k = 0.5f + 0.5f * Mathf.Clamp01(t / telegraphTime);
                    marker.transform.localScale = new Vector3(markerRadius * 2f * k, 0.02f, markerRadius * 2f * k);
                }
                yield return null;
            }
            if (marker) Destroy(marker);

            // 3) Sale de la tierra
            if (visual) visual.localPosition = visualBaseLocalPos;
            FaceTarget(true);
            SetHidden(false);
            if (animator && hasEmerge) animator.SetTrigger(EmergeHash);
            bool sped = animator && emergeClipLen > emergeTime + 0.05f;
            if (sped) animator.speed = emergeClipLen / emergeTime;
            yield return new WaitForSeconds(emergeTime);
            if (animator) animator.speed = 1f;

            // 4a) Se queda afuera y persigue a saltos
            if (!burrowAfterAttack)
            {
                yield return StartCoroutine(ChaseLoop());
                yield break;
            }

            // 4b) Version anterior: golpea, queda expuesto y se entierra
            for (int i = 0; i < attacksPerEmerge; i++)
            {
                if (!TargetAlive()) break;
                yield return StartCoroutine(Punch());
            }
            yield return new WaitForSeconds(exposedTime);
            yield return StartCoroutine(Burrow());
            SetHidden(true);
        }
    }

    // ---------------- PERSECUCION ----------------
    IEnumerator ChaseLoop()
    {
        while (!dead)
        {
            FindTarget();
            if (!TargetAlive())
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            float edge = EdgeDistance();
            if (edge <= attackRange)
            {
                if (Time.time >= nextAttack)
                {
                    yield return StartCoroutine(Punch());
                    nextAttack = Time.time + attackCooldown;
                }
                else
                {
                    FaceTarget(false);
                    yield return null;
                    continue;
                }
            }
            else
            {
                yield return StartCoroutine(Leap());
            }

            yield return new WaitForSeconds(pauseBetweenActions);
        }
    }

    IEnumerator Leap()
    {
        float tempo = Mathf.Max(0.1f, jumpTempo);
        float windup = jumpWindupTime / tempo;
        float air = jumpAirTime / tempo;
        float clip = jumpClipLen > 0.1f ? jumpClipLen : 1.33f;
        float recover = Mathf.Max(0.2f, clip / tempo - windup - air);

        if (animator && hasJump)
        {
            animator.speed = tempo;
            animator.SetTrigger(JumpHash);
        }

        // 1) Se agacha mirando a Tuko (esta es la senal para esquivar)
        float t = 0f;
        while (t < windup)
        {
            t += Time.deltaTime;
            FaceTarget(false);
            yield return null;
        }

        // 2) Calcula hacia donde salta (al despegar) y evita caerse al abismo
        Vector3 start = transform.position;
        Vector3 flat = target ? target.position - start : transform.forward;
        flat.y = 0;
        float dist = flat.magnitude;
        Vector3 dir = dist > 0.01f ? flat / dist : transform.forward;
        Vector3 from = start - Vector3.up * groundOffset;
        if (NavMesh.SamplePosition(from, out NavMeshHit fh, 1.5f, NavMesh.AllAreas)) from = fh.position;
        Vector3 goalPos = target ? target.position : from + dir * leapDistance;
        Vector3 land = PickLeapPoint(from, goalPos, leapDistance, minLeapDistance);
        Vector3 end = land + Vector3.up * groundOffset;

        // 3) En el aire
        t = 0f;
        while (t < air)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / air);
            Vector3 pos = Vector3.Lerp(start, end, k);
            transform.position = pos;
            FaceTarget(false);
            yield return null;
        }
        transform.position = end;

        // 4) Aterriza: dano si cae sobre Tuko
        if (landingDamage > 0f && TargetAlive() && EdgeDistance() <= landingHitRadius)
            targetHealth.TakeDamage(landingDamage);

        // 5) Se recupera
        yield return new WaitForSeconds(recover);
        if (animator) animator.speed = 1f;
    }

    // Elige donde aterrizar siguiendo la RUTA del NavMesh (rodea rocas y paredes en vez de chocar con ellas)
    Vector3 PickLeapPoint(Vector3 from, Vector3 goal, float maxDist, float minDist)
    {
        Vector3 g = goal;
        if (NavMesh.SamplePosition(goal, out NavMeshHit gh, 3f, NavMesh.AllAreas)) g = gh.position;

        NavMeshPath path = new NavMeshPath();
        if (!NavMesh.CalculatePath(from, g, NavMesh.AllAreas, path) || path.corners.Length < 2)
        {
            // sin ruta: salto recto, cortado en el borde del NavMesh
            Vector3 d = g - from; d.y = 0;
            Vector3 p = from + d.normalized * Mathf.Min(maxDist, d.magnitude);
            if (NavMesh.Raycast(from, p, out NavMeshHit eh, NavMesh.AllAreas)) p = eh.position;
            return p;
        }

        Vector3[] c = path.corners;
        float total = 0f;
        for (int i = 1; i < c.Length; i++) total += Vector3.Distance(c[i - 1], c[i]);

        // no se pasa de Tuko
        float limit = Mathf.Clamp(total - attackRange * 0.6f, minDist, maxDist);

        // el punto mas lejano de la ruta al que se puede llegar en linea recta sin salirse del NavMesh
        Vector3 best = from;
        bool found = false;
        for (float d = 0.5f; d <= limit + 0.001f; d += 0.5f)
        {
            Vector3 pt = PointAlong(c, d);
            if (!NavMesh.Raycast(from, pt, out NavMeshHit _, NavMesh.AllAreas))
            {
                best = pt;
                found = true;
            }
        }

        // si nada esta libre en linea recta, da un saltito corto por la ruta para no quedarse trabado
        if (!found) best = PointAlong(c, Mathf.Min(minDist * 0.6f, total));
        return best;
    }

    static Vector3 PointAlong(Vector3[] c, float dist)
    {
        float remaining = dist;
        for (int i = 1; i < c.Length; i++)
        {
            float len = Vector3.Distance(c[i - 1], c[i]);
            if (remaining <= len) return Vector3.Lerp(c[i - 1], c[i], len > 0.0001f ? remaining / len : 0f);
            remaining -= len;
        }
        return c[c.Length - 1];
    }

    IEnumerator Punch()
    {
        FaceTarget(true);
        if (animator && hasAttack) animator.SetTrigger(AttackHash);
        yield return new WaitForSeconds(hitDelay);
        DealDamage();
        yield return new WaitForSeconds(Mathf.Max(0f, attackLockTime - hitDelay));
    }

    IEnumerator Burrow()
    {
        SetColliders(false);                          // ya no se le puede golpear
        Transform t = visual ? visual : transform;
        Vector3 start = t.position;
        float e = 0f;
        while (e < burrowTime)
        {
            e += Time.deltaTime;
            t.position = start + Vector3.down * sinkDistance * Mathf.Clamp01(e / Mathf.Max(0.01f, burrowTime));
            yield return null;
        }
    }

    // ---------------- ATAQUE ----------------
    public void DealDamage()
    {
        if (dead || !target || targetHealth == null || targetHealth.IsDead) return;
        if (EdgeDistance() <= attackRange + hitRangeExtra)
            targetHealth.TakeDamage(damage);
    }

    // ---------------- MUERTE ----------------
    void Die()
    {
        dead = true;
        StopAllCoroutines();
        if (animator) animator.speed = 1f;
        if (marker) Destroy(marker);
        SetColliders(false);
        SetRenderers(true);
        if (animator && hasDie) animator.SetTrigger(DieHash);
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathWait);
        Transform t = visual ? visual : transform;
        Vector3 start = t.position;
        float e = 0f;
        while (e < deathSinkTime)
        {
            e += Time.deltaTime;
            t.position = start + Vector3.down * sinkDistance * Mathf.Clamp01(e / Mathf.Max(0.01f, deathSinkTime));
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---------------- AYUDAS ----------------
    void SetHidden(bool hidden)
    {
        SetColliders(!hidden);
        SetRenderers(!hidden);
    }

    void SetColliders(bool on)
    {
        foreach (var c in cols) if (c) c.enabled = on;
    }

    void SetRenderers(bool on)
    {
        foreach (var r in rends) if (r) r.enabled = on;
    }

    void FindTarget()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (target && !targetHealth) targetHealth = target.GetComponent<Health>();
    }

    bool TargetAlive()
    {
        return target && (targetHealth == null || !targetHealth.IsDead);
    }

    float FlatDistance()
    {
        Vector3 d = target.position - transform.position;
        d.y = 0;
        return d.magnitude;
    }

    float EdgeDistance()
    {
        Vector3 edge = targetHealth ? targetHealth.ClosestPoint(transform.position) : target.position;
        edge.y = transform.position.y;
        return (edge - transform.position).magnitude;
    }

    void FaceTarget(bool instant)
    {
        if (!target) return;
        Vector3 d = target.position - transform.position;
        d.y = 0;
        if (d.sqrMagnitude < 0.001f) return;
        Quaternion q = Quaternion.LookRotation(d.normalized, Vector3.up);
        transform.rotation = instant ? q : Quaternion.RotateTowards(transform.rotation, q, turnSpeed * Time.deltaTime);
    }

    // Busca un punto sobre el NavMesh alrededor de Tuko (asi nunca sale dentro del abismo ni de una pared)
    bool PickSpawnPoint(out Vector3 point)
    {
        for (int i = 0; i < spawnTries; i++)
        {
            float a = Random.value * Mathf.PI * 2f;
            Vector3 cand = target.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnDistance;
            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
            {
                Vector3 d = hit.position - target.position;
                d.y = 0;
                if (d.magnitude <= spawnDistance + navSampleRadius)
                {
                    point = hit.position;
                    return true;
                }
            }
        }
        if (!warnedNoNav)
        {
            warnedNoNav = true;
            Debug.LogWarning("[MogoController] No encontre NavMesh cerca de Tuko. Hornea el NavMesh de esta escena.", this);
        }
        point = Vector3.zero;
        return false;
    }

    GameObject SpawnMarker(Vector3 p)
    {
        if (markerPrefab) return Instantiate(markerPrefab, p, Quaternion.identity);

        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Collider c = m.GetComponent<Collider>();
        if (c) Destroy(c);
        m.transform.position = p + Vector3.up * 0.03f;
        m.transform.localScale = new Vector3(markerRadius, 0.02f, markerRadius);
        var r = m.GetComponent<Renderer>();
        r.material.color = markerColor;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return m;
    }

    void OnDestroy()
    {
        if (marker) Destroy(marker);
    }
}
