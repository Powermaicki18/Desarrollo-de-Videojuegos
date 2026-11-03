using UnityEngine;

// Zona de caída. Ponla en objetos con BoxCollider (Is Trigger) que cubran cada hueco.
[RequireComponent(typeof(Collider))]
public class PlogChasmZone : MonoBehaviour
{
    [Header("Jugador")]
    public float playerDamage = 15f;
    [Tooltip("Si no hay punto seguro guardado, reaparece aquí (opcional)")]
    public Transform respawnPoint;
    public Vector3 defaultRespawn = new Vector3(0f, 0.5f, -26f);

    [Header("Enemigos")]
    public float enemyDamage = 9999f;
    public float destroyDelay = 0.4f;

    [Header("Depuración")]
    public bool log = true;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void Start()
    {
        // instala el guardado de "último punto seguro" en el Player desde el inicio
        foreach (var h in Health.All)
            if (h != null && IsPlayer(h) && h.GetComponent<PlogSafeSpot>() == null)
                h.gameObject.AddComponent<PlogSafeSpot>();
    }

    static bool IsPlayer(Health h)
    {
        return h.CompareTag("Player") || h.GetComponent<CharacterController>() != null && h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null;
    }

    void OnTriggerEnter(Collider other) { Handle(other); }
    void OnTriggerStay(Collider other) { Handle(other); }

    void Handle(Collider other)
    {
        var h = other.GetComponentInParent<Health>();
        if (h == null || h.IsDead) return;

        if (IsPlayer(h))
        {
            if (log) Debug.Log("[Abismo] Cayó el jugador: " + h.name);
            var spot = h.GetComponent<PlogSafeSpot>();
            if (spot == null) spot = h.gameObject.AddComponent<PlogSafeSpot>();
            h.TakeDamage(playerDamage);
            Vector3 fallback = respawnPoint ? respawnPoint.position : defaultRespawn;
            spot.ReturnToSafe(fallback);
        }
        else
        {
            if (log) Debug.Log("[Abismo] Cayó enemigo: " + h.name);
            h.TakeDamage(enemyDamage);
            Destroy(h.gameObject, destroyDelay);
        }
    }
}

public class PlogSafeSpot : MonoBehaviour
{
    public float edgeMargin = 1.2f;
    public float minGroundY = -1f;
    Vector3 safe;
    bool hasSafe;
    float next;
    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cc == null) cc = GetComponentInChildren<CharacterController>();
        if (transform.position.y > minGroundY) { safe = transform.position; hasSafe = true; }
    }

    bool Ground(Vector3 p)
    {
        return Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore)
               && h.point.y > minGroundY;
    }

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 0.25f;
        if (cc != null && !cc.isGrounded) return;
        var p = transform.position;
        if (!Ground(p)) return;
        float m = edgeMargin;
        if (Ground(p + Vector3.right * m) && Ground(p + Vector3.left * m) &&
            Ground(p + Vector3.forward * m) && Ground(p + Vector3.back * m))
        { safe = p; hasSafe = true; }
    }

    public void ReturnToSafe(Vector3 fallback)
    {
        var target = (hasSafe ? safe : fallback) + Vector3.up * 0.1f;
        if (cc != null) cc.enabled = false;
        transform.position = target;
        Physics.SyncTransforms();
        if (cc != null) cc.enabled = true;
    }
}