using UnityEngine;

/// <summary>
/// Sonidos de un enemigo. Se pone en el PREFAB del enemigo (pollo y Nox),
/// así cada enemigo que salga en una oleada ya trae su propio audio.
/// - Pollo: ataque + muerte.
/// - Nox (alado): sonido de vuelo en bucle (+ muerte si quieres).
/// Deja vacío el clip que no uses.
/// </summary>
public class EnemyAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip attackClip;
    public AudioClip deathClip;
    public AudioClip flyLoopClip;

    [Header("Volumen")]
    [Range(0f, 1f)] public float attackVolume = 1f;
    [Range(0f, 1f)] public float deathVolume = 1f;
    [Range(0f, 1f)] public float flyVolume = 0.4f;

    [Header("Variación (para que 20 enemigos no suenen idénticos)")]
    [Range(0f, 0.3f)] public float pitchVariation = 0.1f;

    [Header("Sonido 3D")]
    [Range(0f, 1f)] public float spatialBlend = 0.8f;
    public float minDistance = 8f;     // con cámara alta/lejana sube estos valores
    public float maxDistance = 40f;

    AudioSource src;
    bool dead;

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = spatialBlend;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
    }

    void Start()
    {
        if (flyLoopClip)
        {
            src.clip = flyLoopClip;
            src.loop = true;
            src.volume = flyVolume;
            src.time = Random.value * flyLoopClip.length;   // desfasa el aleteo entre enemigos
            src.Play();
        }
    }

    void Update()
    {
        src.mute = !AudioManager.SfxOn;
    }

    public void PlayAttack()
    {
        if (dead || !attackClip || !AudioManager.SfxOn) return;
        src.PlayOneShot(attackClip, attackVolume);
    }

    public void PlayDeath()
    {
        if (dead) return;
        dead = true;

        if (src.loop) { src.Stop(); src.loop = false; }   // corta el vuelo
        if (deathClip && AudioManager.SfxOn)
            src.PlayOneShot(deathClip, deathVolume);
    }
}