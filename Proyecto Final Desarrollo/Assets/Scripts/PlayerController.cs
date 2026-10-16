using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 6f;
    public float turnSpeed = 720f;
    public float gravity = -25f;

    [Header("Ataques (tiempo que bloquean el movimiento)")]
    public float attackLockTime = 0.5f;
    public float superLockTime = 1.0f;

    [Header("Referencias")]
    public Animator animator;
    public Transform cameraTransform;

    public Vector2 MoveInput { get; set; }
    public Vector3 AimDirection { get; set; }
    public bool IsAiming { get; set; }
    public bool IsBusy => Time.time < lockUntil;

    CharacterController cc;
    Vector3 startPos;
    float verticalVel, lockUntil;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int SuperHash = Animator.StringToHash("Super");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        startPos = transform.position;
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    public void Attack() { DoAction(AttackHash, attackLockTime); }
    public void SuperAttack() { DoAction(SuperHash, superLockTime); }

    void DoAction(int trigger, float lockTime)
    {
        if (IsBusy) return;
        lockUntil = Time.time + lockTime;
        if (animator) animator.SetTrigger(trigger);
    }

    void Update()
    {
        Vector2 input = IsBusy ? Vector2.zero : MoveInput;   // no camina mientras ataca

        Vector3 fwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        fwd.y = 0; right.y = 0; fwd.Normalize(); right.Normalize();

        Vector3 dir = fwd * input.y + right * input.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (cc.isGrounded && verticalVel < 0) verticalVel = -2f;
        verticalVel += gravity * Time.deltaTime;
        cc.Move((dir * moveSpeed + Vector3.up * verticalVel) * Time.deltaTime);

        Vector3 face = (IsAiming && AimDirection.sqrMagnitude > 0.01f) ? AimDirection : dir;
        if (face.sqrMagnitude > 0.01f && !IsBusy)
        {
            Quaternion t = Quaternion.LookRotation(face.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, t, turnSpeed * Time.deltaTime);
        }

        if (animator)
            animator.SetFloat(SpeedHash, dir.magnitude * moveSpeed, 0.1f, Time.deltaTime);

        if (transform.position.y < -20f)
        {
            cc.enabled = false; transform.position = startPos; cc.enabled = true; verticalVel = 0;
        }
    }
}