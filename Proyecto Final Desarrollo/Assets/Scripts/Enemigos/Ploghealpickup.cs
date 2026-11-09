using UnityEngine;

// Cristal de vida. Se pega al suelo al aparecer (aunque el enemigo muriera en el aire) y se recoge
// por distancia al jugador, sin depender del collider. Si estás con la vida llena, no se gasta y
// puedes tomarlo después. Cuando te falta vida y estás cerca, el cristal se acerca solo.
public class PlogHealPickup : MonoBehaviour
{
    [Header("Curación")]
    public float healAmount = 20f;
    public float lifetime = 12f;       // 0 = no desaparece

    [Header("Recogida")]
    public float pickupRadius = 1.6f;      // distancia horizontal para recogerlo
    public float maxHeightDiff = 3f;       // diferencia de altura máxima con el jugador
    public float magnetRadius = 3.5f;      // dentro de este radio se acerca solo (0 = sin imán)
    public float magnetSpeed = 10f;

    [Header("Posición")]
    public bool snapToGround = true;
    public float groundOffset = 0.6f;      // altura sobre el suelo
    [Tooltip("Si el suelo bajo el cristal está más abajo que el jugador por esta distancia (abismo), el cristal desaparece")]
    public float destroyIfBelowPlayer = 2.5f;
    public bool debugLog = false;
    public float bobHeight = 0.2f;
    public float spinSpeed = 90f;

    Vector3 basePos;
    Transform player;
    Health playerHealth;

    void Start()
    {
        if (lifetime > 0f) Destroy(gameObject, lifetime);
        FindPlayer();
        if (snapToGround && !PlaceOnGround()) { Destroy(gameObject); return; }
        basePos = transform.position;
    }

    bool PlaceOnGround()
    {
        Vector3 o = transform.position + Vector3.up * 0.5f;
        var hits = Physics.RaycastAll(o, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<Health>() != null) continue;   // ni enemigos ni jugador
            if (h.normal.y < 0.3f) continue;                                   // paredes laterales
            if (player && h.point.y < player.position.y - destroyIfBelowPlayer)   // abismo: inalcanzable
            {
                if (debugLog) Debug.Log("[Cristal] Desaparece: el suelo está muy por debajo del jugador (abismo)");
                return false;
            }
            transform.position = new Vector3(transform.position.x, h.point.y + groundOffset, transform.position.z);
            if (debugLog) Debug.Log("[Cristal] Colocado sobre " + h.collider.name + " en y=" + h.point.y.ToString("0.00"));
            return true;
        }
        return true;   // sin suelo debajo: se queda donde está
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (!p) return;
        player = p.transform;
        playerHealth = p.GetComponent<Health>();
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (!player) FindPlayer();
        if (player && playerHealth && !playerHealth.IsDead && playerHealth.CurrentHealth < playerHealth.maxHealth)
        {
            Vector3 d = player.position - basePos; d.y = 0f;
            float dist = d.magnitude;
            float dy = Mathf.Abs(player.position.y - (basePos.y - groundOffset));

            if (dist <= pickupRadius && dy <= maxHeightDiff)
            {
                playerHealth.Heal(healAmount);
                Destroy(gameObject);
                return;
            }
            if (magnetRadius > 0f && dist <= magnetRadius && dy <= maxHeightDiff)
            {
                Vector3 step = d.normalized * magnetSpeed * Time.deltaTime;
                basePos += step;
            }
        }

        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
    }
}
