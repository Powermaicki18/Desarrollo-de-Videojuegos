using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Gestor de rendimiento para celular. Se crea solo al iniciar el juego (no hay que ponerlo en ninguna escena).
// Arranca con la mejor calidad configurada y, si el celular no sostiene los FPS, baja la calidad por pasos
// (primero un poco de resolucion y distancia de sombras; al final limita a 30 FPS estables). Nunca sube de nuevo, para no oscilar.
public static class PlogRendimiento
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        if (!Application.isMobilePlatform) return;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        var go = new GameObject("PlogRendimiento");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PlogRendimientoAuto>();
    }
}

public class PlogRendimientoAuto : MonoBehaviour
{
    const float FpsMinimo = 45f;      // por debajo de esto se baja la calidad un paso
    const float Ventana = 4f;         // segundos por medicion
    const float EsperaTrasCarga = 3f; // se ignora lo que pasa justo al cargar una escena

    UniversalRenderPipelineAsset urp;
    float escalaOriginal, sombrasOriginal;
    int nivel;
    float t, acum; int frames, malas;
    float esperar = EsperaTrasCarga;

    void Awake()
    {
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null) { escalaOriginal = urp.renderScale; sombrasOriginal = urp.shadowDistance; }
        SceneManager.sceneLoaded += (s, m) => { esperar = EsperaTrasCarga; t = acum = 0; frames = 0; malas = 0; if (nivel >= 3) Application.targetFrameRate = 30; };
    }

    void Update()
    {
        if (urp == null || nivel >= 3) return;
        float dt = Time.unscaledDeltaTime;
        if (esperar > 0f) { esperar -= dt; return; }
        if (Time.timeScale == 0f) return;   // en pausa no se mide
        t += dt; acum += dt; frames++;
        if (t < Ventana) return;
        float fps = frames / acum;
        t = acum = 0; frames = 0;
        if (fps < FpsMinimo) malas++; else malas = 0;
        if (malas >= 2) { malas = 0; Bajar(); }
    }

    void Bajar()
    {
        nivel++;
        switch (nivel)
        {
            case 1: urp.renderScale = Mathf.Min(escalaOriginal, 0.8f); urp.shadowDistance = sombrasOriginal * 0.6f; break;
            case 2: urp.renderScale = Mathf.Min(escalaOriginal, 0.7f); urp.shadowDistance = sombrasOriginal * 0.35f; break;
            case 3: urp.renderScale = Mathf.Min(escalaOriginal, 0.8f); urp.shadowDistance = sombrasOriginal * 0.6f; Application.targetFrameRate = 30; break;
        }
        Debug.Log("[Rendimiento] Bajando calidad al nivel " + nivel);
    }
}
