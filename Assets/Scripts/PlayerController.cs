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
            jumpPressed = true;
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
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -1f; // Assure qu'on reste bien collé au sol
        }

        // Mouvement horizontal
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        float finalSpeed = isSprinting ? speed * sprintMultiplier : speed;
        controller.Move(move * finalSpeed * Time.deltaTime);

        // Saut
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        jumpPressed = false;

        // Gravité
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

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
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, reachDistance))
        {
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            if (chunk != null)
            {
                Vector3 hitPosition = hit.point - hit.normal * 0.5f; // Ajustez la position pour cibler le centre du bloc
                chunk.SetBlock(hitPosition, BlockType.air);
            }
        }
    }

}
