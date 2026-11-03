using UnityEngine;

// Ponlo en el prefab de cada enemigo (junto a Health). Al morir puede soltar un pickup de vida.
[RequireComponent(typeof(Health))]
public class PlogDropOnDeath : MonoBehaviour
{
    public GameObject pickupPrefab;
    [Range(0f, 1f)] public float dropChance = 0.15f;
    public Vector3 offset = new Vector3(0f, 0.6f, 0f);

    void Awake() { GetComponent<Health>().onDeath.AddListener(Drop); }

    void Drop()
    {
        if (pickupPrefab && Random.value <= dropChance)
            Instantiate(pickupPrefab, transform.position + offset, Quaternion.identity);
    }
}