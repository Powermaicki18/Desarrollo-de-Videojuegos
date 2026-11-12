using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón del menú de pausa para activar/desactivar la música o los efectos.
/// Ponlo en el Button y elige el tipo. Opcional: arrastra un objeto "offMark"
/// (por ejemplo una raya o una X) que se muestra cuando está desactivado.
/// </summary>
[RequireComponent(typeof(Button))]
public class AudioToggleButton : MonoBehaviour
{
    public enum Kind { Music, Sfx }

    public Kind kind = Kind.Music;
    [Tooltip("Se activa cuando el audio está DESACTIVADO (raya, X, icono tachado...)")]
    public GameObject offMark;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        AudioManager.OnSettingsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        AudioManager.OnSettingsChanged -= Refresh;
    }

    void OnClick()
    {
        var am = AudioManager.Instance;
        if (!am) return;
        if (kind == Kind.Music) am.ToggleMusic();
        else am.ToggleSfx();
    }

    void Refresh()
    {
        bool on = kind == Kind.Music ? AudioManager.MusicOn : AudioManager.SfxOn;
        if (offMark) offMark.SetActive(!on);
    }
}