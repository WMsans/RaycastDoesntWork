using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float fastMoveSpeed = 15f;
    public float verticalSpeed = 3f;

    [Header("Look Settings")]
    public float lookSensitivity = 0.1f;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool moveUp;
    private bool moveDown;

    private float pitch = 0f;
    private float yaw = 0f;

    private void Awake()
    {
        // Initialize the Input System Actions
        inputActions = new InputSystem_Actions();

        // Register callbacks for player actions
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // Use the Jump action for moving up
        inputActions.Player.Jump.performed += ctx => moveUp = true;
        inputActions.Player.Jump.canceled += ctx => moveUp = false;

        // Use the Dash action for moving down
        inputActions.Player.Dash.performed += ctx => moveDown = true;
        inputActions.Player.Dash.canceled += ctx => moveDown = false;
        
        // Set initial camera rotation from its current orientation
        pitch = transform.eulerAngles.x;
        yaw = transform.eulerAngles.y;

        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
        HandleCursor();
    }

    private void HandleRotation()
    {
        // Update yaw and pitch based on mouse input
        yaw += lookInput.x * lookSensitivity;
        pitch -= lookInput.y * lookSensitivity;
        
        // Clamp the pitch to prevent the camera from flipping upside down
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // Apply the rotation to the camera
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        // Check if the "fast move" key (Left Shift) is held down
        bool isMovingFast = Keyboard.current.leftCtrlKey.isPressed;
        float currentSpeed = isMovingFast ? fastMoveSpeed : moveSpeed;

        // Calculate the horizontal movement direction
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        
        // Apply horizontal movement
        transform.position += moveDirection * (currentSpeed * Time.deltaTime);

        // Apply vertical movement
        if (moveUp)
        {
            transform.position += Vector3.up * (verticalSpeed * Time.deltaTime);
        }
        if (moveDown)
        {
            transform.position -= Vector3.up * (verticalSpeed * Time.deltaTime);
        }
    }

    private void HandleCursor()
    {
        // Press Escape to unlock the cursor
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // Click the left mouse button to re-lock the cursor
        else if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
             Cursor.lockState = CursorLockMode.Locked;
             Cursor.visible = false;
        }
    }
}