using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlayerController))]
public class PCInput : MonoBehaviour
{
    public bool aimWithMouse = false;
    public UnityEvent onFire;
    public UnityEvent onSuper;
    public Vector3 AimPoint { get; private set; }

    PlayerController pc;
    Camera cam;

    void Awake() { pc = GetComponent<PlayerController>(); cam = Camera.main; }

    void Update()
    {
        Vector2 move = Vector2.zero, mouse = Vector2.zero;
        bool mouseFire = false, mouseSuper = false, keySuper = false;
        bool useMouse = !Application.isMobilePlatform;   // en el celular el mouse no existe

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var ms = Mouse.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1;
            keySuper = kb.spaceKey.wasPressedThisFrame;
        }
        if (useMouse && ms != null)
        {
            mouse = ms.position.ReadValue();
            mouseFire = ms.leftButton.wasPressedThisFrame;
            mouseSuper = ms.rightButton.wasPressedThisFrame;
        }
#else
        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        keySuper = Input.GetKeyDown(KeyCode.Space);
        if (useMouse)
        {
            mouse = Input.mousePosition;
            mouseFire = Input.GetMouseButtonDown(0);
            mouseSuper = Input.GetMouseButtonDown(1);
        }
#endif

        // El joystick virtual manda sobre el teclado cuando se está usando
        if (MobileInput.Move.sqrMagnitude > 0.0001f) move = MobileInput.Move;
        pc.MoveInput = move.sqrMagnitude > 1f ? move.normalized : move;

        // Un clic sobre un botón o joystick de la interfaz no cuenta como golpe con el mouse
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (overUI) { mouseFire = false; mouseSuper = false; }

        if (useMouse && cam)
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
        pc.IsAiming = aimWithMouse && useMouse;

        if (mouseFire) { pc.Attack(); onFire?.Invoke(); }
        if (mouseSuper || keySuper) { pc.SuperAttack(); onSuper?.Invoke(); }
    }
}