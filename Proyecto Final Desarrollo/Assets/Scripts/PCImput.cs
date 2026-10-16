using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerController))]
public class PCInput : MonoBehaviour
{
    public bool aimWithMouse = false;          // mira al mouse todo el tiempo
    public UnityEvent onFire;
    public UnityEvent onSuper;
    public Vector3 AimPoint { get; private set; }

    PlayerController pc;
    Camera cam;

    void Awake() { pc = GetComponent<PlayerController>(); cam = Camera.main; }

    void Update()
    {
        Vector2 move = Vector2.zero, mouse = Vector2.zero;
        bool firePressed = false, fireHeld = false, superPressed = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var ms = Mouse.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1;
            superPressed = kb.spaceKey.wasPressedThisFrame;
        }
        if (ms != null)
        {
            mouse = ms.position.ReadValue();
            firePressed = ms.leftButton.wasPressedThisFrame;
            fireHeld = ms.leftButton.isPressed;
            if (ms.rightButton.wasPressedThisFrame) superPressed = true;
        }
#else
        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        mouse = Input.mousePosition;
        firePressed = Input.GetMouseButtonDown(0);
        fireHeld = Input.GetMouseButton(0);
        superPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space);
#endif

        pc.MoveInput = move.sqrMagnitude > 1f ? move.normalized : move;

        if (cam)
        {
            Ray ray = cam.ScreenPointToRay(mouse);
            Plane ground = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (ground.Raycast(ray, out float d))
            {
                AimPoint = ray.GetPoint(d);
                Vector3 dir = AimPoint - transform.position; dir.y = 0;
                if (dir.sqrMagnitude > 0.04f) pc.AimDirection = dir.normalized;
            }
        }
        pc.IsAiming = aimWithMouse || fireHeld;

        if (firePressed) { pc.Attack(); onFire?.Invoke(); }
        if (superPressed) { pc.SuperAttack(); onSuper?.Invoke(); }
    }
}