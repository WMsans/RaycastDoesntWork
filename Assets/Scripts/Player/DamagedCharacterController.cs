using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class DamagedCharacterController : BaseCharacterController
{
    [SerializeField] private BaseCharacterController normalController;
    [SerializeField] private float stayTime;
    [Header("Stable Movement")]
    public float StableMovementSharpness = 15;
    public float OrientationSharpness = 10;
    [Header("Misc")]
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public bool OrientTowardsGravity = true;
    public Transform MeshRoot;
    
    private bool _decelerated = false;
    private Vector3 _internalVelocityAdd;
    public override void AddVelocity(Vector3 velocity)
    {
        _internalVelocityAdd += velocity;
    }
    public override void OnEnableController()
    {
        _decelerated = false;
        Timing.RunCoroutine(ExitStateCoroutine());
    }

    private IEnumerator<float> ExitStateCoroutine()
    {
        yield return Timing.WaitForSeconds(stayTime);
        CharacterControllerStateMachine.Instance.SetCharacterController(normalController);
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if(!_decelerated)
        {
            _decelerated = true;
            currentVelocity /= 2;
        }

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            // Reorient velocity on slope
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            var targetMovementVelocity = Vector3.zero;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        else
        {
            // Gravity
            currentVelocity += Gravity * deltaTime;
        }

        // Take into account additive velocity
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }
    }
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
}
