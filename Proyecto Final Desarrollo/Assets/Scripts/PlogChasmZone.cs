using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Zona de caída. Ponla en objetos con BoxCollider (Is Trigger) que cubran cada hueco.
[RequireComponent(typeof(Collider))]
public class PlogChasmZone : MonoBehaviour
{
    [Header("Jugador")]
    public float playerDamage = 15f;
    [Tooltip("Si no hay punto seguro guardado, reaparece aquí (opcional)")]
    public Transform respawnPoint;
    public Vector3 defaultRespawn = new Vector3(0f, 0.5f, -26f);

    [Header("Efecto de daño y caída del jugador")]
    [Tooltip("Color del destello/viñeta al caer")]
    public Color colorDestello = new Color(0.9f, 0f, 0f, 0.55f);
    [Tooltip("Color de la pantalla mientras Tuko está 'fuera' antes de volver (oscuro rojizo)")]
    public Color colorPantallaCaida = new Color(0.25f, 0f, 0f, 0.85f);
    [Tooltip("Tiempo que Tuko tarda en hundirse/encogerse al caer")]
    public float tiempoCaida = 0.7f;
    [Tooltip("Cuánto baja Tuko mientras cae")]
    public float profundidadCaida = 4f;
    [Tooltip("Espera con la pantalla oscura antes de devolverlo al punto seguro")]
    public float esperaAntesDeVolver = 0.8f;
    [Tooltip("Tiempo en que la pantalla vuelve a verse normal")]
    public float tiempoRecuperarPantalla = 0.6f;
    [Tooltip("Invulnerabilidad al reaparecer (también parpadea en rojo)")]
    public float invulnerabilidadAlVolver = 1.5f;

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
            if (spot.IsFalling) return;   // ya está cayendo: no repetir daño
            h.TakeDamage(playerDamage);
            Vector3 fallback = respawnPoint ? respawnPoint.position : defaultRespawn;
            if (h.IsDead) return;         // si murió, que siga el flujo de Game Over
            spot.StartFall(this, fallback);
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

    public bool IsFalling { get; private set; }

    public void StartFall(PlogChasmZone z, Vector3 fallback)
    {
        if (IsFalling) return;
        StartCoroutine(FallRoutine(z, fallback));
    }

    IEnumerator FallRoutine(PlogChasmZone z, Vector3 fallback)
    {
        IsFalling = true;
        var pc = GetComponent<PlayerController>();
        var health = GetComponent<Health>();
        if (pc != null) pc.enabled = false;
        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetFloat("Speed", 0f);

        // pantalla roja
        var img = CrearOverlay();
        var rends = GetComponentsInChildren<Renderer>();
        var mpb = new MaterialPropertyBlock();

        // destello rojo rápido
        float t = 0f;
        while (t < 0.1f) { t += Time.deltaTime; SetAlpha(img, z.colorDestello, t / 0.1f); yield return null; }

        // caída: se hunde y se encoge, tinte rojo
        Vector3 p0 = transform.position;
        Vector3 s0 = transform.localScale;
        bool detectPrev = cc != null && cc.detectCollisions;
        if (cc != null) cc.detectCollisions = false;   // no se desactiva el controller (evita errores de Move)
        t = 0f;
        float dur = Mathf.Max(0.05f, z.tiempoCaida);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = k * k;
            transform.position = p0 + Vector3.down * z.profundidadCaida * e;
            transform.localScale = Vector3.Lerp(s0, s0 * 0.25f, e);
            transform.Rotate(0f, 540f * Time.deltaTime, 0f);
            img.color = Color.Lerp(z.colorDestello, z.colorPantallaCaida, k);
            Tint(rends, mpb, Color.Lerp(Color.white, Color.red, Mathf.PingPong(t * 8f, 1f)));
            yield return null;
        }
        img.color = z.colorPantallaCaida;

        // espera "fuera"
        yield return new WaitForSeconds(Mathf.Max(0f, z.esperaAntesDeVolver));

        // vuelve al punto seguro
        transform.localScale = s0;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        ReturnToSafe(fallback);
        if (cc != null) cc.detectCollisions = detectPrev;
        Tint(rends, mpb, Color.white, true);
        if (health != null && z.invulnerabilidadAlVolver > 0f) health.GrantInvulnerability(z.invulnerabilidadAlVolver);
        if (pc != null) pc.enabled = true;

        // la pantalla se aclara; Tuko parpadea en rojo mientras es invulnerable
        t = 0f;
        float fade = Mathf.Max(0.05f, z.tiempoRecuperarPantalla);
        float blink = Mathf.Max(fade, z.invulnerabilidadAlVolver);
        float tb = 0f;
        while (tb < blink)
        {
            float dt = Time.deltaTime;
            t += dt; tb += dt;
            if (t < fade) { var c = z.colorPantallaCaida; c.a *= 1f - t / fade; img.color = c; }
            else if (img != null) img.color = Color.clear;
            bool on = tb < z.invulnerabilidadAlVolver && Mathf.FloorToInt(tb * 10f) % 2 == 0;
            Tint(rends, mpb, on ? new Color(1f, 0.45f, 0.45f) : Color.white, !on);
            yield return null;
        }
        Tint(rends, mpb, Color.white, true);
        if (img != null) Destroy(img.canvas.gameObject);
        IsFalling = false;
    }

    static void SetAlpha(Image img, Color c, float k)
    {
        c.a *= Mathf.Clamp01(k);
        img.color = c;
    }

    static void Tint(Renderer[] rends, MaterialPropertyBlock mpb, Color c, bool clear = false)
    {
        foreach (var r in rends)
        {
            if (r == null || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (clear) { r.SetPropertyBlock(null); continue; }
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }
    }

    static Image CrearOverlay()
    {
        var go = new GameObject("FX_CaidaAbismo");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 900;
        var imgGo = new GameObject("Rojo");
        imgGo.transform.SetParent(go.transform, false);
        var img = imgGo.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return img;
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