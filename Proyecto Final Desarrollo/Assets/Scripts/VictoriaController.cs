
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

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

    [Header("Totem de victoria")]
    [SerializeField] private GameObject totemPrefab;
    [SerializeField] private Material totemMaterial;
    [SerializeField] private Transform puntoTotem;
    [SerializeField] private float escalaTotem = 1.5f;
    [SerializeField] private float alturaSobrePunto = 0.1f;
    [SerializeField] private float distanciaAlJugador = 4f;
    [SerializeField] private float radioTocarTotem = 0.9f;   // distancia al centro del totem para recogerlo
    [Header("Texto del aviso")]
    [SerializeField] private string mensajeTotem = "¡Recoge el tótem para continuar!";
    [SerializeField] private TMPro.TMP_Text textoAvisoPropio;

    [Header("Estilo del aviso")]
    [Tooltip("TMP Font Asset")]
    [SerializeField] private TMPro.TMP_FontAsset fuenteAviso;
    [SerializeField] private float tamanoAviso = 48f;
    [SerializeField] private bool negritaAviso = true;
    [SerializeField] private bool mayusculasAviso = false;
    [SerializeField] private Color colorAviso = Color.white;
    [SerializeField] private Color colorContornoAviso = new Color(0.15f, 0.08f, 0.02f, 1f);
    [Range(0f, 1f)] [SerializeField] private float grosorContornoAviso = 0.25f;
    [SerializeField] private float espaciadoLetrasAviso = 0f;
    [Tooltip("Posicion vertical en pantalla: 0 = abajo, 1 = arriba")]
    [Range(0f, 1f)] [SerializeField] private float posicionVerticalAviso = 0.88f;
    [SerializeField] private float anchoAviso = 1200f;
    [Tooltip("Latido suave del texto (0 = sin latido)")]
    [SerializeField] private float latidoAviso = 0.06f;

    [Header("Cinematica del totem (Cinemachine)")]
    [SerializeField] private bool usarCinematica = true;
    [SerializeField] private float tiempoPaneo = 2f;        // ida y vuelta
    [SerializeField] private float tiempoMirandoTotem = 2.5f;
    [SerializeField] private float giroCinematica = 150f;   // grados/seg del totem mientras se ve
    [SerializeField] private float zoomCinematica = 0.8f;   // <1 = se acerca al totem (0.8 = 20 % mas cerca)

    [Header("Debug (solo Editor / Development Build)")]
    [SerializeField] private Key teclaTerminarNivel = Key.L;

    private bool victoriaActiva = false;
    private bool totemEnCurso = false;
    private GameObject totemActual;
    private GameObject canvasAviso;
    private Vector3 escalaOriginal;

    private void Awake()
    {
        if (marcoVictoria != null)
            escalaOriginal = marcoVictoria.localScale;

        if (panelVictoria != null)
            panelVictoria.SetActive(false);

        if (textoAvisoPropio != null)
            textoAvisoPropio.gameObject.SetActive(false);
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current[teclaTerminarNivel].wasPressedThisFrame)
        {
            Debug.Log("DEBUG: nivel terminado con la tecla " + teclaTerminarNivel);
            MostrarVictoria();
        }
