using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Crea los controles táctiles (joystick + botones) al iniciar. No hay que montar nada a mano.
public class MobileControls : MonoBehaviour
{
    public PlayerController player;       // si queda vacío usa el tag Player

    [Header("Visibilidad")]
    public bool showOnPC = true;          // déjalo marcado para probar con el mouse en el editor

    [Header("Joystick de movimiento")]
    public float joystickRadius = 120f;
    public Vector2 joystickRestOffset = new Vector2(280f, 260f);
    [Range(0.3f, 0.7f)] public float joystickAreaWidth = 0.5f;   // fracción izquierda de la pantalla

    [Header("Botón de golpe (mantener = repetir)")]
    public Vector2 attackPosition = new Vector2(-230f, 230f);     // desde la esquina inferior derecha
    public float attackSize = 220f;
    public Color attackColor = new Color(1f, 0.45f, 0.2f);

    [Header("Botón de súper")]
    public Vector2 superPosition = new Vector2(-490f, 330f);
    public float superSize = 170f;
    public Color superChargingColor = new Color(1f, 0.65f, 0.1f);
    public Color superReadyColor = new Color(0.2f, 0.9f, 1f);
    public Color superDeniedColor = new Color(1f, 0.25f, 0.25f);

    Canvas canvas;
    Sprite circle, ring;
    HoldButton attackBtn, superBtn;
    Image superBg, superRing, superOverlay;
    Text superText;
    readonly List<Object> created = new List<Object>();

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Start()
    {
        if (!showOnPC && !Application.isMobilePlatform) { enabled = false; return; }

        MobileInput.Move = Vector2.zero;
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.GetComponent<PlayerController>();
        }

        EnsureEventSystem();
        circle = MakeSprite(128, 0f);
        ring = MakeSprite(128, 6f);

        BuildCanvas();
        BuildJoystick();
        BuildAttackButton();
        BuildSuperButton();
    }

    void Update()
    {
        if (!player) return;

        // Mantener el botón de golpe = golpear sin parar (el controller respeta el tiempo entre golpes)
        if (attackBtn && attackBtn.IsHeld) player.Attack();

        if (!superBtn) return;
        bool ready = player.SuperReady;
        superOverlay.fillAmount = ready ? 0f : 1f - player.SuperCharge01;

        Color c = ready
            ? Color.Lerp(superReadyColor, Color.white, (0.5f + 0.5f * Mathf.Sin(Time.time * 6f)) * 0.4f)
            : superChargingColor;
        if (Time.time - player.LastSuperDeniedTime < 0.25f) c = superDeniedColor;

        superRing.color = c;
        superBg.color = new Color(c.r, c.g, c.b, 0.55f);
        superText.text = ready ? "SÚPER" : Mathf.CeilToInt(player.SuperCooldownRemaining).ToString();
        superText.fontSize = ready ? 32 : 60;
    }

    // ---------- Construcción de la interfaz ----------

    void BuildCanvas()
    {
        var go = new GameObject("MobileControlsCanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }

    void BuildJoystick()
    {
        // Zona táctil (transparente) en la izquierda de la pantalla
        var areaRT = MakeRect("JoystickArea", canvas.transform);
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = new Vector2(joystickAreaWidth, 0.8f);
        areaRT.offsetMin = areaRT.offsetMax = Vector2.zero;
        var areaImg = areaRT.gameObject.AddComponent<Image>();
        areaImg.color = new Color(0f, 0f, 0f, 0f);

        // Base
        var baseImg = MakeImage("Base", areaRT, circle, new Color(1f, 1f, 1f, 0.12f));
        var baseRT = baseImg.rectTransform;
        CenterAnchored(baseRT, Vector2.one * joystickRadius * 2f);
        var group = baseRT.gameObject.AddComponent<CanvasGroup>();
        var baseRing = MakeImage("Ring", baseRT, ring, new Color(1f, 1f, 1f, 0.55f));
        Stretch(baseRing.rectTransform, 0f);

        // Botón central
        var knobImg = MakeImage("Knob", baseRT, circle, new Color(1f, 1f, 1f, 0.6f));
        CenterAnchored(knobImg.rectTransform, Vector2.one * joystickRadius * 0.9f);

        var joy = areaRT.gameObject.AddComponent<VirtualJoystick>();
        joy.baseRect = baseRT;
        joy.knobRect = knobImg.rectTransform;
        joy.baseGroup = group;
        joy.radius = joystickRadius;
        joy.restOffsetFromBottomLeft = joystickRestOffset;
    }

    void BuildAttackButton()
    {
        attackBtn = BuildButton("AttackButton", attackPosition, attackSize, attackColor,
                                out _, out _, out Text label, out _);
        label.text = "GOLPE";
        label.fontSize = 36;
    }

    void BuildSuperButton()
    {
        superBtn = BuildButton("SuperButton", superPosition, superSize, superChargingColor,
                               out superBg, out superRing, out superText, out superOverlay);
        superBtn.onDown = () => { if (player) player.SuperAttack(); };
    }

    HoldButton BuildButton(string name, Vector2 pos, float size, Color color,
                           out Image bg, out Image ringImg, out Text label, out Image overlay)
    {
        var rt = MakeRect(name, canvas.transform);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);       // esquina inferior derecha
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.one * size;

        bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = circle;
        bg.color = new Color(color.r, color.g, color.b, 0.55f);

        ringImg = MakeImage("Ring", rt, ring, color);
        Stretch(ringImg.rectTransform, 0f);

        overlay = MakeImage("Cooldown", rt, circle, new Color(0f, 0f, 0f, 0.65f));
        Stretch(overlay.rectTransform, 0f);
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillAmount = 0f;

        label = MakeText("Label", rt);
        Stretch(label.rectTransform, 0f);

        var hb = rt.gameObject.AddComponent<HoldButton>();
        hb.visual = rt;
        return hb;
    }

    // ---------- Utilidades ----------

    RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var rt = MakeRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text MakeText(string name, Transform parent)
    {
        var rt = MakeRect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    static void CenterAnchored(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    // Círculo (o anillo) dibujado por código, sin necesidad de imágenes
    Sprite MakeSprite(int size, float ringThickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        float r = size * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d);
                if (ringThickness > 0f) a *= Mathf.Clamp01(d - (r - ringThickness));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        created.Add(tex);
        created.Add(sp);
        return sp;
    }

    // La interfaz necesita un EventSystem compatible con el Input System
    void EnsureEventSystem()
    {
        var es = FindFirstObjectByType<EventSystem>();
        if (!es) es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);
        if (!es.GetComponent<InputSystemUIInputModule>())
        {
            var module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
#else
        if (!es.GetComponent<StandaloneInputModule>()) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    void OnDestroy()
    {
        if (canvas) Destroy(canvas.gameObject);
        foreach (var o in created) if (o) Destroy(o);
        MobileInput.Move = Vector2.zero;
    }
}