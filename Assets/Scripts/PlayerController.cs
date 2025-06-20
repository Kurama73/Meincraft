using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float sprintMultiplier = 1.5f;
    public float jumpForce = 1.25f;
    public float gravity = -20f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 0.1f;

    [Header("Interaction Settings")]
    public float reachDistance = 5f;

    private CharacterController controller;
    private Camera playerCamera;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private Vector3 velocity;
    private float verticalRotation = 0f;
    private bool jumpPressed = false;
    private bool isSprinting = false;
    private bool isFlying = false;
    private float flySpeed = 8f;

    private float lastJumpTime = 0f;
    private float doubleTapDelay = 0.3f;


    private bool wasGroundedLastFrame = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>() * mouseSensitivity;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            float time = Time.time;
            if (time - lastJumpTime < doubleTapDelay)
            {
                isFlying = !isFlying;
                velocity.y = 0f; // reset vertical velocity
            }
            else
            {
                jumpPressed = true;
            }

            lastJumpTime = time;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            BreakBlock();
        }
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        transform.Rotate(Vector3.up * lookInput.x);

        verticalRotation -= lookInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        if (!controller.enabled) return; // 🛑 Empêche les erreurs

        bool isGrounded = controller.isGrounded;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (isFlying)
        {
            if (Keyboard.current.eKey.isPressed) move += Vector3.up;
            if (Keyboard.current.qKey.isPressed) move += Vector3.down;

            float flyFinalSpeed = isSprinting ? flySpeed * 5f : flySpeed;
            controller.Move(move * flyFinalSpeed * Time.deltaTime);
            velocity = Vector3.zero;
        }
        else
        {
            float finalSpeed = isSprinting ? speed * sprintMultiplier : speed;

            if (isGrounded && velocity.y < 0) velocity.y = -1f;
            controller.Move(move * finalSpeed * Time.deltaTime);

            if (jumpPressed && isGrounded)
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            jumpPressed = false;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        wasGroundedLastFrame = isGrounded;
    }




    public void TeleportTo(Vector3 position)
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }
    }


    void BreakBlock()
    {
        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * 0.3f; // Avance le point d'origine
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, reachDistance, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
        {
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            if (chunk != null)
            {
                Vector3 hitPosition = hit.point - hit.normal * 0.5f;
                chunk.SetBlock(hitPosition, BlockType.air);
            }
        }
    }



}
