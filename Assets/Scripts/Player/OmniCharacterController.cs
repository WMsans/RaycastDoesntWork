using UnityEngine;

public class OmniCharacterController : BaseCharacterController
{
    [Header("Reeling")]
    public float ReelInSpeed = 25f;
    public float ReelOutSpeed = 15f;

    [Header("Burst")]
    public float BurstSpeed = 50f;
    public float SustainedBurstDuration = 0.4f;

    [Header("Redirect")]
    public float RedirectSpeed = 12f;

    [Header("Misc")]
    public Vector3 Gravity = new Vector3(0, -25f, 0);
    public float OrientationSharpness = 12;

    private Vector3 _hookPoint;
    private bool _isReeling = false;

    // Burst related
    private float _lastJumpRequestTime = -1f;
    private bool _isBursting = false;
    private float _burstSustainTime = 0f;

    // Inputs
    private bool _jumpHold;
    private bool _jumpDown;
    private bool _reelOutHold;
    private bool _crouchHold;
    private float _moveAxisRight;

    public override void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        base.SetInputs(ref inputs);

        _jumpDown = inputs.JumpDown;
        _jumpHold = inputs.JumpHold;
        _reelOutHold = inputs.ReelOutHold;
        _crouchHold = inputs.CrouchHold;
        _moveAxisRight = inputs.MoveAxisRight;

        if (_jumpDown && Time.time - _lastJumpRequestTime < 0.25f && !_isReeling)
        {
            _isBursting = true;
            _burstSustainTime = Time.time;
        }
        _lastJumpRequestTime = Time.time;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_isBursting)
        {
            currentVelocity = _lookInputVector * BurstSpeed;

            if ((!_jumpHold && Time.time - _burstSustainTime > 0.1f) || Time.time - _burstSustainTime >= SustainedBurstDuration)
            {
                _isBursting = false;
            }
            Debug.Log("Burst: " + (Time.time - _burstSustainTime));
        }
        else if (_isReeling)
        {
            Vector3 directionToHook = (_hookPoint - Motor.TransientPosition).normalized;
            Vector3 targetVelocity = Vector3.zero;

            if (_jumpHold)
            {
                targetVelocity = directionToHook * ReelInSpeed;
            }
            else if (_reelOutHold)
            {
                targetVelocity = -directionToHook * ReelOutSpeed;
            }

            // Redirection
            Vector3 right = Motor.transform.right;
            targetVelocity += right * (_moveAxisRight * RedirectSpeed);

            if (_jumpHold && !_reelOutHold)
            {
                targetVelocity += Motor.CharacterUp * RedirectSpeed;
            }
            else if (_crouchHold)
            {
                targetVelocity -= Motor.CharacterUp * RedirectSpeed;
            }

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Mathf.Exp(-10 * deltaTime));
        }
        else
        {
            currentVelocity += Gravity * deltaTime;
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
    }

    public void StopReeling()
    {
        _isReeling = false;
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
        base.AfterCharacterUpdate(deltaTime);
        if (_isBursting || _isReeling)
        {
            Motor.ForceUnground();
        }
    }
}