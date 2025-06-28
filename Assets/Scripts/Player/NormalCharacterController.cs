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

    [Header("Attacking")]
    public float AttackBurstSpeed = 20f;
    public float BurstGravityModifier = 0.4f;
    public float BurstGravityDuration = 0.6f;

    private bool _canAttackBurst = false;
    private float _burstGravityTimer = 0f;
    private bool _attackDown;

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
    private bool _canWallJump = false;
    private Vector3 _wallJumpNormal;
    
    private bool _shouldBeCrouching = false;
    private bool _isCrouching = false;

    public override void OnEnableController()
    {
        // Recharge burst when the controller is enabled
        _canAttackBurst = true;
    }

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
        base.UpdateVelocity(ref currentVelocity, deltaTime);
        Vector3 targetMovementVelocity;
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            // ⭐ REMOVED: All ground-sticking and slope-reorientation logic.
            // This prevents the character from being pushed down, allowing a burst to launch them off the ground.
            
            // Calculate target velocity as if on a flat plane
            targetMovementVelocity = _moveInputVector * MaxStableMoveSpeed;

            // Smoothly interpolate to target velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        else
        {
            // Air movement
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
                currentVelocity += velocityDiff * (AirAccelerationSpeed * deltaTime);
            }

            // Gravity
            if (_burstGravityTimer > 0f)
            {
                // Apply modified gravity
                currentVelocity += Gravity * BurstGravityModifier * deltaTime;
                _burstGravityTimer -= deltaTime;
            }
            else
            {
                // Apply regular gravity
                currentVelocity += Gravity * deltaTime;
            }


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
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public override void AfterCharacterUpdate(float deltaTime)
    {
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
                    _jumpConsumed = false;
                }
                _timeSinceLastAbleToJump = 0f;
                
                // Recharge attack burst on landing
                _canAttackBurst = true;
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

    public override void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        base.SetInputs(ref inputs);

        // Handle Jump inputs
        if (inputs.JumpDown)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
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

    public override void OnUseWeapon(Weapon weapon)
    {
        UseAttackBurst();
    }

    private void UseAttackBurst()
    {
        _burstGravityTimer = BurstGravityDuration;

        if (_canAttackBurst)
        {
            // Add an instant velocity burst
            this.SetVelocity(Camera.main.transform.forward * AttackBurstSpeed);
                
            // Consume the burst
            _canAttackBurst = false;
        }
        _attackDown = true;
        _jumpRequested = false;
    }
}