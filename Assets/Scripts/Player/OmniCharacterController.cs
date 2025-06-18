using UnityEngine;

public class OmniCharacterController : BaseCharacterController
{
    [Header("Reeling")]
    public float ReelInSpeed = 25f;
    public float ReelOutSpeed = 15f;
    public float BurstReelInSpeed = 8f;

    [Header("Swinging")]
    public float RedirectSpeed = 12f;
    
    [Header("Burst")]
    public float BurstSpeed = 50f;
    public float BurstAcceleration = 10f;
    public float SustainedBurstDuration = 0.4f;

    [Header("Misc")]
    public Vector3 Gravity = new Vector3(0, -25f, 0);
    public float OrientationSharpness = 12;

    private Vector3 _hookPoint;
    private bool _isReeling = false;
    private float _ropeDistance; // The current length of the rope tether
    private Vector3 _internalVelocityAdd = Vector3.zero; // Stores velocity to be added
    
    // Burst state
    private bool _isBursting = false;
    private bool _burstDown = false;
    private float _burstSustainTime = 0f;

    // Inputs
    private bool _jumpHold;
    private bool _reelOutHold;
    private bool _crouchHold;
    private float _moveAxisRight;
    private bool _dashHold;
    
    public override void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        base.SetInputs(ref inputs);

        _jumpHold = inputs.JumpHold;
        _reelOutHold = inputs.ReelOutHold;
        _crouchHold = inputs.CrouchHold;
        _moveAxisRight = inputs.MoveAxisRight;
        _dashHold = inputs.DashHold;
        
        // Handle Burst inputs
        if (inputs.DashDown && !_isBursting)
        {
            _isBursting = true;
            _burstSustainTime = Time.time;
        }
    }
    
    public override void AddVelocity(Vector3 velocity)
    {
        // Store the velocity to be applied in the next update
        _internalVelocityAdd += velocity;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Apply any externally added velocity (from the state transition)
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }

        if (!_isReeling)
        {
            currentVelocity += Gravity * deltaTime; // Apply gravity if not hooked
            return;
        }

        // Properties of the tether
        Vector3 vectorToHook = _hookPoint - Motor.TransientPosition;
        float distanceToHook = vectorToHook.magnitude;

        // Auto-shorten the rope if the player moves closer to the hook point
        _ropeDistance = Mathf.Min(_ropeDistance, distanceToHook);

        // Player input for side-to-side swinging
        Vector3 sideInputVelocity = Motor.transform.right * (_moveAxisRight * RedirectSpeed);

        if (_isBursting)
        {
            if (_burstDown)
            {
                _burstDown = false;
                currentVelocity += Camera.main.transform.forward * BurstSpeed;
            }
            currentVelocity += Camera.main.transform.forward * BurstAcceleration;

            // Stop bursting if the button is released or the duration expires
            if ((!_dashHold && Time.time - _burstSustainTime > 0.1f) || Time.time - _burstSustainTime >= SustainedBurstDuration)
            {
                _isBursting = false;
            }

            // The rope must still act as a constraint to maintain the swing's arc.
            float futureDistance = Vector3.Distance(Motor.TransientPosition + currentVelocity * deltaTime, _hookPoint);
            if (futureDistance > _ropeDistance)
            {
                Vector3 radialDir = vectorToHook.normalized;
                currentVelocity = Vector3.ProjectOnPlane(currentVelocity, radialDir);
            }
        }
        else if (_jumpHold) // Reeling IN
        {
            // No gravity while actively reeling in
            Vector3 targetVelocity = vectorToHook.normalized * ReelInSpeed;
            targetVelocity += sideInputVelocity; // Allow strafing while reeling

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-10 * deltaTime));
        }
        else // Swinging or Hanging
        {
            // Apply gravity to create a natural swinging arc
            currentVelocity += Gravity * deltaTime;

            // Add player-controlled side velocity
            currentVelocity += sideInputVelocity * deltaTime;

            if (_reelOutHold)
            {
                _ropeDistance += ReelOutSpeed * deltaTime;
            }

            // Check if the next position would exceed the rope's length
            float futureDistance = Vector3.Distance(Motor.TransientPosition + currentVelocity * deltaTime, _hookPoint);
            if (futureDistance > _ropeDistance)
            {
                // If the rope is taut, project the velocity onto the tangent of the sphere.
                // This removes any velocity component that would stretch the rope further,
                // creating a swinging motion.
                Vector3 radialDir = vectorToHook.normalized;
                currentVelocity = Vector3.ProjectOnPlane(currentVelocity, radialDir);
            }
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
        {
            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
        }
    }

    public void StartReeling(Vector3 hookPoint)
    {
        _isReeling = true;
        _hookPoint = hookPoint;
        // Initialize the rope's starting length
        _ropeDistance = Vector3.Distance(Motor.TransientPosition, hookPoint);
    }

    public void StopReeling()
    {
        _isReeling = false;
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
        base.AfterCharacterUpdate(deltaTime);
        if (_isReeling || _isBursting)
        {
            Motor.ForceUnground();
        }
    }
}