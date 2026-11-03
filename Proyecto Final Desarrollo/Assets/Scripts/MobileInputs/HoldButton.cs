using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Botón táctil que sabe si está siendo mantenido
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform visual;          // se encoge un poco al pulsarlo
    public bool IsHeld { get; private set; }
    public Action onDown;

    float scale = 1f;

    public void OnPointerDown(PointerEventData e) { IsHeld = true; onDown?.Invoke(); }
    public void OnPointerUp(PointerEventData e) { IsHeld = false; }
    void OnDisable() { IsHeld = false; }

    void Update()
    {
        if (!visual) return;
        scale = Mathf.Lerp(scale, IsHeld ? 0.92f : 1f, 20f * Time.unscaledDeltaTime);
        visual.localScale = Vector3.one * scale;
    }
}