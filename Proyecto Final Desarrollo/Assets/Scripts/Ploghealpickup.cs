using UnityEngine;

// Pon esto en un objeto con Collider (Is Trigger): un cristal/corazón que cura al Player al tocarlo.
public class PlogHealPickup : MonoBehaviour
{
    public float healAmount = 20f;
    public float lifetime = 12f;      // 0 = no desaparece
    public float bobHeight = 0.2f;
    public float spinSpeed = 90f;

    Vector3 start;

    void Reset() { var c = GetComponent<Collider>(); if (c) c.isTrigger = true; }

    void Start()
    {
        start = transform.position;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = start + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        var h = other.GetComponentInParent<Health>();
        if (h == null || h.IsDead || !h.CompareTag("Player")) return;
        if (h.CurrentHealth >= h.maxHealth) return;   // no se gasta si ya tienes la vida llena
        h.Heal(healAmount);
        Destroy(gameObject);
    }
}