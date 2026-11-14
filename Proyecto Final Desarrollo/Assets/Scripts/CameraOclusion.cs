using System.Collections.Generic;
using UnityEngine;

// Hace transparentes los objetos altos del escenario que quedan entre la camara y Tuko (rocas, arboles, pilares...),
// para que siempre se vea bien. Se agrega solo a la camara que tiene CameraFollow.
// No necesita colliders: calcula con las cajas (bounds) de los objetos del mapa.
public class CameraOclusion : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform jugador;

    [Header("Solo estos grupos del escenario")]
    [Tooltip("Solo se vuelven transparentes los objetos que esten dentro de un grupo (objeto padre) cuyo nombre contenga alguna de estas palabras. Si la lista queda vacia, se usan los filtros de tamano.")]
    public string[] soloGruposConNombre = { "Perimetro", "Paredes" };

    [Header("Filtros de tamano (solo si la lista de grupos esta vacia)")]
    [Tooltip("Distancia a la linea camara-Tuko (en metros) a la que un objeto empieza a taparte.")]
    public float radioDeVision = 1.8f;
    [Tooltip("Solo objetos con al menos esta altura (en metros). Asi no se tocan el piso, pasto ni flores.")]
    public float alturaMinima = 3.5f;
    [Tooltip("El objeto tambien debe medir al menos esto en su lado mas grande (ancho o largo). Asi se ignoran piedras, cactus y arbustos pequenos.")]
    public float tamanoMinimo = 3f;
    [Tooltip("Objetos mas anchos que esto se consideran piso o terreno y no se tocan.")]
    public float anchoMaximo = 25f;
    [Range(0f, 1f)] public float transparenciaMinima = 0.22f;
    public float velocidadFade = 9f;
    [Tooltip("No toca objetos a menos de esta distancia detras de Tuko.")]
    public float margenDetrasDeTuko = 0.8f;
    public float intervaloRevision = 0.06f;

    class Item
    {
        public Renderer r;
        public Material[] originales;
        public Material[] fade;
        public float alpha = 1f, objetivo = 1f;
        public bool enFade;
    }

    readonly List<Item> items = new List<Item>();
    Material baseTransparente;
    float proximaRevision;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int Color_ = Shader.PropertyToID("_Color");
    static readonly int MainTex = Shader.PropertyToID("_MainTex");

    void Start()
    {
        baseTransparente = Resources.Load<Material>("FadeTransparente");
        if (!jugador)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) jugador = p.transform;
        }

        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r.enabled || r.GetComponentInParent<Health>() != null) continue;
            if (r.gameObject.CompareTag("Player")) continue;
            if (soloGruposConNombre != null && soloGruposConNombre.Length > 0)
            {
                if (!EnGrupo(r.transform)) continue;
                items.Add(new Item { r = r });
                continue;
            }
            Vector3 s = r.bounds.size;
            if (s.y < alturaMinima) continue;
            if (Mathf.Max(s.x, s.z) < tamanoMinimo) continue;
            if (Mathf.Max(s.x, s.z) > anchoMaximo) continue;
            items.Add(new Item { r = r });
        }
    }

    bool EnGrupo(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            foreach (var palabra in soloGruposConNombre)
                if (!string.IsNullOrEmpty(palabra) && n.Contains(palabra.ToLowerInvariant()))
                    return true;
        }
        return false;
    }

    void LateUpdate()
    {
        if (!jugador) return;
        Camera cam = Camera.main;
        if (!cam) return;

        if (Time.time >= proximaRevision)
        {
            proximaRevision = Time.time + intervaloRevision;
            Vector3 a = cam.transform.position;
            Vector3 b = jugador.position + Vector3.up * 0.8f;
            Vector3 dir = b - a;
            float largo = dir.magnitude;
            dir /= Mathf.Max(0.001f, largo);
            var ray = new Ray(a, dir);

            foreach (var it in items)
            {
                if (it.r == null) continue;
                Bounds bd = it.r.bounds;
                bd.Expand(radioDeVision * 2f);
                bool tapa = bd.IntersectRay(ray, out float dist) && dist < largo - margenDetrasDeTuko;
                // si el centro de la camara ya esta dentro de la caja, tambien tapa
                if (!tapa && bd.Contains(a)) tapa = true;
                it.objetivo = tapa ? transparenciaMinima : 1f;
            }
        }

        float paso = velocidadFade * Time.deltaTime;
        foreach (var it in items)
        {
            if (it.r == null) continue;
            if (Mathf.Approximately(it.alpha, it.objetivo) && it.objetivo >= 1f) continue;
            it.alpha = Mathf.MoveTowards(it.alpha, it.objetivo, paso);

            if (it.alpha < 0.999f)
            {
                if (!it.enFade) Entrar(it);
                if (it.fade != null)
                    foreach (var m in it.fade)
                        if (m) { Color c = m.GetColor(BaseColor); c.a = it.alpha; m.SetColor(BaseColor, c); }
            }
            else if (it.enFade)
            {
                Salir(it);
            }
        }
    }

    void Entrar(Item it)
    {
        it.originales = it.r.sharedMaterials;
        it.fade = new Material[it.originales.Length];
        for (int i = 0; i < it.originales.Length; i++)
        {
            Material o = it.originales[i];
            Material f;
            if (baseTransparente != null)
            {
                f = new Material(baseTransparente);
                if (o != null)
                {
                    if (o.HasProperty(BaseMap)) f.SetTexture(BaseMap, o.GetTexture(BaseMap));
                    else if (o.HasProperty(MainTex)) f.SetTexture(BaseMap, o.GetTexture(MainTex));
                    if (o.HasProperty(BaseColor)) f.SetColor(BaseColor, o.GetColor(BaseColor));
                    else if (o.HasProperty(Color_)) f.SetColor(BaseColor, o.GetColor(Color_));
                    f.SetTextureScale(BaseMap, o.HasProperty(BaseMap) ? o.GetTextureScale(BaseMap) : Vector2.one);
                    f.SetTextureOffset(BaseMap, o.HasProperty(BaseMap) ? o.GetTextureOffset(BaseMap) : Vector2.zero);
                }
            }
            else
            {
                f = new Material(o);                      // plan B: convierte el material en transparente
                f.SetFloat("_Surface", 1f); f.SetFloat("_Blend", 0f);
                f.SetFloat("_SrcBlend", 5f); f.SetFloat("_DstBlend", 10f); f.SetFloat("_ZWrite", 0f);
                f.SetOverrideTag("RenderType", "Transparent");
                f.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                f.DisableKeyword("_ALPHATEST_ON");
                f.renderQueue = 3000;
            }
            it.fade[i] = f;
        }
        it.r.sharedMaterials = it.fade;
        it.enFade = true;
    }

    void Salir(Item it)
    {
        if (it.r) it.r.sharedMaterials = it.originales;
        if (it.fade != null) foreach (var m in it.fade) if (m) Destroy(m);
        it.fade = null;
        it.enFade = false;
        it.alpha = 1f;
    }

    void OnDisable()
    {
        foreach (var it in items) if (it.enFade) Salir(it);
    }
}
