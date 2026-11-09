
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

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

    [Header("Boton SKIP")]
    [Tooltip("Si lo dejas vacio, el boton se crea solo por codigo.")]
    [SerializeField] private Button botonSkip;
    [SerializeField] private string textoSkip = "SALTAR";
    [SerializeField] private Vector2 tamanoSkip = new Vector2(260f, 90f);
    [SerializeField] private Vector2 margenSkip = new Vector2(40f, 40f);

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

        CrearBotonSkip();

        // Pausar el juego
        Time.timeScale = 0f;

        // Iniciar animacion y temporizador
        StartCoroutine(AnimarEntrada());
        StartCoroutine(TemporizadorInstrucciones());
    }

    private void CrearBotonSkip()
    {
        if (botonSkip == null)
        {
            GameObject go = new GameObject("BotonSkip", typeof(RectTransform));
            go.transform.SetParent(panelInstrucciones.transform, false);

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

        botonSkip.onClick.RemoveListener(ComenzarJuego);
        botonSkip.onClick.AddListener(ComenzarJuego);
        botonSkip.transform.SetAsLastSibling();
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

        // Ocultar boton SALTAR y panel
        if (botonSkip != null) botonSkip.gameObject.SetActive(false);
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

