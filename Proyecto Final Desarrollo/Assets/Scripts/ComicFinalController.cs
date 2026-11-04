
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ComicFinalController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelComicFinal;
    [SerializeField] private GameObject panelVictoria;

    [Header("Vinetas en orden")]
    [SerializeField] private GameObject[] vinetas;

    [Header("Tiempos del comic")]
    [SerializeField] private float tiempoEntreVinetas = 2f;
    [SerializeField] private float tiempoComicCompleto = 6f;

    [Header("Animacion POP")]
    [SerializeField] private float duracionPop = 0.35f;
    [SerializeField] private float escalaInicial = 0.65f;
    [SerializeField] private float escalaRebote = 1.08f;

    [Header("Final de la historia")]
    [SerializeField] private string escenaFinal = "MenuPrincipal";

    private bool comicIniciado = false;

    private void Awake()
    {
        // Ocultar el comic al entrar al nivel.
        if (panelComicFinal != null)
            panelComicFinal.SetActive(false);

        // Ocultar todas las vinetas.
        if (vinetas != null)
        {
            foreach (GameObject vineta in vinetas)
            {
                if (vineta != null)
                    vineta.SetActive(false);
            }
        }
    }

    // Esta funcion se conecta al boton CONTINUAR
    // de la pantalla de victoria.
    public void IniciarComicFinal()
    {
        if (comicIniciado)
            return;

        if (panelComicFinal == null)
        {
            Debug.LogError(
                "ComicFinalController: falta Panel Comic Final."
            );
            return;
        }

        comicIniciado = true;

        // Detener el juego mientras se cuenta el final.
        Time.timeScale = 0f;

        // Ocultar la pantalla de nivel completado.
        if (panelVictoria != null)
            panelVictoria.SetActive(false);

        // Mostrar el fondo del comic.
        panelComicFinal.SetActive(true);

        StartCoroutine(ReproducirComic());

        Debug.Log("Iniciando comic final de Plog Origins.");
    }

    private IEnumerator ReproducirComic()
    {
        foreach (GameObject vineta in vinetas)
        {
            if (vineta == null)
                continue;

            vineta.SetActive(true);

            RectTransform rect =
                vineta.GetComponent<RectTransform>();

            if (rect != null)
            {
                yield return AnimarPop(rect);
            }

            // Esperar antes de mostrar la siguiente.
            yield return new WaitForSecondsRealtime(
                tiempoEntreVinetas
            );
        }

        // Dejar visible la historia completa.
        yield return new WaitForSecondsRealtime(
            tiempoComicCompleto
        );

        TerminarHistoria();
    }

    private IEnumerator AnimarPop(RectTransform rect)
    {
        Vector3 escalaOriginal = rect.localScale;
        Vector3 pequena = escalaOriginal * escalaInicial;
        Vector3 grande = escalaOriginal * escalaRebote;

        yield return AnimarEscala(
            rect,
            pequena,
            grande,
            duracionPop * 0.7f
        );

        yield return AnimarEscala(
            rect,
            grande,
            escalaOriginal,
            duracionPop * 0.3f
        );
    }

    private IEnumerator AnimarEscala(
        RectTransform rect,
        Vector3 desde,
        Vector3 hasta,
        float duracion)
    {
        if (duracion <= 0f)
        {
            rect.localScale = hasta;
            yield break;
        }

        float tiempo = 0f;
        rect.localScale = desde;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            t = t * t * (3f - 2f * t);

            rect.localScale =
                Vector3.Lerp(desde, hasta, t);

            yield return null;
        }

        rect.localScale = hasta;
    }

    public void TerminarHistoria()
    {
        // Restaurar el tiempo.
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(escenaFinal))
        {
            Debug.LogError("Falta configurar escena final.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(escenaFinal))
        {
            Debug.LogError(
                "La escena no esta en Build Profiles: "
                + escenaFinal
            );
            return;
        }

        SceneManager.LoadScene(escenaFinal);
    }

    private void OnDestroy()
    {
        if (comicIniciado)
            Time.timeScale = 1f;
    }
}

