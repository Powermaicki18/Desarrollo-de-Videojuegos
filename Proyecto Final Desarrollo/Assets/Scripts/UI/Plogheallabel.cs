using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ponlo en el mismo objeto que PlogHealZone (el del estanque).
// Muestra un cartel en pantalla (UI) que sigue al estanque: siempre legible, mismo tamaño y encima de todo.
public class PlogHealLabel : MonoBehaviour
{
    [Header("Textos")]
    public string title = "OASIS";
    public string idleText = "Cúrate aquí";
    public string healingText = "+ Curando...";
    public string cooldownText = "Recargando {0}s";

    [Header("Posición")]
    public float worldHeight = 1.5f;     // altura sobre el centro del collider
    public float screenOffsetY = 60f;    // píxeles extra hacia arriba
    public float hideBeyondDistance = 40f;

    [Header("Colores")]
    public Color idleColor = new Color(0.18f, 0.10f, 0.05f, 0.85f);
    public Color healingColor = new Color(0.10f, 0.45f, 0.20f, 0.92f);
    public Color cooldownColor = new Color(0.25f, 0.25f, 0.28f, 0.85f);
    public Color textColor = new Color(1f, 0.95f, 0.75f);

    RectTransform canvasRect, panel;
    Image bg;
    TextMeshProUGUI titleTmp, subTmp;
    CanvasGroup group;
    Collider zone;
    PlogHealZone healZone;
    Transform player;
    int inside;

    void Start()
    {
        zone = GetComponent<Collider>();
        healZone = GetComponent<PlogHealZone>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        Build();
        Refresh();
    }

    static Sprite MakeRounded()
    {
        const int s = 64, r = 24;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float cx = Mathf.Clamp(x, r, s - 1 - r), cy = Mathf.Clamp(y, r, s - 1 - r);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    void Build()
    {
        var cgo = new GameObject("HUD_Oasis");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = cgo.GetComponent<RectTransform>();

        var pgo = new GameObject("Panel", typeof(RectTransform));
        pgo.transform.SetParent(cgo.transform, false);
        panel = pgo.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(300, 100);
        bg = pgo.AddComponent<Image>();
        bg.sprite = MakeRounded();
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;
        group = pgo.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        titleTmp = MakeText("Titulo", pgo.transform, 38, FontStyles.Bold, new Vector2(0, 18));
        subTmp = MakeText("Sub", pgo.transform, 28, FontStyles.Normal, new Vector2(0, -20));
    }

    TextMeshProUGUI MakeText(string name, Transform parent, float size, FontStyles style, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(290, 44);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = textColor;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        return t;
    }

    void LateUpdate()
    {
        if (panel == null) return;
        Refresh();
        var cam = Camera.main;
        if (!cam) return;

        Vector3 world = (zone ? zone.bounds.center : transform.position) + Vector3.up * worldHeight;
        Vector3 sp = cam.WorldToScreenPoint(world);
        bool visible = sp.z > 0f && sp.x > -50 && sp.x < Screen.width + 50 && sp.y > -50 && sp.y < Screen.height + 50;
        if (visible && player && hideBeyondDistance > 0f)
            visible = Vector3.Distance(player.position, transform.position) < hideBeyondDistance;
        group.alpha = visible ? 1f : 0f;
        if (!visible) return;

        sp.y += screenOffsetY;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out Vector2 local);
        panel.anchoredPosition = local;

        // pequeño latido cuando está curando
        float s = inside > 0 ? 1f + Mathf.Sin(Time.time * 6f) * 0.04f : 1f;
        panel.localScale = Vector3.one * s;
    }

    void OnTriggerEnter(Collider other) { if (IsPlayer(other)) { inside++; Refresh(); } }
    void OnTriggerExit(Collider other) { if (IsPlayer(other)) { inside = Mathf.Max(0, inside - 1); Refresh(); } }

    static bool IsPlayer(Collider c)
    {
        var h = c.GetComponentInParent<Health>();
        return h != null && h.CompareTag("Player");
    }

    void Refresh()
    {
        if (bg == null) return;
        titleTmp.text = title;
        if (healZone != null && healZone.OnCooldown)
        {
            bg.color = cooldownColor;
            subTmp.text = string.Format(cooldownText, Mathf.CeilToInt(healZone.CooldownRemaining));
            return;
        }
        bool heal = inside > 0;
        bg.color = heal ? healingColor : idleColor;
        subTmp.text = heal ? healingText : idleText;
    }

    void OnDestroy() { if (panel) Destroy(panel.root.gameObject); }
}