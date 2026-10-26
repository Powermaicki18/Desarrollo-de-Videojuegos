using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    // Todos los personajes con vida activos (los ataques los recorren)
    public static readonly List<Health> All = new List<Health>();

    public float maxHealth = 100f;
    public bool showDebugBar = true;
    public float barHeight = 2.4f;

    [Header("Zona de impacto (opcional)")]
    public Collider hurtbox;               // caja ancha (trigger) que cubre cuerpo y brazos

    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    float invulnerableUntil;

    void Awake()
    {
        CurrentHealth = maxHealth;
        if (!hurtbox) hurtbox = GetComponentInChildren<BoxCollider>();
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Durante estos segundos no recibe daño
    public void GrantInvulnerability(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        onDamaged?.Invoke();
        if (IsDead) onDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    // Punto del personaje más cercano a "from" (usa la caja ancha si existe)
    public Vector3 ClosestPoint(Vector3 from)
    {
        return hurtbox ? hurtbox.ClosestPoint(from) : transform.position;
    }

    void OnGUI()
    {
        if (!showDebugBar || !Camera.main) return;
        Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * barHeight);
        if (sp.z < 0) return;
        float w = 70f, h = 9f;
        var r = new Rect(sp.x - w / 2f, Screen.height - sp.y, w, h);
        GUI.color = Color.black; GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = IsInvulnerable ? Color.cyan : Color.red;    // cian = invulnerable
        GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (w - 2) * (CurrentHealth / maxHealth), h - 2), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}