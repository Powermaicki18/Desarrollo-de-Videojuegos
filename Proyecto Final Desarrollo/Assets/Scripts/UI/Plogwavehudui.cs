using System.Collections;
using UnityEngine;
using TMPro;

// Versión para canvas hecho a mano: tú diseñas los objetos y este script solo les pone el texto.
// Arrastra cada referencia en el Inspector.
public class PlogWaveHudUI : MonoBehaviour
{
    [Header("Referencias")]
    public EnemySpawner spawner;            // vacío = lo busca solo
    public TMP_Text waveText;               // "OLEADA 2/5"
    public TMP_Text enemiesText;            // "Enemigos: 7"
    public TMP_Text bannerText;             // aviso grande (opcional)
    public CanvasGroup bannerGroup;         // para el desvanecido del aviso (opcional)

    [Header("Textos")]
    public string waitingText = "PREPÁRATE";
    public string waveFormat = "OLEADA {0}/{1}";
    public string finalWaveText = "OLEADA FINAL";
    public string enemiesFormat = "{0}/{1}";   // {0} = enemigos restantes, {1} = total de la horda
    public string clearedText = "Zona limpia";
    public string bannerFormat = "OLEADA {0}";
    public string bannerFinalText = "¡OLEADA FINAL!";
    public string bannerClearedText = "¡ZONA LIMPIA!";

    [Header("Colores")]
    public Color normalColor = new Color(1f, 0.95f, 0.75f);
    public Color finalColor = new Color(1f, 0.35f, 0.25f);
    public Color clearedColor = new Color(0.5f, 1f, 0.55f);
    public float bannerSeconds = 2.2f;

    int lastWave = -1;
    bool done;

    void Start()
    {
        if (!spawner) spawner = FindFirstObjectByType<EnemySpawner>();
        if (!spawner) { Debug.LogWarning("[PlogWaveHudUI] No encuentro el EnemySpawner."); enabled = false; return; }
        spawner.showDebugHud = false;
        spawner.onAllWavesCleared.AddListener(() => { done = true; ShowBanner(bannerClearedText, clearedColor); });
        if (bannerGroup) bannerGroup.alpha = 0f;
        ShowBanner(waitingText, normalColor);   // aviso inicial antes de la primera horda
    }

    void Update()
    {
        int total = spawner.waveSizes != null ? spawner.waveSizes.Length : 0;
        int w = spawner.CurrentWave;
        bool isFinal = w == total && total > 0;

        if (waveText)
        {
            waveText.text = w <= 0 ? string.Format(waveFormat, 0, total) : (isFinal ? finalWaveText : string.Format(waveFormat, w, total));
            waveText.color = isFinal ? finalColor : normalColor;
        }
        if (enemiesText)
            enemiesText.text = done ? clearedText : string.Format(enemiesFormat, spawner.EnemiesRemaining, WaveTotal(w));

        if (w != lastWave && w > 0)
        {
            lastWave = w;
            ShowBanner(isFinal ? bannerFinalText : string.Format(bannerFormat, w), isFinal ? finalColor : normalColor);
        }
    }

    int WaveTotal(int wave)
    {
        var sizes = spawner.waveSizes;
        if (sizes == null || sizes.Length == 0) return 0;
        return sizes[Mathf.Clamp(wave - 1, 0, sizes.Length - 1)];
    }

    void ShowBanner(string text, Color c)
    {
        if (!bannerText) return;
        StopAllCoroutines();
        StartCoroutine(Banner(text, c));
    }

    IEnumerator Banner(string text, Color c)
    {
        bannerText.text = text; bannerText.color = c;
        float t = 0f;
        while (t < bannerSeconds)
        {
            t += Time.deltaTime;
            float k = t / bannerSeconds;
            if (bannerGroup) bannerGroup.alpha = k < 0.15f ? k / 0.15f : (k > 0.7f ? Mathf.Clamp01((1f - k) / 0.3f) : 1f);
            bannerText.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.25f - k) * 1.2f);
            yield return null;
        }
        if (bannerGroup) bannerGroup.alpha = 0f;
        bannerText.transform.localScale = Vector3.one;
    }
}