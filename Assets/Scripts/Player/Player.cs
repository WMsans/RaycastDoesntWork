using UnityEngine;
using UnityEngine.InputSystem; // Import the new Input System namespace
using KinematicCharacterController.Examples;

public class Player : MonoBehaviour
{
    public ExampleCharacterCamera OrbitCamera;
    public Transform CameraFollowPoint;
    public CharacterControllerStateMachine Character;
    public HookController HookController;

    private InputSystem_Actions inputActions; // Reference to the generated input actions class

    // Variables to hold input values
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float scrollInput;
    

    private void Awake()
    {
        // Instantiate the input actions class
        inputActions = new InputSystem_Actions();

        // --- Set up Player Action Map Callbacks ---

        // Read value for Move action (continuous)
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Read value for Look action (continuous)
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // --- NOTE: You will need to create a "Zoom" action for the scroll wheel ---
        // For example, create a new Action named "Zoom" of type "Value" with a "Vector2" control type.
        // Then bind the "Scroll [Mouse]" path to it.
        // inputActions.Player.Zoom.performed += ctx => scrollInput = ctx.ReadValue<Vector2>().y;
        // inputActions.Player.Zoom.canceled += ctx => scrollInput = 0f;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Tell camera to follow transform
        OrbitCamera.SetFollowTransform(CameraFollowPoint);

        // Ignore the character's collider(s) for camera obstruction checks
        OrbitCamera.IgnoredColliders.Clear();
        OrbitCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
    }

    private void Update()
    {
        // Use the "Attack" action (typically bound to Left Mouse Button) to re-lock the cursor
        if (inputActions.Player.Attack.WasPressedThisFrame())
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        HandleCharacterInput();
    }

    private void LateUpdate()
    {
        HandleCameraInput();
    }

    private void HandleCameraInput()
    {
        // Create the look input vector for the camera from our stored input
        Vector3 lookInputVector = new Vector3(lookInput.x, lookInput.y, 0f);

        // Prevent moving the camera while the cursor isn't locked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            lookInputVector = Vector3.zero;
        }
        
        // --- NOTE: Camera Zooming ---
        // The original code used the scroll wheel. The line below is commented out
        // because your Action Map doesn't have a "Zoom" action yet. Once you add it
        // (as explained in Awake), you can uncomment this part.
        // float scrollThisFrame = scrollInput * 0.1f; // Adjust sensitivity as needed
        
        // Apply inputs to the camera
        // OrbitCamera.UpdateWithInput(Time.deltaTime, scrollThisFrame, lookInputVector);
        
        // Temporarily passing 0 for scroll until you add the action
        OrbitCamera.UpdateWithInput(Time.deltaTime, 0f, lookInputVector);

        // Handle toggling zoom level
        // --- NOTE: You need to decide which button toggles zoom. ---
        // The original used Right Mouse Button. You could bind this to the "Interact" action,
        // or create a new "ToggleZoom" action. Here's an example using "Interact":
        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
        }
    }

    private void HandleCharacterInput()
    {
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

        // Build the CharacterInputs struct from our stored and direct-read input values
        characterInputs.MoveAxisForward = moveInput.y;
        characterInputs.MoveAxisRight = moveInput.x;
        characterInputs.CameraRotation = OrbitCamera.Transform.rotation;
        
        // Read button presses directly in the frame they occur
        characterInputs.JumpDown = inputActions.Player.Jump.WasPressedThisFrame();
        characterInputs.JumpHold = inputActions.Player.Jump.IsPressed();
        characterInputs.CrouchDown = inputActions.Player.Crouch.WasPressedThisFrame();
        characterInputs.CrouchUp = inputActions.Player.Crouch.WasReleasedThisFrame();
        characterInputs.CrouchHold = inputActions.Player.Crouch.IsPressed();
        characterInputs.AttackDown = inputActions.Player.Attack.WasPressedThisFrame();
        characterInputs.AttackUp = inputActions.Player.Attack.WasReleasedThisFrame();
        characterInputs.AttackHold = inputActions.Player.Attack.IsPressed();
        characterInputs.ReelOutDown = inputActions.Player.Interact.WasPressedThisFrame();
        characterInputs.ReelOutUp = inputActions.Player.Interact.WasReleasedThisFrame();
        characterInputs.ReelOutHold = inputActions.Player.Interact.IsPressed();

        // Apply inputs to character
        Character.SetInputs(ref characterInputs);
        HookController.SetInputs(ref characterInputs);
    }

    // This struct remains unchanged
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool JumpHold;
        public bool CrouchDown;
        public bool CrouchUp;
        public bool CrouchHold;
        public bool AttackDown;
        public bool AttackUp;
        public bool AttackHold;
        public bool ReelOutDown;
        public bool ReelOutUp;
        public bool ReelOutHold;
    }
}