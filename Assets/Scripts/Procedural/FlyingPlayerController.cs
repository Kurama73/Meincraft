using UnityEngine;
using UnityEngine.InputSystem;

public class FlyingPlayerController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySpeed = 10f;
    public float boostMultiplier = 2f;
    public float rotationSpeed = 2f;
    public float ascendSpeed = 8f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;

    [Header("Camera")]
    public Transform cameraTransform;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isFlying = true;
    private bool isBoosting = false;
    private float verticalInput = 0f;

    private float verticalRotation = 0f;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (isFlying)
        {
            HandleFlightMovement();
            HandleMouseLook();
        }
    }

    void HandleFlightMovement()
    {
        // Mouvement horizontal
        Vector3 movement = Vector3.zero;
        movement += transform.right * moveInput.x;
        movement += transform.forward * moveInput.y;

        // Mouvement vertical
        movement += transform.up * verticalInput;

        // Appliquer la vitesse
        float currentSpeed = isBoosting ? flySpeed * boostMultiplier : flySpeed;
        transform.position += movement * currentSpeed * Time.deltaTime;
    }

    void HandleMouseLook()
    {
        // Rotation horizontale
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        // Rotation verticale
        verticalRotation -= lookInput.y * mouseSensitivity * (invertY ? -1 : 1);
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    // Input System callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnAscend(InputAction.CallbackContext context)
    {
        if (context.performed)
            verticalInput = 1f;
        else if (context.canceled)
            verticalInput = 0f;
    }

    public void OnDescend(InputAction.CallbackContext context)
    {
        if (context.performed)
            verticalInput = -1f;
        else if (context.canceled)
            verticalInput = 0f;
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        isBoosting = context.ReadValueAsButton();
    }

    public void OnToggleFlying(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isFlying = !isFlying;

            // Activer/désactiver la physique selon le mode
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = !isFlying;
                rb.isKinematic = isFlying;
            }
        }
    }
}
