using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;                 // el jugador
    public Vector3 focusOffset = new Vector3(0, 0, 1.5f); // mira un poco adelante del jugador

    [Header("Encuadre")]
    [Range(30f, 80f)] public float pitch = 55f;   // inclinación
    public float yaw = 0f;                        // giro (0 = mira hacia +Z)
    public float distance = 20f;                  // lejanía (en ortográfica no afecta el zoom)
    public float orthoSize = 9f;                  // solo si la cámara es ortográfica

    [Header("Suavizado")]
    public float smoothTime = 0.12f;

    [Header("Límites del mapa (en el suelo, XZ)")]
    public bool clampToMap = true;
    public Vector2 mapMin = new Vector2(-15, -9);
    public Vector2 mapMax = new Vector2(15, 9);

    Vector3 focus, vel;
    Camera cam;

    void Awake() { cam = GetComponent<Camera>(); }

    void Start()
    {
        if (target) focus = GetTargetFocus();
        Apply(focus);
    }

    Vector3 GetTargetFocus()
    {
        Vector3 p = target.position + Quaternion.Euler(0, yaw, 0) * focusOffset;
        p.y = 0;
        if (clampToMap)
        {
            p.x = Mathf.Clamp(p.x, mapMin.x, mapMax.x);
            p.z = Mathf.Clamp(p.z, mapMin.y, mapMax.y);
        }
        return p;
    }

    void LateUpdate()
    {
        if (!target) return;
        focus = Vector3.SmoothDamp(focus, GetTargetFocus(), ref vel, smoothTime);
        Apply(focus);
    }

    void Apply(Vector3 f)
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        transform.rotation = rot;
        transform.position = f - rot * Vector3.forward * distance;
        if (cam && cam.orthographic) cam.orthographicSize = orthoSize;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 c = new Vector3((mapMin.x + mapMax.x) / 2, 0, (mapMin.y + mapMax.y) / 2);
        Gizmos.DrawWireCube(c, new Vector3(mapMax.x - mapMin.x, 0.1f, mapMax.y - mapMin.y));
    }
}