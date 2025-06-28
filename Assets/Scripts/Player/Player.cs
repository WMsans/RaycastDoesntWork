using UnityEngine;
using UnityEngine.InputSystem; // Import the new Input System namespace
using KinematicCharacterController.Examples;

public class Player : MonoBehaviour
{
    public RotatableCharacterCamera OrbitCamera;
    public Transform CameraFollowPoint;
    public CharacterControllerStateMachine Character;
    public HookController HookController;
    public WeaponController WeaponController; // Reference to the weapon controller

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
        
        OrbitCamera.UpdateWithInput(Time.deltaTime, lookInputVector);
    }

    private void HandleCharacterInput()
    {
        PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

        characterInputs.MoveAxisForward = moveInput.y;
        characterInputs.MoveAxisRight = moveInput.x;
        characterInputs.CameraRotation = OrbitCamera.Transform.rotation;
        
        characterInputs.JumpDown = inputActions.Player.Jump.WasPressedThisFrame();
        characterInputs.JumpHold = inputActions.Player.Jump.IsPressed();
        characterInputs.CrouchDown = inputActions.Player.Crouch.WasPressedThisFrame();
        characterInputs.CrouchUp = inputActions.Player.Crouch.WasReleasedThisFrame();
        characterInputs.CrouchHold = inputActions.Player.Crouch.IsPressed();
        
        // Primary Attack Inputs
        characterInputs.AttackDown = inputActions.Player.Attack.WasPressedThisFrame();
        characterInputs.AttackUp = inputActions.Player.Attack.WasReleasedThisFrame();
        characterInputs.AttackHold = inputActions.Player.Attack.IsPressed();
        
        characterInputs.HookDown = inputActions.Player.Hook.WasPressedThisFrame();
        characterInputs.HookUp = inputActions.Player.Hook.WasReleasedThisFrame();
        characterInputs.HookHold = inputActions.Player.Hook.IsPressed();
        characterInputs.ReelOutDown = inputActions.Player.Interact.WasPressedThisFrame();
        characterInputs.ReelOutUp = inputActions.Player.Interact.WasReleasedThisFrame();
        characterInputs.ReelOutHold = inputActions.Player.Interact.IsPressed();
        characterInputs.DashDown = inputActions.Player.Dash.WasPressedThisFrame();
        characterInputs.DashUp = inputActions.Player.Dash.WasReleasedThisFrame();
        characterInputs.DashHold = inputActions.Player.Dash.IsPressed();

        // Apply inputs to controllers
        Character?.SetInputs(ref characterInputs);
        HookController?.SetInputs(ref characterInputs);
        WeaponController?.SetInputs(ref characterInputs);
    }

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
        public bool HookDown;
        public bool HookUp;
        public bool HookHold;
        public bool ReelOutDown;
        public bool ReelOutUp;
        public bool ReelOutHold;
        public bool DashDown;
        public bool DashUp;
        public bool DashHold;
    }
}