using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Menu: Plog > Crear pantalla de cartas (editable)
// Genera Assets/Resources/Cartas/PantallaCartas.prefab. Abrelo (doble clic) y mueve/escala/cambia lo que quieras.
// Los nombres de los objetos (Carta_0.., BotonElegir, Arte, Texto, MarcoSeleccion) NO los cambies: el codigo los busca.
public static class PlogCartasPrefabBuilder
{
    const string Ruta = "Assets/Resources/Cartas/PantallaCartas.prefab";

    static Texture2D T(string n) { return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Cartas/UI/" + n + ".png"); }

    static RectTransform Nodo(Transform p, string n, Vector2 pos, Vector2 dim)
    {
        var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
        var rt = (RectTransform)g.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = dim; return rt;
    }
    static RawImage Raw(Transform p, string n, Texture tex, Vector2 pos, Vector2 dim)
    {
        var rt = Nodo(p, n, pos, dim); var r = rt.gameObject.AddComponent<RawImage>(); r.texture = tex; r.raycastTarget = false; return r;
    }
    static TextMeshProUGUI Txt(Transform p, string n, string s, float size, Vector2 pos, Vector2 dim, Color col, Color borde)
    {
        var rt = Nodo(p, n, pos, dim); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.color = col; t.raycastTarget = false; t.outlineWidth = 0.22f; t.outlineColor = borde; return t;
    }

    [MenuItem("Plog/Crear pantalla de cartas (editable)")]
    public static void Crear()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Ruta) != null &&
            !EditorUtility.DisplayDialog("Pantalla de cartas", "Ya existe PantallaCartas.prefab. Si continuas se reemplaza (perderas tus cambios). Se guarda copia como PantallaCartas_ANTES.prefab.", "Reemplazar", "Cancelar"))
            return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Ruta) != null)
            AssetDatabase.CopyAsset(Ruta, "Assets/Resources/Cartas/PantallaCartas_ANTES.prefab");

        var raiz = new GameObject("PantallaCartas", typeof(RectTransform));
        var cv = raiz.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 300;
        var sc = raiz.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        raiz.AddComponent<GraphicRaycaster>();

        // fondo oscuro a pantalla completa
        var fg = new GameObject("Fondo", typeof(RectTransform), typeof(Image)); fg.transform.SetParent(raiz.transform, false);
        var fr = (RectTransform)fg.transform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        var fi = fg.GetComponent<Image>(); fi.color = new Color(0.05f, 0.02f, 0.12f, 0.92f); fi.raycastTarget = false;

        // marco de pantalla (estirado)
        var mp = Raw(raiz.transform, "MarcoPantalla", T("Marco_Pantalla"), Vector2.zero, Vector2.zero);
        var mpr = mp.rectTransform; mpr.anchorMin = Vector2.zero; mpr.anchorMax = Vector2.one; mpr.offsetMin = mpr.offsetMax = Vector2.zero;

        Raw(raiz.transform, "BannerTitulo", T("Banner_Titulo"), new Vector2(0, 392), new Vector2(900, 249));
        Txt(raiz.transform, "Titulo", "ELIGE UNA MEJORA", 64, new Vector2(0, 392), new Vector2(640, 100), new Color32(255, 246, 214, 255), new Color32(90, 40, 10, 255));
        Txt(raiz.transform, "Subtitulo", "Se aplicará en el siguiente nivel", 34, new Vector2(0, 262), new Vector2(1400, 50), Color.white, new Color32(30, 10, 50, 255));

        // 3 cartas (el codigo les pone la imagen real al abrir la pantalla; aqui van de ejemplo)
        string[] ej = { "Carta_Agilidad", "Carta_Fuerza", "Carta_Vida" };
        var selTex = T("Marco_Seleccion");
        for (int i = 0; i < 3; i++)
        {
            var rt = Nodo(raiz.transform, "Carta_" + i, new Vector2((i - 1) * 420f, -30f), new Vector2(330, 440));
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Cartas/" + ej[i] + ".png");
            var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = raw;
            // marco que aparece al seleccionar: mostrado aqui al 100% para que lo veas; el codigo lo oculta hasta elegir
            var m = Raw(rt, "MarcoSeleccion", selTex, new Vector2(0, 3), new Vector2(330 / 0.6833f, 440 / 0.7585f));
        }

        // boton ELEGIR
        var bt = Nodo(raiz.transform, "BotonElegir", new Vector2(0, -325), new Vector2(500, 103));
        var bi = bt.gameObject.AddComponent<Image>(); bi.color = new Color(1, 1, 1, 0);
        var bb = bt.gameObject.AddComponent<Button>(); bb.targetGraphic = bi;
        var arte = Raw(bt, "Arte", T("Boton_Elegir"), Vector2.zero, Vector2.zero);
        var ar = arte.rectTransform; ar.anchorMin = Vector2.zero; ar.anchorMax = Vector2.one; ar.offsetMin = ar.offsetMax = Vector2.zero;
        Txt(bt, "Texto", "ELEGIR", 52, Vector2.zero, new Vector2(400, 90), Color.white, new Color32(30, 10, 50, 255));

        System.IO.Directory.CreateDirectory("Assets/Resources/Cartas");
        PrefabUtility.SaveAsPrefabAsset(raiz, Ruta);
        Object.DestroyImmediate(raiz);
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Ruta);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("Plog: creado " + Ruta + ". Doble clic para editarlo.");
    }
}
