
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausaController : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private RectTransform marcoPausa;
    [SerializeField] private GameObject botonPausa;

    [Header("Animacion POP")]
    [SerializeField] private float duracionPop = 0.3f;
    [SerializeField] private float escalaInicial = 0.75f;
    [SerializeField] private float escalaRebote = 1.05f;

    [Header("Musica")]
    [SerializeField] private Image imagenBotonMusica;
    [SerializeField] private Sprite musicaActiva;
    [SerializeField] private Sprite musicaDesactivada;
    [SerializeField] private AudioSource[] fuentesMusica;

    [Header("Sonido")]
    [SerializeField] private Image imagenBotonSonido;
    [SerializeField] private Sprite sonidoActivo;
    [SerializeField] private Sprite sonidoDesactivado;
    [SerializeField] private AudioSource[] fuentesSonido;

    [Header("Escenas")]
    [SerializeField] private string escenaMenuPrincipal = "MenuPrincipal";

    private bool estaPausado = false;
    private bool musicaEncendida = true;
    private bool sonidoEncendido = true;
    private Vector3 escalaOriginal;
    private Coroutine animacionActual;

    private void Awake()
    {
        if (marcoPausa != null)
            escalaOriginal = marcoPausa.localScale;

        if (panelPausa != null)
            panelPausa.SetActive(false);
    }

    public void AbrirPausa()
    {
        if (estaPausado)
            return;

        // No abrir pausa durante las instrucciones
        if (Time.timeScale == 0f)
            return;
        // (Las barras de vida se dibujan con OnGUI y Health ya las oculta con timeScale 0,
        //  asi que no hace falta buscarlas en toda la escena.)

        estaPausado = true;
        Time.timeScale = 0f;

        panelPausa.SetActive(true);

        if (botonPausa != null)
            botonPausa.SetActive(false);

        if (marcoPausa != null)
        {
            if (animacionActual != null)
                StopCoroutine(animacionActual);

            animacionActual = StartCoroutine(AnimarMarco());
        }

        Debug.Log("Juego pausado");
    }

    public void ReanudarJuego()
    {
        if (!estaPausado)
            return;

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
        }

        if (marcoPausa != null)
            marcoPausa.localScale = escalaOriginal;

        panelPausa.SetActive(false);

        if (botonPausa != null)
            botonPausa.SetActive(true);

        estaPausado = false;
        Time.timeScale = 1f;

        Debug.Log("Juego reanudado");
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        estaPausado = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        estaPausado = false;

        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    public void AlternarMusica()
    {
        musicaEncendida = !musicaEncendida;

        foreach (AudioSource fuente in fuentesMusica)
        {
            if (fuente != null)
                fuente.mute = !musicaEncendida;
        }

        if (imagenBotonMusica != null)
        {
            imagenBotonMusica.sprite = musicaEncendida
                ? musicaActiva
                : musicaDesactivada;
        }
    }

    public void AlternarSonido()
    {
        sonidoEncendido = !sonidoEncendido;

        foreach (AudioSource fuente in fuentesSonido)
        {
            if (fuente != null)
                fuente.mute = !sonidoEncendido;
        }

        if (imagenBotonSonido != null)
        {
            imagenBotonSonido.sprite = sonidoEncendido
                ? sonidoActivo
                : sonidoDesactivado;
        }
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

        animacionActual = null;
    }

    private IEnumerator AnimarEscala(
        Vector3 desde,
        Vector3 hasta,
        float duracion
    )
    {
        if (duracion <= 0f)
        {
            marcoPausa.localScale = hasta;
            yield break;
        }

        float tiempo = 0f;
        marcoPausa.localScale = desde;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            t = t * t * (3f - 2f * t);

            marcoPausa.localScale =
                Vector3.LerpUnclamped(desde, hasta, t);

            yield return null;
        }

        marcoPausa.localScale = hasta;
    }

    private void OnDestroy()
    {
        // Evita que quede pausado al salir de la escena
        if (estaPausado)
            Time.timeScale = 1f;
    }

    [Header("Barras de vida de enemigos")]
    [SerializeField] private string nombreBarraVida = "HealthBar";

    private readonly System.Collections.Generic.Dictionary<
        GameObject, bool> barrasVidaOcultas = new();

    private void OcultarBarrasVida()
    {
        barrasVidaOcultas.Clear();

        // Buscar todos los objetos de la escena,
        // incluyendo los hijos de los enemigos.
        Transform[] objetos =
            FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Transform objeto in objetos)
        {
            if (objeto.name != nombreBarraVida)
                continue;

            GameObject barra = objeto.gameObject;

            barrasVidaOcultas[barra] = barra.activeSelf;

            if (barra.activeSelf)
                barra.SetActive(false);
        }
    }

    private void RestaurarBarrasVida()
    {
        foreach (var elemento in barrasVidaOcultas)
        {
            if (elemento.Key != null)
            {
                elemento.Key.SetActive(elemento.Value);
            }
        }

        barrasVidaOcultas.Clear();
    }

}
