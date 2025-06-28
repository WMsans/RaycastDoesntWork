using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public abstract class BaseCharacterController : MonoBehaviour, ICharacterController
{
    protected KinematicCharacterMotor Motor;
    
    protected Vector3 _moveInputVector;
    protected Vector3 _lookInputVector;
    
    protected Vector3 _internalVelocityAdd = Vector3.zero;
    protected Vector3 _internalVelocityChange = Vector3.zero;

    public virtual void OnEnableController() {}
    public virtual void OnDisableController(){}
    
    // This new method allows external scripts to add velocity to the controller.
    public virtual void AddVelocity(Vector3 velocity)
    {
        _internalVelocityAdd += velocity;
    }

    public virtual void SetVelocity(Vector3 velocity)
    {
        _internalVelocityChange = velocity;
    }

    protected void HandleInternalVelocityChange(ref Vector3 currentVelocity)
    {
        // Take into account additive velocity
        if (_internalVelocityChange.sqrMagnitude > 0f)
        {
            currentVelocity = _internalVelocityChange;
            _internalVelocityChange = Vector3.zero;
        }
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }
    }
    
    public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        HandleInternalVelocityChange(ref currentVelocity);
    }

    public virtual void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public virtual void PostGroundingUpdate(float deltaTime)
    {
    }

    public virtual void AfterCharacterUpdate(float deltaTime)
    {
    }

    public virtual bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
    public virtual void OnUseWeapon(Weapon weapon){}
    public virtual void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        // Clamp input
        Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

        // Move and look inputs
        _moveInputVector = cameraPlanarRotation * moveInputVector;
        _lookInputVector = cameraPlanarDirection;
    }
    public void SetMotor(KinematicCharacterMotor motor) => Motor = motor;
}