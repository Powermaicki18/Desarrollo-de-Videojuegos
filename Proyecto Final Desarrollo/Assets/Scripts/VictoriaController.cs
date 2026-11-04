
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoriaController : MonoBehaviour
{
    [Header("Interfaz de victoria")]
    [SerializeField] private GameObject panelVictoria;
    [SerializeField] private RectTransform marcoVictoria;

    [Header("Animacion POP")]
    [SerializeField] private float duracionPop = 0.4f;
    [SerializeField] private float escalaInicial = 0.75f;
    [SerializeField] private float escalaRebote = 1.05f;

    [Header("Siguiente escena")]
    [SerializeField] private string siguienteEscena = "SegundoMapa";

    private bool victoriaActiva = false;
    private Vector3 escalaOriginal;

    private void Awake()
    {
        if (marcoVictoria != null)
            escalaOriginal = marcoVictoria.localScale;

        if (panelVictoria != null)
            panelVictoria.SetActive(false);
    }

    public void MostrarVictoria()
    {
        if (victoriaActiva)
            return;

        if (panelVictoria == null)
        {
            Debug.LogError("Falta asignar PanelVictoria.");
            return;
        }

        victoriaActiva = true;

        // Detener la partida
        Time.timeScale = 0f;

        // Mostrar interfaz
        panelVictoria.SetActive(true);

        // Animar el marco completo
        if (marcoVictoria != null)
            StartCoroutine(AnimarMarco());

        Debug.Log("VICTORIA - Nivel completado");
    }

    private IEnumerator AnimarMarco()
    {
        Vector3 pequena = escalaOriginal * escalaInicial;
        Vector3 grande = escalaOriginal * escalaRebote;

        yield return AnimarEscala(
            pequena,
            grande,
            duracionPop * 0.7f
        );

        yield return AnimarEscala(
            grande,
            escalaOriginal,
            duracionPop * 0.3f
        );
    }

    private IEnumerator AnimarEscala(
        Vector3 desde,
        Vector3 hasta,
        float duracion
    )
    {
        if (duracion <= 0f)
        {
            marcoVictoria.localScale = hasta;
            yield break;
        }

        float tiempo = 0f;
        marcoVictoria.localScale = desde;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);

            // Movimiento suave
            t = t * t * (3f - 2f * t);

            marcoVictoria.localScale =
                Vector3.LerpUnclamped(desde, hasta, t);

            yield return null;
        }

        marcoVictoria.localScale = hasta;
    }

    // Funcion del boton CONTINUAR
    public void Continuar()
    {
        if (!victoriaActiva)
            return;

        // Restaurar el tiempo antes de cambiar escena
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(siguienteEscena))
        {
            Debug.LogError(
                "No se ha configurado la siguiente escena."
            );
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(siguienteEscena))
        {
            victoriaActiva = false;
            SceneManager.LoadScene(siguienteEscena);
        }
        else
        {
            Debug.LogError(
                "La escena no esta incluida en Build Profiles: "
                + siguienteEscena
            );
        }
    }

    // Permite probar la victoria durante Play
    [ContextMenu("Probar Victoria")]
    private void ProbarVictoria()
    {
        if (Application.isPlaying)
            MostrarVictoria();
    }

    private void OnDestroy()
    {
        if (victoriaActiva)
            Time.timeScale = 1f;
    }
}

