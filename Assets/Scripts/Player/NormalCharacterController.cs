using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;

public class NormalCharacterController : BaseCharacterController
{
    [Header("Stable Movement")]
    public float MaxStableMoveSpeed = 10f;
    public float StableMovementSharpness = 15;
    public float OrientationSharpness = 10;

    [Header("Air Movement")]
    public float MaxAirMoveSpeed = 10f;
    public float AirAccelerationSpeed = 5f;
    public float Drag = 0.1f;

    [Header("Jumping")]
    public bool AllowJumpingWhenSliding = false;
    public bool AllowWallJump = false;
    public float JumpSpeed = 10f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;

    [Header("Burst")]
    public float BurstSpeed = 20f;
    public float SustainedBurstDuration = 0.4f; // How long the burst can be sustained
    private float _lastJumpRequestTime = -1f;
    
    // Burst state variables
    private bool _isBursting = false;
    private float _burstSustainTime = 0f;

    [Header("Misc")]
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public bool OrientTowardsGravity = true;
    public Transform MeshRoot;

    private Collider[] _probedColliders = new Collider[8];
    private bool _jumpRequested = false;
    private bool _jumpConsumed = false;
    private bool _jumpedThisFrame = false;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump = 0f;
    private bool _doubleJumpConsumed = false;
    private bool _canWallJump = false;
    private Vector3 _wallJumpNormal;
    private Vector3 _internalVelocityAdd = Vector3.zero;
    private bool _shouldBeCrouching = false;
    private bool _isCrouching = false;

    // Input state
    private bool _jumpHold = false;

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>
    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
        }

        if (OrientTowardsGravity)
        {
            // Rotate from current up to invert gravity
            currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>
    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Handle Sustained Burst
        if (_isBursting)
        {
            // Set velocity to burst in the look direction
            currentVelocity = Camera.main.transform.forward * BurstSpeed;

            // Stop bursting if the button is released or the duration expires
            if ((!_jumpHold && Time.time - _burstSustainTime > 0.1f) || Time.time - _burstSustainTime >= SustainedBurstDuration)
            {
                _isBursting = false;
            }

            // If we are bursting, skip the rest of the velocity logic for this frame
            return;
        }
        
        var targetMovementVelocity = Vector3.zero;
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            // Reorient velocity on slope
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        else
        {
            // Add move input
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                targetMovementVelocity = _moveInputVector * Mathf.Max(MaxAirMoveSpeed, currentVelocity.magnitude);

                // Prevent climbing on un-stable slopes with air movement
                if (Motor.GroundingStatus.FoundAnyGround)
                {
                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                }

                Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
            }

            // Gravity
            currentVelocity += Gravity * deltaTime;

            // Drag
            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        }

        // Handle jumping
        {
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested)
            {
                // See if we actually are allowed to jump
                if (_canWallJump ||
                    (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
                {
                    // Calculate jump direction before ungrounding
                    Vector3 jumpDirection = Motor.CharacterUp;
                    if (_canWallJump)
                    {
                        jumpDirection = _wallJumpNormal;
                    }
                    else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }

                    // Makes the character skip ground probing/snapping on its next update. 
                    Motor.ForceUnground(0.1f);

                    // Add to the return velocity and reset jump state
                    currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                    _jumpRequested = false;
                    _jumpConsumed = true;
                    _jumpedThisFrame = true;
                }
            }

            // Reset wall jump
            _canWallJump = false;
        }

        // Take into account additive velocity
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public override void AfterCharacterUpdate(float deltaTime)
    {
        // Make sure we stay ungrounded while bursting
        if (_isBursting)
        {
            Motor.ForceUnground();
        }

        // Handle jump-related values
        {
            // Handle jumping pre-ground grace period
            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
            {
                _jumpRequested = false;
            }

            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
            {
                // If we're on a ground surface, reset jumping values
                if (!_jumpedThisFrame)
                {
                    _doubleJumpConsumed = false;
                    _jumpConsumed = false;
                }
                _timeSinceLastAbleToJump = 0f;
            }
            else
            {
                // Keep track of time since we were last able to jump (for grace period)
                _timeSinceLastAbleToJump += deltaTime;
            }
        }

        // Handle uncrouching
        if (_isCrouching && !_shouldBeCrouching)
        {
            // Do an overlap test with the character's standing height to see if there are any obstructions
            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
            if (Motor.CharacterOverlap(
                Motor.TransientPosition,
                Motor.TransientRotation,
                _probedColliders,
                Motor.CollidableLayers,
                QueryTriggerInteraction.Ignore) > 0)
            {
                // If obstructions, just stick to crouching dimensions
                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
            }
            else
            {
                // If no obstructions, uncrouch
                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                _isCrouching = false;
            }
        }
    }

    public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        // We can wall jump only if we are not stable on ground and are moving against an obstruction
        if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
        {
            _canWallJump = true;
            _wallJumpNormal = hitNormal;
        }
    }

    public override void AddVelocity(Vector3 velocity)
    {
        _internalVelocityAdd += velocity;
    }

    public override void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        base.SetInputs(ref inputs);
        
        // Store jump hold state
        _jumpHold = inputs.JumpHold;
        
        // Handle Jump and Burst inputs
        if (inputs.JumpDown)
        {
            // Check for double-tap to trigger burst
            if(Time.time - _lastJumpRequestTime < 0.25f)
            {
                _isBursting = true;
                _burstSustainTime = Time.time;
                _jumpRequested = false; // Cancel any pending jump request
            }
            else
            {
                // Regular jump
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }
            // Record the time of this jump press for double-tap detection
            _lastJumpRequestTime = Time.time;
        }

        // Crouching input
        if (inputs.CrouchDown)
        {
            _shouldBeCrouching = true;

            if (!_isCrouching)
            {
                _isCrouching = true;
                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
            }
        }
        else if (inputs.CrouchUp)
        {
            _shouldBeCrouching = false;
        }
    }
}