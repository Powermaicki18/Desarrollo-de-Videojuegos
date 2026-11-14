using System.Collections;
using UnityEngine;

// Totem que aparece al ganar el nivel. Flota y gira; cuando Tuko lo toca, el totem salta,
// gira rapido, vuela hacia Tuko y se absorbe con un destello. Despues avisa al VictoriaController.
public class TotemRecogible : MonoBehaviour
{
    public VictoriaController victoria;
    public Transform jugador;
    public float radioRecoger = 0.9f;
    public float velocidadGiro = 60f;
    public float alturaFlote = 0.15f;
    public float velocidadFlote = 2f;
    public float retardoRecoger = 1.0f;   // no se recoge de inmediato aunque Tuko este encima

    [Header("Animacion al recoger")]
    public float alturaSalto = 2.2f;
    public float tiempoSalto = 0.55f;
    public float tiempoAbsorcion = 0.6f;
    public float giroRapido = 900f;
    public float duracionDestello = 1.2f;       // cuanto dura la luz sobre Tuko
    public float pausaAntesDelPanel = 0.8f;     // espera extra despues del destello

    Vector3 basePos;
    float recogerDesde;
    bool recogido;

    void OnEnable() { recogerDesde = Time.time + retardoRecoger; }

    void Start()
    {
        basePos = transform.position;
        if (!jugador)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) jugador = p.transform;
        }
    }

    void Update()
    {
        if (recogido) return;

        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * velocidadFlote) * alturaFlote);

        if (!jugador || Time.time < recogerDesde) return;
        Vector3 d = jugador.position - basePos;
        d.y = 0f;
        if (d.magnitude <= radioRecoger)
        {
            recogido = true;
            StartCoroutine(AnimacionRecoger());
        }
    }

    IEnumerator AnimacionRecoger()
    {
        PlayerController control = jugador ? jugador.GetComponent<PlayerController>() : null;
        if (control) control.enabled = false;           // Tuko se queda quieto

        Vector3 inicio = transform.position;
        Vector3 escalaInicial = transform.localScale;

        // 1) Salta hacia arriba girando
        float e = 0f;
        while (e < tiempoSalto)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / tiempoSalto);
            float h = Mathf.Sin(k * Mathf.PI * 0.5f) * alturaSalto;   // sube y frena arriba
            transform.position = inicio + Vector3.up * h;
            transform.Rotate(0f, giroRapido * Time.deltaTime, 0f, Space.World);
            transform.localScale = escalaInicial * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        // 2) Vuela hacia Tuko encogiendose
        Vector3 desde = transform.position;
        e = 0f;
        while (e < tiempoAbsorcion)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / tiempoAbsorcion);
            k = k * k;                                                 // acelera al final
            Vector3 destino = jugador ? jugador.position + Vector3.up * 1.0f : desde;
            transform.position = Vector3.Lerp(desde, destino, k);
            transform.Rotate(0f, giroRapido * 1.3f * Time.deltaTime, 0f, Space.World);
            transform.localScale = escalaInicial * Mathf.Lerp(1.1f, 0f, k);
            yield return null;
        }

        // 3) Destello sobre Tuko
        if (jugador) yield return StartCoroutine(Destello(jugador.position + Vector3.up * 1.0f));
        yield return new WaitForSeconds(pausaAntesDelPanel);

        if (control) control.enabled = true;
        if (victoria) victoria.TotemRecogido();
        Destroy(gameObject);
    }

    IEnumerator Destello(Vector3 pos)
    {
        GameObject go = new GameObject("DestelloTotem");
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.9f, 0.5f);
        l.range = 8f;
        l.intensity = 0f;

        float dur = duracionDestello, e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float k = e / dur;
            l.intensity = Mathf.Sin(k * Mathf.PI) * 14f;
            yield return null;
        }
        Destroy(go);
    }
}
