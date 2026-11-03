using UnityEngine;

// Zona que cura al Player mientras está dentro (ej. el oasis). Collider con Is Trigger.
// Tiene una reserva limitada: cuando se agota, entra en cooldown y luego se recarga.
public class PlogHealZone : MonoBehaviour
{
    public float healPerSecond = 6f;
    [Tooltip("Vida total que puede dar antes de agotarse")]
    public float reserve = 40f;
    [Tooltip("Segundos que tarda en recargarse cuando se agota")]
    public float cooldown = 30f;

    float current;
    float readyAt;

    public bool OnCooldown => Time.time < readyAt;
    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
    public float ReserveFraction => reserve <= 0f ? 0f : current / reserve;

    void Reset() { var c = GetComponent<Collider>(); if (c) c.isTrigger = true; }

    void Awake() { current = reserve; }

    void Update()
    {
        // al terminar el cooldown se recarga por completo
        if (current <= 0f && !OnCooldown) current = reserve;
    }

    void OnTriggerStay(Collider other)
    {
        if (OnCooldown || current <= 0f) return;
        var h = other.GetComponentInParent<Health>();
        if (h == null || h.IsDead || !h.CompareTag("Player")) return;

        float before = h.CurrentHealth;
        float want = Mathf.Min(healPerSecond * Time.deltaTime, current);
        h.Heal(want);
        float given = h.CurrentHealth - before;
        current -= given;

        if (current <= 0.01f)
        {
            current = 0f;
            readyAt = Time.time + cooldown;
        }
    }
}