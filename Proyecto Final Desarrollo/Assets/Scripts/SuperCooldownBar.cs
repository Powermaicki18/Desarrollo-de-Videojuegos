using UnityEngine;
using UnityEngine.UI;

// Barra de recarga del súper. Crea su propio Canvas al iniciar, no hay que montar nada.
public class SuperCooldownBar : MonoBehaviour
{
    public PlayerController player;                       // si queda vacío usa el tag Player

    [Header("Posición y tamaño")]
    public Vector2 size = new Vector2(340f, 28f);
    public Vector2 offsetFromBottomCenter = new Vector2(0f, 48f);

    [Header("Colores")]
    public Color chargingColor = new Color(1f, 0.65f, 0.1f);
    public Color readyColor = new Color(0.2f, 0.9f, 1f);
    public Color deniedColor = new Color(1f, 0.25f, 0.25f);
    public string label = "SÚPER";

    GameObject canvasGO;
    Image fill;
    Text text;
    Texture2D whiteTex;
    Sprite whiteSprite;

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.GetComponent<PlayerController>();
        }
        BuildUI();
    }

    void BuildUI()
    {
        canvasGO = new GameObject("SuperBarCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
        whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        // Fondo
        var back = NewImage("Back", canvasGO.transform, new Color(0f, 0f, 0f, 0.65f));
        var rt = back.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offsetFromBottomCenter;

        // Relleno
        fill = NewImage("Fill", back.transform, chargingColor);
        var frt = fill.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(3f, 3f);
        frt.offsetMax = new Vector2(-3f, -3f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;

        // Texto
        var tgo = new GameObject("Label");
        tgo.transform.SetParent(back.transform, false);
        text = tgo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = whiteSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    void Update()
    {
        if (!player || !fill) return;

        fill.fillAmount = player.SuperCharge01;
        bool ready = player.SuperReady;

        Color c = chargingColor;
        if (ready)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);   // parpadeo suave cuando está listo
            c = Color.Lerp(readyColor, Color.white, pulse * 0.4f);
        }
        if (Time.time - player.LastSuperDeniedTime < 0.25f)           // intento sin cargar
            c = deniedColor;
        fill.color = c;

        if (text)
            text.text = ready ? $"{label}  ¡LISTO!" : $"{label}  {player.SuperCooldownRemaining:0.0}s";
    }

    void OnDestroy()
    {
        if (canvasGO) Destroy(canvasGO);
        if (whiteSprite) Destroy(whiteSprite);
        if (whiteTex) Destroy(whiteTex);
    }
}