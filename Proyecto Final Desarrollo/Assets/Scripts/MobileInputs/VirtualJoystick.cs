using UnityEngine;
using UnityEngine.EventSystems;

// Joystick flotante: aparece donde pones el dedo dentro de su zona y vuelve a su sitio al soltar
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform baseRect;
    public RectTransform knobRect;
    public CanvasGroup baseGroup;
    public float radius = 120f;
    public float deadZone = 0.15f;
    public Vector2 restOffsetFromBottomLeft = new Vector2(280f, 260f);
    public float idleAlpha = 0.45f;

    public Vector2 Value { get; private set; }

    RectTransform area;
    int pointerId;
    bool active;

    void Awake() { area = (RectTransform)transform; }

    void LateUpdate()
    {
        if (!active)   // en reposo se queda en su posición fija
        {
            baseRect.anchoredPosition = new Vector2(area.rect.xMin + restOffsetFromBottomLeft.x,
                                                    area.rect.yMin + restOffsetFromBottomLeft.y);
            baseGroup.alpha = idleAlpha;
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (active) return;
        active = true;
        pointerId = e.pointerId;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 local);
        local.x = Mathf.Clamp(local.x, area.rect.xMin + radius, area.rect.xMax - radius);
        local.y = Mathf.Clamp(local.y, area.rect.yMin + radius, area.rect.yMax - radius);
        baseRect.anchoredPosition = local;
        baseGroup.alpha = 1f;
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!active || e.pointerId != pointerId) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, e.pressEventCamera, out Vector2 p);
        Vector2 off = Vector2.ClampMagnitude(p, radius);
        knobRect.anchoredPosition = off;

        Vector2 v = off / radius;
        float mag = v.magnitude;
        v = mag < deadZone ? Vector2.zero : v.normalized * ((mag - deadZone) / (1f - deadZone));
        Value = v;
        MobileInput.Move = v;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != pointerId) return;
        Release();
    }

    void Release()
    {
        active = false;
        Value = Vector2.zero;
        MobileInput.Move = Vector2.zero;
        if (knobRect) knobRect.anchoredPosition = Vector2.zero;
    }

    void OnDisable() { Release(); }
}