
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InstruccionesController : MonoBehaviour, IPointerDownHandler
{
    [Header("Referencias")]
    [SerializeField] private GameObject panelInstrucciones;
    [SerializeField] private RectTransform imagenInstrucciones;

    [Header("Tiempo de instrucciones")]
    [SerializeField] private float tiempoEspera = 8f;

    [Header("Animacion POP")]
    [SerializeField] private float duracionAnimacion = 0.5f;
    [SerializeField] private float escalaInicial = 0.75f;
    [SerializeField] private float escalaRebote = 1.05f;

    private bool instruccionesTerminadas = false;
    private Vector3 escalaOriginal;

    private void Start()
    {
        // Guardar escala original de la imagen
        if (imagenInstrucciones != null)
        {
            escalaOriginal = imagenInstrucciones.localScale;
        }

        // Mostrar las instrucciones
        panelInstrucciones.SetActive(true);

        // Pausar el juego
        Time.timeScale = 0f;

        // Iniciar animacion y temporizador
        StartCoroutine(AnimarEntrada());
        StartCoroutine(TemporizadorInstrucciones());
    }

    private IEnumerator AnimarEntrada()
    {
        if (imagenInstrucciones == null)
            yield break;

        Vector3 pequena = escalaOriginal * escalaInicial;
        Vector3 grande = escalaOriginal * escalaRebote;

        imagenInstrucciones.localScale = pequena;

        // Efecto de crecimiento
        yield return AnimarEscala(
            pequena,
            grande,
            duracionAnimacion * 0.7f
        );

        // Regresar al tamano original
        yield return AnimarEscala(
            grande,
            escalaOriginal,
            duracionAnimacion * 0.3f
        );
    }

    private IEnumerator AnimarEscala(
        Vector3 desde,
        Vector3 hasta,
        float duracion)
    {
        if (duracion <= 0f)
        {
            imagenInstrucciones.localScale = hasta;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracion);

            // Suavizar movimiento
            progreso = progreso * progreso *
                       (3f - 2f * progreso);

            imagenInstrucciones.localScale =
                Vector3.Lerp(desde, hasta, progreso);

            yield return null;
        }

        imagenInstrucciones.localScale = hasta;
    }

    private IEnumerator TemporizadorInstrucciones()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, tiempoEspera)
        );

        ComenzarJuego();
    }

    // Detectar clic del mouse o tap en Android
    public void OnPointerDown(PointerEventData eventData)
    {
        ComenzarJuego();
    }

    public void ComenzarJuego()
    {
        if (instruccionesTerminadas)
            return;

        instruccionesTerminadas = true;

        // Detener animaciones y temporizador
        StopAllCoroutines();

        // Restaurar escala original
        if (imagenInstrucciones != null)
        {
            imagenInstrucciones.localScale = escalaOriginal;
        }

        // Ocultar panel
        panelInstrucciones.SetActive(false);

        // Reanudar juego
        Time.timeScale = 1f;

        Debug.Log("Instrucciones terminadas. Comienza Plog Origins.");
    }

    private void OnDestroy()
    {
        // Evitar que el juego quede pausado
        // si se destruye el controlador durante las instrucciones
        if (!instruccionesTerminadas)
        {
            Time.timeScale = 1f;
        }
    }
}

