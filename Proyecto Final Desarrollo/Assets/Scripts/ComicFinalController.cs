
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ComicFinalController : MonoBehaviour
{
    [Header("Panel del comic")]
    [SerializeField] private GameObject panelComicFinal;

    [Header("Vinetas en orden")]
    [SerializeField] private GameObject[] vinetas;

    [Header("Tiempos")]
    [SerializeField] private float tiempoEntreVinetas = 2f;
    [SerializeField] private float tiempoComicCompleto = 6f;

    [Header("Animacion POP")]
    [SerializeField] private float duracionPop = 0.35f;
    [SerializeField] private float escalaInicial = 0.65f;
    [SerializeField] private float escalaRebote = 1.08f;

    [Header("Escena al terminar")]
    [SerializeField] private string escenaFinal = "MenuPrincipal";

    private void Awake()
    {
        // Garantizar que el tiempo este restaurado
        Time.timeScale = 1f;

        if (panelComicFinal != null)
            panelComicFinal.SetActive(true);

        foreach (GameObject vineta in vinetas)
        {
            if (vineta != null)
                vineta.SetActive(false);
        }
    }

    private void Start()
    {
        StartCoroutine(ReproducirComic());
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
                yield return AnimarPop(rect);

            yield return new WaitForSecondsRealtime(
                tiempoEntreVinetas
            );
        }

        yield return new WaitForSecondsRealtime(
            tiempoComicCompleto
        );

        TerminarHistoria();
    }

    private IEnumerator AnimarPop(RectTransform rect)
    {
        Vector3 original = rect.localScale;
        Vector3 pequena = original * escalaInicial;
        Vector3 grande = original * escalaRebote;

        yield return AnimarEscala(
            rect, pequena, grande,
            duracionPop * 0.7f
        );

        yield return AnimarEscala(
            rect, grande, original,
            duracionPop * 0.3f
        );
    }

    private IEnumerator AnimarEscala(
        RectTransform rect,
        Vector3 desde,
        Vector3 hasta,
        float duracion
    )
    {
        float tiempo = 0f;
        rect.localScale = desde;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.001f, duracion)
            );

            t = t * t * (3f - 2f * t);

            rect.localScale =
                Vector3.Lerp(desde, hasta, t);

            yield return null;
        }

        rect.localScale = hasta;
    }

    public void TerminarHistoria()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(escenaFinal))
        {
            SceneManager.LoadScene(escenaFinal);
        }
        else
        {
            Debug.LogError(
                "No se encuentra la escena: " + escenaFinal
            );
        }
    }
}
