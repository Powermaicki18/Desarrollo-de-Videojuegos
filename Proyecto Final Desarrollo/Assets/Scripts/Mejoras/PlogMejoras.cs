using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TipoMejora { Agilidad, Fuerza, Vida, Escudo }

// Mejoras (cartas) que el jugador elige entre niveles. Se guardan mientras dura la partida
// y se aplican solas al jugador en el siguiente mapa (no hay que poner nada en las escenas).
public static class PlogMejoras
{
    // ---- valores de las cartas (cambialos aqui) ----
    public const float VelocidadExtra = 3f;     // Agilidad: +3 a la velocidad de movimiento
    public const float DanoExtra = 3f;          // Fuerza: +3 al dano del golpe y del super
    public const float VidaExtra = 5f;          // Vida: +5 a la vida maxima
    public const float ReduccionDano = 0.20f;   // Escudo: recibe 20 % menos de dano
    public const int OpcionesPorEleccion = 3;   // cuantas cartas se ofrecen (se sortean entre las 4)

    public static readonly List<TipoMejora> Elegidas = new List<TipoMejora>();

    public static string Nombre(TipoMejora t) { return t.ToString().ToUpper(); }

    public static string Efecto(TipoMejora t)
    {
        switch (t)
        {
            case TipoMejora.Agilidad: return "+" + VelocidadExtra + " de Velocidad";
            case TipoMejora.Fuerza: return "+" + DanoExtra + " de Daño";
            case TipoMejora.Vida: return "+" + VidaExtra + " de Vida";
            default: return "-" + Mathf.RoundToInt(ReduccionDano * 100f) + "% de Daño recibido";
        }
    }

    public static Texture2D Carta(TipoMejora t) { return Resources.Load<Texture2D>("Cartas/Carta_" + t); }
    public static void Elegir(TipoMejora t) { Elegidas.Add(t); }
    public static void Reiniciar() { Elegidas.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // una partida nueva empieza sin mejoras
        if (escena.name == "MenuPrincipal" || escena.name == "CinematicaIntro") { Reiniciar(); return; }
        if (Object.FindFirstObjectByType<PlayerController>() == null) return;      // solo en mapas con jugador
        if (Object.FindFirstObjectByType<PlogMejorasEnJuego>() != null) return;
        new GameObject("PlogMejorasEnJuego").AddComponent<PlogMejorasEnJuego>();
    }
}

// Aplica las mejoras al jugador al empezar el mapa y las muestra en el HUD.
public class PlogMejorasEnJuego : MonoBehaviour
{
    void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null || PlogMejoras.Elegidas.Count == 0) { Destroy(gameObject); return; }

        var h = pc.GetComponentInChildren<Health>();
        if (h == null) h = pc.GetComponentInParent<Health>();

        foreach (var m in PlogMejoras.Elegidas)
        {
            switch (m)
            {
                case TipoMejora.Agilidad: pc.moveSpeed += PlogMejoras.VelocidadExtra; break;
                case TipoMejora.Fuerza: pc.attackDamage += PlogMejoras.DanoExtra; pc.superDamage += PlogMejoras.DanoExtra; break;
                case TipoMejora.Vida: if (h != null) { h.maxHealth += PlogMejoras.VidaExtra; h.Heal(PlogMejoras.VidaExtra); } break;
                case TipoMejora.Escudo: if (h != null) h.damageTakenMultiplier *= (1f - PlogMejoras.ReduccionDano); break;
            }
        }
        ConstruirHud();
    }

    void ConstruirHud()
    {
        var cgo = new GameObject("HUD_Mejoras"); cgo.transform.SetParent(transform, false);
        var cv = cgo.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 30;
        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        // sin GraphicRaycaster: no bloquea los toques del juego

        int n = PlogMejoras.Elegidas.Count;
        const float ancho = 108f, alto = 144f, sep = 76f;
        var texMini = Resources.Load<Texture2D>("Cartas/UI/Marco_Mini");
        for (int i = 0; i < n; i++)
        {
            var t = PlogMejoras.Elegidas[i];
            var g = new GameObject("Mejora_" + t, typeof(RectTransform));
            g.transform.SetParent(cgo.transform, false);
            var rt = g.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(ancho, alto);
            rt.anchoredPosition = new Vector2(48f, (n - 1) * (alto + sep) * 0.5f - i * (alto + sep));
            var raw = g.AddComponent<RawImage>(); raw.texture = PlogMejoras.Carta(t); raw.raycastTarget = false;
            if (texMini != null)
            {
                // el hueco del marco mide ~66% x 67%: el marco se dibuja mas grande que la carta, centrado
                var mg = new GameObject("Marco", typeof(RectTransform), typeof(RawImage));
                mg.transform.SetParent(g.transform, false);
                var mrt = (RectTransform)mg.transform; mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.sizeDelta = new Vector2(ancho / 0.6639f, alto / 0.6688f); mrt.anchoredPosition = Vector2.zero;
                var mri = mg.GetComponent<RawImage>(); mri.texture = texMini; mri.raycastTarget = false;
            }
        }

        // aviso al empezar el nivel
        var ag = new GameObject("AvisoMejora", typeof(RectTransform)); ag.transform.SetParent(cgo.transform, false);
        var art = ag.GetComponent<RectTransform>();
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.78f); art.sizeDelta = new Vector2(1500f, 120f); art.anchoredPosition = Vector2.zero;
        var txt = ag.AddComponent<TextMeshProUGUI>();
        var ult = PlogMejoras.Elegidas[n - 1];
        txt.text = "MEJORA ACTIVA: " + PlogMejoras.Nombre(ult) + "\n<size=60%>" + PlogMejoras.Efecto(ult) + "</size>";
        txt.fontSize = 62; txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white; txt.raycastTarget = false;
        txt.outlineWidth = 0.25f; txt.outlineColor = new Color32(30, 10, 50, 255);
        StartCoroutine(Desvanecer(txt));
    }

    IEnumerator Desvanecer(TextMeshProUGUI txt)
    {
        yield return new WaitForSecondsRealtime(3.5f);
        float t = 0f;
        while (t < 0.7f && txt != null) { t += Time.unscaledDeltaTime; txt.alpha = 1f - t / 0.7f; yield return null; }
        if (txt != null) txt.gameObject.SetActive(false);
    }
}
