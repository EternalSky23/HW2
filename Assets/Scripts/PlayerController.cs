using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Camera")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;
    private float verticalSpeed;
    private float turnSmoothVelocity;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        bool grounded = controller.isGrounded;
        if (grounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f,
            Input.GetAxisRaw("Vertical")).normalized;
        Vector3 horizontalMotion = Vector3.zero;

        if (input.sqrMagnitude > 0.01f)
        {
            float cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            float targetYaw = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + cameraYaw;
            float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw,
                ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            horizontalMotion = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward * moveSpeed;
        }

        if (grounded && Input.GetButtonDown("Jump"))
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalSpeed += gravity * Time.deltaTime;
        CollisionFlags collisions = controller.Move(
            (horizontalMotion + Vector3.up * verticalSpeed) * Time.deltaTime);
        grounded = (collisions & CollisionFlags.Below) != 0;

        if (animator != null)
        {
            animator.SetFloat(SpeedId, input.magnitude, 0.12f, Time.deltaTime);
            animator.SetBool(GroundedId, grounded);
            animator.SetFloat(VerticalSpeedId, verticalSpeed);
        }
    }
}
