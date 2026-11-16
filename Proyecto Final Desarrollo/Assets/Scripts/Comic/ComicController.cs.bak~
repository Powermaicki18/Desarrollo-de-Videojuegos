using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ComicController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelInicio;
    [SerializeField] private GameObject panelComic;

    [Header("Viñetas en orden")]
    [SerializeField] private GameObject[] vinetas;

    [Header("Botón Jugar")]
    [SerializeField] private RectTransform botonJugar;

    [Header("Tiempos del cómic")]
    [SerializeField] private float tiempoEntreVinetas = 2f;
    [SerializeField] private float tiempoComicCompleto = 5f;

    [Header("Animación POP de las viñetas")]
    [SerializeField] private float duracionPop = 0.35f;
    [SerializeField] private float escalaInicial = 0.65f;
    [SerializeField] private float escalaRebote = 1.08f;

    [Header("Animación del botón JUGAR")]
    [SerializeField] private float aumentoBoton = 1.12f;
    [SerializeField] private float duracionBoton = 0.12f;

    [Header("Cambio de escena")]
    [SerializeField] private string nombreSiguienteEscena = "PrimerMapa";

    [Header("Tap / Skip")]
    [Tooltip("Si lo dejas vacío, el botón SALTAR se crea solo por código.")]
    [SerializeField] private Button botonSkip;
    [SerializeField] private string textoSkip = "SALTAR";
    [SerializeField] private Vector2 tamanoSkip = new Vector2(260f, 90f);
    [SerializeField] private Vector2 margenSkip = new Vector2(40f, 40f);

    private bool cinematicaIniciada = false;
    private bool escenaCargando = false;
    private bool avanzar = false;
    private Vector3[] escalasOriginales;

    private void Start()
    {
        // Mostrar únicamente el menú principal
        panelInicio.SetActive(true);
        panelComic.SetActive(false);

        // Guardar la escala original de cada viñeta
        escalasOriginales = new Vector3[vinetas.Length];

        for (int i = 0; i < vinetas.Length; i++)
        {
            if (vinetas[i] == null)
                continue;

            escalasOriginales[i] = vinetas[i].transform.localScale;
            vinetas[i].SetActive(false);
        }

        CrearCapaDeTap();
        CrearBotonSkip();

        // El boton SALTAR solo existe mientras corre el comic
        if (botonSkip != null) botonSkip.gameObject.SetActive(false);
    }

    // ==========================================
    // TAP PARA AVANZAR + BOTÓN SKIP
    // ==========================================

    // Capa invisible a pantalla completa: cualquier tap avanza la viñeta
    private void CrearCapaDeTap()
    {
        GameObject capa = new GameObject("TapCatcher", typeof(RectTransform));
        capa.transform.SetParent(panelComic.transform, false);

        RectTransform rt = capa.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = capa.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        Button btn = capa.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.AddListener(AvanzarVineta);
    }

    private void CrearBotonSkip()
    {
        if (botonSkip == null)
        {
            GameObject go = new GameObject("BotonSkip", typeof(RectTransform));
            go.transform.SetParent(panelComic.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = tamanoSkip;
            rt.anchoredPosition = new Vector2(-margenSkip.x, margenSkip.y);

            Image fondo = go.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.65f);

            botonSkip = go.AddComponent<Button>();
            botonSkip.targetGraphic = fondo;

            GameObject txt = new GameObject("Texto", typeof(RectTransform));
            txt.transform.SetParent(go.transform, false);

            RectTransform trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = textoSkip + "  >>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 40f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        botonSkip.onClick.RemoveListener(SaltarComic);
        botonSkip.onClick.AddListener(SaltarComic);

        // Debe estar encima de la capa de tap
        botonSkip.transform.SetAsLastSibling();
    }

    public void AvanzarVineta()
    {
        avanzar = true;
    }

    public void SaltarComic()
    {
        if (escenaCargando)
            return;

        escenaCargando = true;
        if (botonSkip != null) botonSkip.gameObject.SetActive(false);
        StopAllCoroutines();
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreSiguienteEscena);
    }

    // Espera 'segundos' o hasta que el jugador toque la pantalla
    private IEnumerator EsperarOToque(float segundos)
    {
        float t = 0f;

        while (t < segundos && !avanzar)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        avanzar = false;
    }

    // Esta función se conecta al On Click del botón JUGAR
    public void IniciarComic()
    {
        if (cinematicaIniciada)
            return;

        cinematicaIniciada = true;

        StartCoroutine(SecuenciaCompleta());
    }

    private IEnumerator SecuenciaCompleta()
    {
        // ======================================
        // 1. ANIMACIÓN DEL BOTÓN JUGAR
        // ======================================

        if (botonJugar != null)
        {
            Vector3 escalaOriginal = botonJugar.localScale;
            Vector3 escalaGrande = escalaOriginal * aumentoBoton;

            yield return StartCoroutine(
                AnimarEscala(botonJugar, escalaOriginal, escalaGrande, duracionBoton));

            yield return StartCoroutine(
                AnimarEscala(botonJugar, escalaGrande, escalaOriginal, duracionBoton));
        }

        // ======================================
        // 2. ACTIVAR EL CÓMIC
        // ======================================

        panelInicio.SetActive(false);
        panelComic.SetActive(true);
        avanzar = false;
        if (botonSkip != null) botonSkip.gameObject.SetActive(true);

        // Asegurar que todas empiecen ocultas
        foreach (GameObject vineta in vinetas)
        {
            if (vineta != null)
                vineta.SetActive(false);
        }

        // La capa de tap y el botón skip siempre por encima de las viñetas
        // (el skip queda al final, encima de la capa)
        Transform capa = panelComic.transform.Find("TapCatcher");
        if (capa != null)
            capa.SetAsLastSibling();
        if (botonSkip != null)
            botonSkip.transform.SetAsLastSibling();

        // ======================================
        // 3. MOSTRAR LAS VIÑETAS UNA POR UNA
        // ======================================

        for (int i = 0; i < vinetas.Length; i++)
        {
            if (vinetas[i] == null)
                continue;

            GameObject vinetaActual = vinetas[i];
            Transform transformVineta = vinetaActual.transform;

            Vector3 escalaFinal = escalasOriginales[i];

            // Empezar pequeña
            transformVineta.localScale = escalaFinal * escalaInicial;

            // Mostrar viñeta
            vinetaActual.SetActive(true);

            // Las viñetas nuevas se crean/activan debajo de la capa de tap
            if (capa != null)
                capa.SetAsLastSibling();
            if (botonSkip != null)
                botonSkip.transform.SetAsLastSibling();

            // Crecer ligeramente más que su tamaño final
            yield return StartCoroutine(
                AnimarEscala(transformVineta,
                    escalaFinal * escalaInicial,
                    escalaFinal * escalaRebote,
                    duracionPop * 0.7f));

            // Regresar suavemente al tamaño original
            yield return StartCoroutine(
                AnimarEscala(transformVineta,
                    escalaFinal * escalaRebote,
                    escalaFinal,
                    duracionPop * 0.3f));

            // Esperar hasta la siguiente viñeta (o hasta un tap)
            if (i < vinetas.Length - 1)
            {
                float espera = Mathf.Max(0f, tiempoEntreVinetas - duracionPop);
                yield return StartCoroutine(EsperarOToque(espera));
            }
        }

        // ======================================
        // 4. MOSTRAR EL CÓMIC COMPLETO (tap = continuar)
        // ======================================

        yield return StartCoroutine(EsperarOToque(tiempoComicCompleto));

        // ======================================
        // 5. CARGAR EL PRIMER MAPA
        // ======================================

        SaltarComic();
    }

    // ==========================================
    // ANIMACIÓN SUAVE DE ESCALA
    // ==========================================

    private IEnumerator AnimarEscala(
        Transform objetivo,
        Vector3 escalaDesde,
        Vector3 escalaHasta,
        float duracion)
    {
        if (duracion <= 0f)
        {
            objetivo.localScale = escalaHasta;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracion);

            // Suavizado de la animación
            progreso = progreso * progreso * (3f - 2f * progreso);

            objetivo.localScale = Vector3.Lerp(escalaDesde, escalaHasta, progreso);

            yield return null;
        }

        // Garantizar la escala final exacta
        objetivo.localScale = escalaHasta;
    }
}
