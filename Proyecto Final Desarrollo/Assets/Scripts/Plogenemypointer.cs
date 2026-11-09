using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cuando quedan pocos enemigos, muestra flechitas alrededor del jugador que apuntan hacia ellos.
// Ponlo en cualquier objeto de la escena (por ejemplo el EnemySpawner o el HUD). Crea su propio canvas.
public class PlogEnemyPointer : MonoBehaviour
{
    [Header("Referencias (si quedan vacías las busca solas)")]
    public EnemySpawner spawner;
    public Transform player;

    [Header("Cuándo aparecen")]
    public int showWhenRemaining = 3;          // aparecen cuando quedan este número de enemigos o menos
    public float hideWithinMeters = 5f;        // no muestra la flecha de un enemigo que ya está cerca
    public bool onlyOffscreen = false;         // true = solo flecha para enemigos fuera de la pantalla

    [Header("Aspecto")]
    public float radius = 130f;                // distancia de la flecha al jugador (en píxeles de 1080p)
    public float arrowSize = 64f;
    public Color arrowColor = new Color(1f, 0.82f, 0.2f, 1f);

    RectTransform canvasRect;
    Image[] arrows;
    Sprite sprite;
    Texture2D tex;
    Camera cam;
    GameObject canvasGO;
    readonly List<Health> enemies = new List<Health>();

    void Start()
    {
        if (!spawner) spawner = FindFirstObjectByType<EnemySpawner>();
        FindPlayer();
        BuildCanvas();
    }

    void FindPlayer()
    {
        if (player) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void BuildCanvas()
    {
        canvasGO = new GameObject("HUD_FlechasEnemigos");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasGO.GetComponent<RectTransform>();

        sprite = MakeArrowSprite();
        int n = Mathf.Max(1, showWhenRemaining);
        arrows = new Image[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Flecha" + i, typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * arrowSize;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = arrowColor;
            img.raycastTarget = false;
            go.SetActive(false);
            arrows[i] = img;
        }
    }

    int RemainingCount()
    {
        if (spawner) return spawner.EnemiesRemaining;
        return enemies.Count;
    }

    void LateUpdate()
    {
        if (arrows == null) return;
        if (!player) FindPlayer();
        if (!cam) cam = Camera.main;

        // enemigos vivos (todo Health que no sea el jugador)
        enemies.Clear();
        foreach (var h in Health.All)
        {
            if (h == null || h.IsDead || !h.isActiveAndEnabled) continue;
            if (player && h.transform == player) continue;
            if (h.CompareTag("Player")) continue;
            enemies.Add(h);
        }

        bool active = player && cam && Time.timeScale > 0f && enemies.Count > 0 && RemainingCount() <= showWhenRemaining;
        if (!active) { HideFrom(0); return; }

        Vector3 pPos = player.position;
        enemies.Sort((a, b) =>
            (a.transform.position - pPos).sqrMagnitude.CompareTo((b.transform.position - pPos).sqrMagnitude));

        Vector3 pScr = cam.WorldToScreenPoint(pPos + Vector3.up);
        float rpx = radius * Screen.height / 1080f;
        float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);

        int used = 0;
        for (int i = 0; i < enemies.Count && used < arrows.Length; i++)
        {
            Vector3 ePos = enemies[i].transform.position;
            Vector3 flat = ePos - pPos; flat.y = 0f;
            if (flat.magnitude < hideWithinMeters) continue;

            Vector3 eScr = cam.WorldToScreenPoint(ePos);
            bool onScreen = eScr.z > 0f && eScr.x > 0f && eScr.x < Screen.width && eScr.y > 0f && eScr.y < Screen.height;
            if (onlyOffscreen && onScreen) continue;

            Vector2 dir = new Vector2(eScr.x - pScr.x, eScr.y - pScr.y);
            if (eScr.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 1f) continue;
            dir.Normalize();

            Vector2 pos = new Vector2(pScr.x, pScr.y) + dir * rpx;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pos, null, out Vector2 local);

            var a = arrows[used++];
            a.gameObject.SetActive(true);
            var rt = a.rectTransform;
            rt.anchoredPosition = local;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            rt.localScale = Vector3.one * pulse;
        }
        HideFrom(used);
    }

    void HideFrom(int start)
    {
        for (int i = start; i < arrows.Length; i++)
            if (arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
    }

    // Flecha con contorno oscuro, dibujada por código (apunta hacia arriba)
    Sprite MakeArrowSprite()
    {
        const int S = 96;
        Vector2[] poly = { new Vector2(48, 90), new Vector2(10, 12), new Vector2(48, 30), new Vector2(86, 12) };
        var inside = new bool[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                inside[y * S + x] = PointInPoly(new Vector2(x + 0.5f, y + 0.5f), poly);

        tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[S * S];
        const int border = 5;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                if (inside[y * S + x]) { px[y * S + x] = new Color32(255, 255, 255, 255); continue; }
                bool near = false;
                for (int dy = -border; dy <= border && !near; dy++)
                    for (int dx = -border; dx <= border; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= S || ny >= S) continue;
                        if (dx * dx + dy * dy <= border * border && inside[ny * S + nx]) { near = true; break; }
                    }
                px[y * S + x] = near ? new Color32(40, 20, 10, 255) : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool PointInPoly(Vector2 p, Vector2[] v)
    {
        bool c = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            if (((v[i].y > p.y) != (v[j].y > p.y)) &&
                (p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x))
                c = !c;
        return c;
    }

    void OnDestroy()
    {
        if (canvasGO) Destroy(canvasGO);
        if (sprite) Destroy(sprite);
        if (tex) Destroy(tex);
    }
}