
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private bool cinematicaIniciada = false;
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

            escalasOriginales[i] =
                vinetas[i].transform.localScale;

            vinetas[i].SetActive(false);
        }
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
                AnimarEscala(
                    botonJugar,
                    escalaOriginal,
                    escalaGrande,
                    duracionBoton
                )
            );

            yield return StartCoroutine(
                AnimarEscala(
                    botonJugar,
                    escalaGrande,
                    escalaOriginal,
                    duracionBoton
                )
            );
        }

        // ======================================
        // 2. ACTIVAR EL CÓMIC
        // ======================================

        panelInicio.SetActive(false);
        panelComic.SetActive(true);

        // Asegurar que todas empiecen ocultas
        foreach (GameObject vineta in vinetas)
        {
            if (vineta != null)
                vineta.SetActive(false);
        }

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
            transformVineta.localScale =
                escalaFinal * escalaInicial;

            // Mostrar viñeta
            vinetaActual.SetActive(true);

            // Crecer ligeramente más que su tamaño final
            yield return StartCoroutine(
                AnimarEscala(
                    transformVineta,
                    escalaFinal * escalaInicial,
                    escalaFinal * escalaRebote,
                    duracionPop * 0.7f
                )
            );

            // Regresar suavemente al tamaño original
            yield return StartCoroutine(
                AnimarEscala(
                    transformVineta,
                    escalaFinal * escalaRebote,
                    escalaFinal,
                    duracionPop * 0.3f
                )
            );

            // Esperar hasta la siguiente viñeta
            if (i < vinetas.Length - 1)
            {
                float espera = Mathf.Max(
                    0f,
                    tiempoEntreVinetas - duracionPop
                );

                yield return new WaitForSecondsRealtime(espera);
            }
        }

        // ======================================
        // 4. MOSTRAR EL CÓMIC COMPLETO
        // ======================================

        yield return new WaitForSecondsRealtime(
            tiempoComicCompleto
        );

        // ======================================
        // 5. CARGAR EL PRIMER MAPA
        // ======================================

        SceneManager.LoadScene(nombreSiguienteEscena);
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

            float progreso = Mathf.Clamp01(
                tiempo / duracion
            );

            // Suavizado de la animación
            progreso = progreso * progreso *
                       (3f - 2f * progreso);

            objetivo.localScale = Vector3.Lerp(
                escalaDesde,
                escalaHasta,
                progreso
            );

            yield return null;
        }

        // Garantizar la escala final exacta
        objetivo.localScale = escalaHasta;
    }
}