#endif
    }

    // Se llama al ganar el nivel (onAllWavesCleared). Con totem: aparece el totem; sin totem: panel directo.
    public void MostrarVictoria()
    {
        if (victoriaActiva || totemEnCurso)
            return;

        if (totemPrefab == null)
        {
            MostrarPanel();
            return;
        }

        totemEnCurso = true;
        AparecerTotem();
    }

    private void AparecerTotem()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        Vector3 pos;

        if (puntoTotem != null)
            pos = puntoTotem.position;
        else if (jugador != null)
            pos = jugador.transform.position + jugador.transform.forward * distanciaAlJugador;
        else
            pos = transform.position;

        // Con punto fijo aparece exactamente ahi; si no, se pega al suelo / NavMesh
        if (puntoTotem == null)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out UnityEngine.AI.NavMeshHit nh, 4f, UnityEngine.AI.NavMesh.AllAreas))
                pos = nh.position;
            else if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit rh, 20f))
                pos = rh.point;
        }

        totemActual = Instantiate(totemPrefab, pos + Vector3.up * alturaSobrePunto, Quaternion.identity);
        totemActual.name = "Totem_Victoria";
        totemActual.transform.localScale = totemPrefab.transform.localScale * escalaTotem;

        if (totemMaterial != null)
        {
            foreach (Renderer r in totemActual.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = totemMaterial;
        }

        TotemRecogible t = totemActual.AddComponent<TotemRecogible>();
        t.victoria = this;
        t.radioRecoger = radioTocarTotem;
        if (jugador != null) t.jugador = jugador.transform;

        AudioManager.PlayVictory();

        if (usarCinematica && Camera.main != null && jugador != null)
        {
            t.enabled = false;                       // no se puede recoger durante la cinematica
            StartCoroutine(CinematicaTotem(t, jugador.transform));
        }
        else
        {
            CrearAviso();
        }
        Debug.Log("VICTORIA - Aparece el totem");
    }

    // Paneo Tuko -> totem -> Tuko con Cinemachine, mientras el totem aparece y gira
    private IEnumerator CinematicaTotem(TotemRecogible t, Transform jugador)
    {
        Camera cam = Camera.main;
        CameraFollow seguir = cam.GetComponent<CameraFollow>();
        PlayerController control = jugador.GetComponent<PlayerController>();
        if (seguir) seguir.enabled = false;
        if (control) control.enabled = false;

        CinemachineBrain brain = cam.GetComponent<CinemachineBrain>();
        bool brainNuevo = brain == null;
        if (brainNuevo) brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.enabled = true;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, tiempoPaneo);

        // Camara A: donde esta la camara ahora (mirando a Tuko)
        GameObject goA = new GameObject("VCam_Tuko");
        goA.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
        CinemachineCamera vcamA = goA.AddComponent<CinemachineCamera>();
        vcamA.Lens = LensSettings.FromCamera(cam);
        vcamA.Priority.Value = 20;
        yield return null;
        yield return null;

        // Camara B: misma inclinacion, desplazada hasta el totem
        Vector3 desplazamiento = t.transform.position - jugador.position;
        desplazamiento.y = 0f;
        GameObject goB = new GameObject("VCam_Totem");
        goB.transform.SetPositionAndRotation(cam.transform.position + desplazamiento, cam.transform.rotation);
        CinemachineCamera vcamB = goB.AddComponent<CinemachineCamera>();
        LensSettings lente = LensSettings.FromCamera(cam);
        lente.FieldOfView *= zoomCinematica;
        lente.OrthographicSize *= zoomCinematica;
        vcamB.Lens = lente;
        vcamB.Priority.Value = 30;

        // El totem aparece con un "pop" y gira durante el paneo
        Vector3 escalaFinal = t.transform.localScale;
        t.transform.localScale = Vector3.zero;
        float total = tiempoPaneo + tiempoMirandoTotem;
        float e = 0f;
        while (e < total)
        {
            e += Time.deltaTime;
            t.transform.Rotate(0f, giroCinematica * Time.deltaTime, 0f, Space.World);
            float k = Mathf.Clamp01((e - tiempoPaneo * 0.6f) / 0.6f);
            k = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);   // rebote suave
            if (e < tiempoPaneo * 0.6f) k = 0f;
            t.transform.localScale = escalaFinal * Mathf.Max(0f, k);
            yield return null;
        }
        t.transform.localScale = escalaFinal;

        // Volver a Tuko
        vcamB.Priority.Value = 0;
        yield return new WaitForSeconds(tiempoPaneo);

        Destroy(goA);
        Destroy(goB);
        brain.enabled = false;
        if (brainNuevo) Destroy(brain);
        if (seguir) seguir.enabled = true;
        if (control) control.enabled = true;

        t.enabled = true;
        CrearAviso();
    }

    private void CrearAviso()
    {
        string msg = mayusculasAviso ? mensajeTotem.ToUpper() : mensajeTotem;

        // Opcion 1: tu propio texto de la escena
        if (textoAvisoPropio != null)
        {
            textoAvisoPropio.text = msg;
            textoAvisoPropio.gameObject.SetActive(true);
            return;
        }

        // Opcion 2: se crea solo con el estilo de arriba
        canvasAviso = new GameObject("AvisoTotem", typeof(Canvas));
        Canvas c = canvasAviso.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 50;

        GameObject txt = new GameObject("Texto", typeof(RectTransform));
        txt.transform.SetParent(canvasAviso.transform, false);
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, posicionVerticalAviso);
        rt.anchorMax = new Vector2(0.5f, posicionVerticalAviso);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(anchoAviso, 150f);

        TMPro.TextMeshProUGUI tmp = txt.AddComponent<TMPro.TextMeshProUGUI>();
        if (fuenteAviso != null) tmp.font = fuenteAviso;
        tmp.text = msg;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = tamanoAviso;
        tmp.fontStyle = negritaAviso ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        tmp.color = colorAviso;
        tmp.characterSpacing = espaciadoLetrasAviso;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        if (grosorContornoAviso > 0f)
        {
            tmp.outlineColor = colorContornoAviso;
            tmp.outlineWidth = grosorContornoAviso;
        }

        if (latidoAviso > 0f)
            StartCoroutine(LatidoAviso(rt));
    }

    private IEnumerator LatidoAviso(RectTransform rt)
    {
        while (rt != null)
        {
            float k = 1f + Mathf.Sin(Time.unscaledTime * 4f) * latidoAviso;
            rt.localScale = new Vector3(k, k, 1f);
            yield return null;
        }
    }

    // Lo llama el totem cuando Tuko lo recoge
    public void TotemRecogido()
    {
        if (canvasAviso != null) Destroy(canvasAviso);
        if (textoAvisoPropio != null) textoAvisoPropio.gameObject.SetActive(false);
        totemEnCurso = false;
        MostrarPanel();
    }

    public void MostrarPanel()
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
        if (totemPrefab == null)
            AudioManager.PlayVictory();   // con totem, el sonido ya sono al aparecer

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

