using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemigos (se elige uno al azar)")]
    public GameObject[] enemyPrefabs;

    [Header("Oleadas (enemigos por horda; la última es la final)")]
    public int[] waveSizes = { 3, 5, 8, 12, 20 };
    public float timeBetweenWaves = 4f;

    [Header("Ritmo de aparición")]
    public float firstSpawnInterval = 1.0f;   // pausa entre enemigos en la primera horda
    public float lastSpawnInterval = 0.4f;    // en la horda final: salen más rápido de lo que los matas
    public int maxAlive = 20;                 // tope de enemigos vivos a la vez (>= tamaño de la final)

    [Header("Dónde aparecen")]
    public Transform player;                  // si queda vacío usa el tag Player
    public float minDistance = 12f;
    public float maxDistance = 18f;
    public Vector2 mapMin = new Vector2(-30f, -30f);
    public Vector2 mapMax = new Vector2(30f, 30f);
    public LayerMask groundMask = ~0;
    public LayerMask blockMask = 0;
    public float spawnHeightOffset = 0.6f;
    public bool useNavMesh = false;

    [Header("Variación procedural")]
    [Range(0f, 0.3f)] public float scaleVariation = 0.1f;
    [Range(0f, 0.5f)] public float speedVariation = 0.2f;

    [Header("Eventos")]
    public UnityEvent onWaveStarted;
    public UnityEvent onFinalWaveStarted;     // útil para música o aviso en pantalla
    public UnityEvent onWaveCleared;
    public UnityEvent onAllWavesCleared;      // aquí se desbloqueará la siguiente región

    public bool showDebugHud = true;

    public int CurrentWave { get; private set; }

    readonly List<GameObject> spawned = new List<GameObject>();
    Health playerHealth;

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player) playerHealth = player.GetComponent<Health>();
        StartCoroutine(Run());
    }

    int Alive()
    {
        spawned.RemoveAll(e => e == null);
        int n = 0;
        foreach (var e in spawned)
        {
            var h = e.GetComponent<Health>();
            if (h == null || !h.IsDead) n++;
        }
        return n;
    }

    bool PlayerAlive() => player && (playerHealth == null || !playerHealth.IsDead);

    IEnumerator Run()
    {
        if (waveSizes == null || waveSizes.Length == 0) yield break;
        yield return new WaitForSeconds(1f);

        for (int w = 0; w < waveSizes.Length; w++)
        {
            CurrentWave = w + 1;
            bool isFinal = (w == waveSizes.Length - 1);
            int count = waveSizes[w];

            // el intervalo baja poco a poco: en la final salen muy seguidos
            float t = waveSizes.Length > 1 ? (float)w / (waveSizes.Length - 1) : 1f;
            float interval = Mathf.Lerp(firstSpawnInterval, lastSpawnInterval, t);

            onWaveStarted?.Invoke();
            if (isFinal) onFinalWaveStarted?.Invoke();

            for (int i = 0; i < count; i++)
            {
                while (Alive() >= maxAlive || !PlayerAlive()) yield return null;
                SpawnOne();
                yield return new WaitForSeconds(interval);
            }

            while (Alive() > 0) yield return null;   // espera a que mueran todos
            onWaveCleared?.Invoke();

            if (!isFinal) yield return new WaitForSeconds(timeBetweenWaves);
        }
        onAllWavesCleared?.Invoke();
    }

    void SpawnOne()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || !player) return;
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (!prefab || !TryGetSpawnPoint(out Vector3 pos)) return;

        var e = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        e.transform.localScale *= 1f + Random.Range(-scaleVariation, scaleVariation);
        var ec = e.GetComponent<EnemyController>();
        if (ec) ec.moveSpeed *= 1f + Random.Range(-speedVariation, speedVariation);
        spawned.Add(e);
    }

    bool TryGetSpawnPoint(out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float d = Random.Range(minDistance, maxDistance);
            Vector3 p = player.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * d;
            p.x = Mathf.Clamp(p.x, mapMin.x, mapMax.x);
            p.z = Mathf.Clamp(p.z, mapMin.y, mapMax.y);

            Vector3 flat = p - player.position; flat.y = 0f;
            if (flat.magnitude < minDistance * 0.7f) continue;

            Vector3 from = new Vector3(p.x, player.position.y + 50f, p.z);
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 120f, groundMask, QueryTriggerInteraction.Ignore)) continue;
            if (hit.normal.y < 0.7f) continue;
            if (hit.collider.GetComponentInParent<Health>() != null) continue;

            Vector3 pos = hit.point;
            if (useNavMesh)
            {
                if (!NavMesh.SamplePosition(pos, out NavMeshHit nh, 2f, NavMesh.AllAreas)) continue;
                pos = nh.position;
            }
            if (blockMask.value != 0 &&
                Physics.CheckSphere(pos + Vector3.up * 0.5f, 0.6f, blockMask, QueryTriggerInteraction.Ignore)) continue;

            pos.y += spawnHeightOffset;
            result = pos;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    void OnGUI()
    {
        if (!showDebugHud || waveSizes == null) return;
        GUI.Label(new Rect(12, 8, 400, 24), $"Oleada {CurrentWave}/{waveSizes.Length}   Enemigos: {Alive()}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 c = new Vector3((mapMin.x + mapMax.x) * 0.5f, 0f, (mapMin.y + mapMax.y) * 0.5f);
        Vector3 s = new Vector3(mapMax.x - mapMin.x, 0.1f, mapMax.y - mapMin.y);
        Gizmos.DrawWireCube(c, s);
    }
}