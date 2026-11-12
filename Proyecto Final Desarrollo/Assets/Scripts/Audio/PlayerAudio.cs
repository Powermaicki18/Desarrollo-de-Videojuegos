using UnityEngine;

/// <summary>
/// Sonidos de Tuko: ataque + grito (a la vez), súper, caminata y muerte.
/// Ponlo en el mismo objeto que PlayerController y Health.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip attackClip;
    public AudioClip screamClip;      // suena junto al ataque simple
    public AudioClip superClip;
    public AudioClip deathClip;
    public AudioClip footstepClip;

    [Header("Volumen")]
    [Range(0f, 1f)] public float attackVolume = 1f;
    [Range(0f, 1f)] public float screamVolume = 1f;
    [Range(0f, 1f)] public float superVolume = 1f;
    [Range(0f, 1f)] public float deathVolume = 1f;
    [Range(0f, 1f)] public float footstepVolume = 0.6f;

    [Header("Pasos")]
    [Tooltip("0 = el clip de caminata se repite en bucle mientras te mueves. >0 = suena una vez cada X segundos (si tu clip es UN solo paso)")]
    public float stepInterval = 0f;
    public float minSpeedForSteps = 0.5f;

    CharacterController cc;
    Health health;
    AudioSource footSource;
    float nextStep;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = GetComponent<Health>();

        footSource = gameObject.AddComponent<AudioSource>();
        footSource.playOnAwake = false;
        footSource.spatialBlend = 0f;
        footSource.loop = stepInterval <= 0f;
        footSource.clip = footstepClip;
        footSource.volume = footstepVolume;
    }

    void OnEnable()
    {
        if (health) health.onDeath.AddListener(OnDeath);
    }

    void OnDisable()
    {
        if (health) health.onDeath.RemoveListener(OnDeath);
    }

    public void PlayAttack()
    {
        AudioManager.Sfx(attackClip, attackVolume);
        AudioManager.Sfx(screamClip, screamVolume);   // al mismo tiempo
    }

    public void PlaySuper()
    {
        AudioManager.Sfx(superClip, superVolume);
    }

    void OnDeath()
    {
        footSource.Stop();
        AudioManager.Sfx(deathClip, deathVolume);
    }

    void Update()
    {
        if (!footstepClip) return;

        footSource.mute = !AudioManager.SfxOn;

        Vector3 v = cc.velocity; v.y = 0f;
        bool moving = v.magnitude > minSpeedForSteps && cc.isGrounded
                      && !(health && health.IsDead) && Time.timeScale > 0f;

        if (stepInterval <= 0f)
        {
            if (moving && !footSource.isPlaying) footSource.Play();
            else if (!moving && footSource.isPlaying) footSource.Stop();
        }
        else if (moving && Time.time >= nextStep)
        {
            nextStep = Time.time + stepInterval;
            if (AudioManager.SfxOn) footSource.PlayOneShot(footstepClip, footstepVolume);
        }
    }
}