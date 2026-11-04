
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Health vidaJugador;
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private RectTransform marcoPerdiste;

    [Header("Animacion POP")]
    [SerializeField] private float duracionPop = 0.4f;
    [SerializeField] private float escalaInicial = 0.75f;
    [SerializeField] private float escalaRebote = 1.05f;

    private bool gameOverActivo = false;
    private Vector3 escalaOriginal;

    private void Awake()
    {
        if (marcoPerdiste != null)
            escalaOriginal = marcoPerdiste.localScale;

        if (panelGameOver != null)
            panelGameOver.SetActive(false);
    }

    private void OnEnable()
    {
        if (vidaJugador != null)
            vidaJugador.onDeath.AddListener(MostrarGameOver);
    }

    private void Start()
    {
        if (vidaJugador != null && vidaJugador.IsDead)
            MostrarGameOver();
    }

    private void OnDisable()
    {
        if (vidaJugador != null)
            vidaJugador.onDeath.RemoveListener(MostrarGameOver);
    }

    public void MostrarGameOver()
    {
        if (gameOverActivo)
            return;

        gameOverActivo = true;

        // Detener la partida
        Time.timeScale = 0f;

        // Mostrar pantalla de derrota
        if (panelGameOver != null)
            panelGameOver.SetActive(true);

        // Animar el marco
        if (marcoPerdiste != null)
            StartCoroutine(AnimarMarco());

        Debug.Log("GAME OVER - Tuko ha muerto");
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
            marcoPerdiste.localScale = hasta;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            t = t * t * (3f - 2f * t);

            marcoPerdiste.localScale =
                Vector3.LerpUnclamped(desde, hasta, t);

            yield return null;
        }

        marcoPerdiste.localScale = hasta;
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        gameOverActivo = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    private void OnDestroy()
    {
        if (gameOverActivo)
            Time.timeScale = 1f;
    }
}

