using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ponlo en cualquier Button: suena el clic de interfaz al presionarlo.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(AudioManager.PlayButton);
    }
}