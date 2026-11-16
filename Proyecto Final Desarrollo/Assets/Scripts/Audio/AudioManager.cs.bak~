using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor central de audio. Persiste entre escenas (DontDestroyOnLoad).
/// - Cambia la música automáticamente según la escena.
/// - Reproduce los sonidos de interfaz (botón, victoria, derrota).
/// - Guarda en PlayerPrefs si la música / los efectos están activados.
///
/// Se crea solo desde un prefab llamado "AudioManager" dentro de una carpeta Resources,
/// así funciona aunque pruebes cualquier escena directamente con Play.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public static event Action OnSettingsChanged;

    [Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.6f;
    }

    [Header("Música por escena")]
    public SceneMusic[] sceneMusic;

    [Header("Sonidos de interfaz")]
    public AudioClip buttonClip;
    public AudioClip victoryClip;
    public AudioClip gameOverClip;
    [Range(0f, 1f)] public float uiVolume = 1f;

    [Header("Fuentes (se crean solas si están vacías)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    const string MusicKey = "audio_music_on";
    const string SfxKey = "audio_sfx_on";

    public static bool MusicOn { get; private set; } = true;
    public static bool SfxOn { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab) Instantiate(prefab);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        SfxOn = PlayerPrefs.GetInt(SfxKey, 1) == 1;

        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.mute = !MusicOn;

        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var m in sceneMusic)
        {
            if (m.sceneName == scene.name)
            {
                PlayMusic(m.clip, m.volume);
                return;
            }
        }
        StopMusic();   // escena sin música asignada
    }

    // ---------- Música ----------
    public void PlayMusic(AudioClip clip, float volume = 0.6f)
    {
        if (!clip) { StopMusic(); return; }

        musicSource.volume = volume;
        // Si ya suena ese mismo clip (menú -> cómic) no se reinicia
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // ---------- Efectos ----------
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!SfxOn || !clip) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // ---------- Atajos estáticos (seguros aunque no exista el manager) ----------
    public static void Sfx(AudioClip clip, float volume = 1f)
    {
        if (Instance) Instance.PlaySfx(clip, volume);
    }

    public static void PlayButton()
    {
        if (Instance) Instance.PlaySfx(Instance.buttonClip, Instance.uiVolume);
    }

    public static void PlayVictory()
    {
        if (!Instance) return;
        Instance.StopMusic();
        Instance.PlaySfx(Instance.victoryClip, Instance.uiVolume);
    }

    public static void PlayGameOver()
    {
        if (!Instance) return;
        Instance.StopMusic();
        Instance.PlaySfx(Instance.gameOverClip, Instance.uiVolume);
    }

    // ---------- Menú de pausa ----------
    public void ToggleMusic() => SetMusic(!MusicOn);
    public void ToggleSfx() => SetSfx(!SfxOn);

    public void SetMusic(bool on)
    {
        MusicOn = on;
        musicSource.mute = !on;   // mute (no stop): al reactivar sigue donde iba
        PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
        OnSettingsChanged?.Invoke();
    }

    public void SetSfx(bool on)
    {
        SfxOn = on;
        if (!on) sfxSource.Stop();
        PlayerPrefs.SetInt(SfxKey, on ? 1 : 0);
        OnSettingsChanged?.Invoke();
    }
}