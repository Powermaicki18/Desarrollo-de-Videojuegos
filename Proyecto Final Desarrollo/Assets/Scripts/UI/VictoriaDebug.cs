
using UnityEngine;
using UnityEngine.InputSystem;

public class VictoriaDebug : MonoBehaviour
{
    [Header("Sistema de victoria")]
    [SerializeField] private VictoriaController victoriaController;

    [Header("Configuracion de prueba")]
    [SerializeField] private Key teclaVictoria = Key.P;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        // Comprobar que exista un teclado
        if (Keyboard.current == null)
            return;

        // Detectar cuando se presiona la tecla P
        if (Keyboard.current[teclaVictoria].wasPressedThisFrame)
        {
            if (victoriaController != null)
            {
                Debug.Log(
                    "DEBUG: Victoria activada con la tecla P"
                );

                victoriaController.MostrarVictoria();
            }
            else
            {
                Debug.LogWarning(
                    "No se ha conectado VictoriaController."
                );
            }
        }

#endif
    }
}
