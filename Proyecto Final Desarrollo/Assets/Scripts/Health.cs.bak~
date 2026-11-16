
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    // Todos los personajes con vida activos.
    // Los ataques pueden recorrer esta lista.
    public static readonly List<Health> All = new List<Health>();

    [Header("Configuracion de vida")]
    public float maxHealth = 100f;

    [Header("Barra de vida")]
    public bool showDebugBar = true;
    public float barHeight = 2.4f;
    [Tooltip("Color de la barra del jugador (el que tenga el tag Player).")]
    public Color playerBarColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    [Tooltip("Color de la barra de los enemigos y demas.")]
    public Color enemyBarColor = Color.red;
    [Tooltip("Color mientras el personaje es invulnerable.")]
    public Color invulnerableBarColor = Color.cyan;

    [Header("Dano recibido")]
    [Tooltip("Segundos de invulnerabilidad despues de recibir un golpe (0 = sin invulnerabilidad). Para el jugador prueba 0.4.")]
    public float postHitInvulnerability = 0f;
    [Tooltip("Escribe en la Consola cada golpe: quien lo recibe, cuanto dano y cuanta vida queda.")]
    public bool logDamage = false;

    [Header("Zona de impacto (opcional)")]
    public Collider hurtbox;

    [Header("Eventos")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0f;

    public bool IsInvulnerable =>
        Time.time < invulnerableUntil;

    private float invulnerableUntil;

    // ==========================================
    // INICIALIZAR VIDA
    // ==========================================

    private void Awake()
    {
        CurrentHealth = maxHealth;

        if (!hurtbox)
        {
            hurtbox = GetComponentInChildren<BoxCollider>();
        }
    }

    // ==========================================
    // REGISTRO DE PERSONAJES ACTIVOS
    // ==========================================

    private void OnEnable()
    {
        if (!All.Contains(this))
        {
            All.Add(this);
        }
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    // ==========================================
    // INVULNERABILIDAD
    // ==========================================

    public void GrantInvulnerability(float seconds)
    {
        invulnerableUntil = Mathf.Max(
            invulnerableUntil,
            Time.time + seconds
        );
    }

    // ==========================================
    // RECIBIR DAÑO
    // ==========================================

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable)
            return;

        float before = CurrentHealth;

        CurrentHealth = Mathf.Max(
            0f,
            CurrentHealth - amount
        );

        if (logDamage)
        {
            Debug.Log(
                "[Health] " + name + " recibio " + amount +
                " de dano: " + before + " -> " + CurrentHealth +
                " / " + maxHealth
            );
        }

        if (postHitInvulnerability > 0f && !IsDead)
            GrantInvulnerability(postHitInvulnerability);

        onDamaged?.Invoke();

        if (IsDead)
        {
            onDeath?.Invoke();
        }
    }

    // ==========================================
    // RECUPERAR VIDA
    // ==========================================

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        CurrentHealth = Mathf.Min(
            maxHealth,
            CurrentHealth + amount
        );
    }

    // ==========================================
    // PUNTO CERCANO DE IMPACTO
    // ==========================================

    public Vector3 ClosestPoint(Vector3 from)
    {
        return hurtbox
            ? hurtbox.ClosestPoint(from)
            : transform.position;
    }

    // ==========================================
    // DIBUJAR BARRAS DE VIDA
    // ==========================================

    private void OnGUI()
    {
        // NUEVO:
        // Ocultar las barras cuando el juego
        // esté pausado o mostrando instrucciones.
        if (Time.timeScale == 0f)
            return;

        // Mantener comprobaciones originales.
        if (!showDebugBar || IsDead || !Camera.main)
            return;

        // Obtener la posicion del personaje
        // en la pantalla.
        Vector3 sp = Camera.main.WorldToScreenPoint(
            transform.position + Vector3.up * barHeight
        );

        // No dibujar si esta detras de la camara.
        if (sp.z < 0f)
            return;

        // Tamaño de la barra.
        float w = 70f;
        float h = 9f;

        Rect r = new Rect(
            sp.x - w / 2f,
            Screen.height - sp.y,
            w,
            h
        );

        // Dibujar fondo negro.
        GUI.color = Color.black;

        GUI.DrawTexture(
            r,
            Texture2D.whiteTexture
        );

        // Verde = jugador, rojo = enemigos.
        // Cian = personaje invulnerable.
        GUI.color = IsInvulnerable
            ? invulnerableBarColor
            : (CompareTag("Player") ? playerBarColor : enemyBarColor);

        // Calcular porcentaje de vida.
        float porcentajeVida = maxHealth > 0f
            ? Mathf.Clamp01(CurrentHealth / maxHealth)
            : 0f;

        // Dibujar vida restante.
        GUI.DrawTexture(
            new Rect(
                r.x + 1f,
                r.y + 1f,
                (w - 2f) * porcentajeVida,
                h - 2f
            ),
            Texture2D.whiteTexture
        );

        // Restaurar color.
        GUI.color = Color.white;
    }
}

