using UnityEngine;

public class OmniCharacterController : BaseCharacterController
{
    [Header("Reeling")]
    public float InitReelInSpeed = 50f; 
    public float EndReelInSpeed = 75f; 
    public float ReelInAccelerationTime = 1.2f; 
    public float ReelOutSpeed = 15f;

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
    private float _ropeDistance; 

    private float _reelInTime = 0f; 
    private float _initialReelSpeed; 

    private bool _isBursting = false;
    private bool _burstDown = false;
    private float _burstSustainTime = 0f;

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

        if (inputs.DashDown && !_isBursting)
        {
            _isBursting = true;
            _burstSustainTime = Time.time;
        }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        base.UpdateVelocity(ref currentVelocity, deltaTime);
        if (!_isReeling)
        {
            currentVelocity += Gravity * deltaTime; 
            return;
        }

        Vector3 vectorToHook = _hookPoint - Motor.TransientPosition;
        float distanceToHook = vectorToHook.magnitude;

        _ropeDistance = Mathf.Min(_ropeDistance, distanceToHook);

        Vector3 sideInputVelocity = Motor.transform.right * (_moveAxisRight * RedirectSpeed);

        if (_jumpHold) 
        {

            _reelInTime += deltaTime;

            float accelerationProgress = Mathf.Clamp01(_reelInTime / ReelInAccelerationTime);

            float currentReelInSpeed = Mathf.Lerp(_initialReelSpeed, EndReelInSpeed, accelerationProgress);

            Vector3 targetVelocity = vectorToHook.normalized * currentReelInSpeed;
            targetVelocity += sideInputVelocity; 

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-10 * deltaTime));
        }
        else 
        {
            _reelInTime = 0f; 

            currentVelocity += Gravity * deltaTime;

            currentVelocity += sideInputVelocity * deltaTime;

            if (_reelOutHold)
            {
                _ropeDistance += ReelOutSpeed * deltaTime;
            }

            float futureDistance = Vector3.Distance(Motor.TransientPosition + currentVelocity * deltaTime, _hookPoint);
            if (futureDistance > _ropeDistance)
            {

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
        _reelInTime = 0f; 

        Vector3 directionToHook = (hookPoint - Motor.TransientPosition).normalized;

        float projectedSpeed = Vector3.Dot(Motor.Velocity, directionToHook);

        _initialReelSpeed = Mathf.Max(projectedSpeed, InitReelInSpeed);

        _ropeDistance = Vector3.Distance(Motor.TransientPosition, hookPoint);
    }

    public void StopReeling()
    {
        _isReeling = false;
        _reelInTime = 0f; 
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