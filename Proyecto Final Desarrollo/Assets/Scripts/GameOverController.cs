
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

    [Header("Muerte de Tuko")]
    [Tooltip("Animator de Tuko. Vacio = lo busca en el jugador.")]
    [SerializeField] private Animator animatorJugador;
    [Tooltip("Si esta activo, espera lo que dure el clip de muerte. Si no, usa Espera Fija.")]
    [SerializeField] private bool usarDuracionDelClip = true;
    [SerializeField] private string nombreClipMuerte = "death";
    [SerializeField] private float esperaFija = 2.5f;
    [Tooltip("Segundos extra despues de que termina la animacion, antes de mostrar el cartel.")]
    [SerializeField] private float pausaExtra = 0.8f;

    private bool gameOverActivo = false;
    private bool muerteEnCurso = false;
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SuperHash = Animator.StringToHash("Super");
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
            vidaJugador.onDeath.AddListener(AlMorirJugador);
    }

    private void Start()
    {
        if (vidaJugador != null && vidaJugador.IsDead)
            AlMorirJugador();
    }

    private void OnDisable()
    {
        if (vidaJugador != null)
            vidaJugador.onDeath.RemoveListener(AlMorirJugador);
    }

    // Tuko murio: reproduce la animacion de muerte y, pasados unos segundos, muestra el cartel
    public void AlMorirJugador()
    {
        if (muerteEnCurso || gameOverActivo)
            return;

        muerteEnCurso = true;
        StartCoroutine(SecuenciaMuerte());
    }

    private IEnumerator SecuenciaMuerte()
    {
        Animator anim = animatorJugador;
        if (anim == null && vidaJugador != null)
            anim = vidaJugador.GetComponentInChildren<Animator>();

        float espera = 0f;
        bool reprodujo = false;

        if (anim != null)
        {
            bool tieneDie = false;
            foreach (var p in anim.parameters)
                if (p.nameHash == DieHash) { tieneDie = true; break; }

            if (tieneDie)
            {
                anim.speed = 1f;
                anim.ResetTrigger(AttackHash);
                anim.ResetTrigger(SuperHash);
                anim.SetTrigger(DieHash);
                reprodujo = true;

                espera = esperaFija;
                if (usarDuracionDelClip && anim.runtimeAnimatorController != null)
                {
                    foreach (var c in anim.runtimeAnimatorController.animationClips)
                    {
                        if (c != null && c.name.ToLower().Contains(nombreClipMuerte.ToLower()))
                        {
                            espera = c.length;
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GameOverController] El Animator de Tuko no tiene el parametro 'Die'.");
            }
        }

        if (reprodujo)
            yield return new WaitForSecondsRealtime(espera + pausaExtra);

        MostrarGameOver();
    }

    public void MostrarGameOver()
    {
        if (gameOverActivo)
            return;

        gameOverActivo = true;

        // Detener la partida
        Time.timeScale = 0f;
		AudioManager.PlayGameOver();  // detiene la música y suena el game over

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

