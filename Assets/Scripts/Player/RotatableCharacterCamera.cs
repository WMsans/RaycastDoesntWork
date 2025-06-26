using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatableCharacterCamera : MonoBehaviour
{
    [Header("Framing")]
    public Camera Camera;
    public Vector2 FollowPointFraming = new Vector2(0f, 0f);
    public float FollowingSharpness = 10000f;

    [Header("Distance")]
    public float DefaultDistance = 6f;
    public float MinDistance = 0f;
    public float MaxDistance = 10f;
    public float DistanceMovementSpeed = 5f;
    public float DistanceMovementSharpness = 10f;

    [Header("Rotation")]
    public bool InvertX = false;
    public bool InvertY = false;
    [Range(-90f, 90f)]
    public float DefaultVerticalAngle = 20f;
    [Range(-90f, 90f)]
    public float MinVerticalAngle = -90f;
    [Range(-90f, 90f)]
    public float MaxVerticalAngle = 90f;
    public float RotationSpeed = 1f;
    public float RotationSharpness = 10000f;
    public bool RotateWithPhysicsMover = false;
    
    [Header("Roll")]
    [Tooltip("The maximum angle in degrees the camera can roll.")]
    [SerializeField] private float maxRollAngle = 30f;

    [Tooltip("How sensitive the roll is to the sideways velocity. Higher values mean more roll for the same speed.")]
    [SerializeField] private float rollSpeed = 5f;

    [Tooltip("How quickly the camera smoothly interpolates to the target roll angle. Higher values are faster.")]
    [SerializeField] private float smoothness = 7f;

    [Header("Obstruction")]
    public float ObstructionCheckRadius = 0.2f;
    public LayerMask ObstructionLayers = -1;
    public float ObstructionSharpness = 10000f;
    public List<Collider> IgnoredColliders = new List<Collider>();

    public Transform Transform { get; private set; }
    public Transform FollowTransform { get; private set; }

    public Vector3 PlanarDirection { get; set; }
    public float TargetDistance { get; set; }

    private bool _distanceIsObstructed;
    private float _currentDistance;
    private float _targetVerticalAngle;
    private RaycastHit _obstructionHit;
    private int _obstructionCount;
    private RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];
    private float _obstructionTime;
    private Vector3 _currentFollowPosition;

    // Fields for Camera Roll
    private float _currentRollAngle;
    private Vector3 _lastFollowPosition;

    private const int MaxObstructions = 32;

    void OnValidate()
    {
        DefaultDistance = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
        DefaultVerticalAngle = Mathf.Clamp(DefaultVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
    }

    void Awake()
    {
        Transform = this.transform;

        _currentDistance = DefaultDistance;
        TargetDistance = _currentDistance;
        _targetVerticalAngle = 0f;
        
        // Initialize roll state
        _currentRollAngle = 0f;
        _lastFollowPosition = Vector3.zero;

        PlanarDirection = Vector3.forward;
    }

    // Set the transform that the camera will orbit around
    public void SetFollowTransform(Transform t)
    {
        FollowTransform = t;
        PlanarDirection = FollowTransform.forward;
        _currentFollowPosition = FollowTransform.position;
        // Set initial position for velocity calculation
        _lastFollowPosition = FollowTransform.position;
    }

    public void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput)
    {
        if (FollowTransform)
        {
            // === CAMERA ROLL LOGIC START ===
            
            // Calculate FollowTransform's velocity
            Vector3 velocity = Vector3.zero;
            if (deltaTime > 0f)
            {
                velocity = (FollowTransform.position - _lastFollowPosition) / deltaTime;
            }
            _lastFollowPosition = FollowTransform.position;
            
            // Calculate the sideways speed relative to the camera's orientation on the character's horizontal plane
            Vector3 cameraPlanarRight = Vector3.Cross(FollowTransform.up, Vector3.ProjectOnPlane(Transform.forward, FollowTransform.up).normalized);
            float sidewaysSpeed = Vector3.Dot(velocity, cameraPlanarRight);

            // Calculate the desired roll angle based on the sideways speed
            float targetRollAngle = -sidewaysSpeed * rollSpeed; // Negative to roll in the direction of movement
            targetRollAngle = Mathf.Clamp(targetRollAngle, -maxRollAngle, maxRollAngle);

            // Smoothly interpolate the current roll angle towards the target
            _currentRollAngle = Mathf.Lerp(_currentRollAngle, targetRollAngle, 1f - Mathf.Exp(-smoothness * deltaTime));
            
            // === CAMERA ROLL LOGIC END ===

            if (InvertX)
            {
                rotationInput.x *= -1f;
            }
            if (InvertY)
            {
                rotationInput.y *= -1f;
            }

            // Process rotation input
            Quaternion rotationFromInput = Quaternion.Euler(FollowTransform.up * (rotationInput.x * RotationSpeed));
            PlanarDirection = rotationFromInput * PlanarDirection;
            PlanarDirection = Vector3.Cross(FollowTransform.up, Vector3.Cross(PlanarDirection, FollowTransform.up));
            Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, FollowTransform.up);

            _targetVerticalAngle -= (rotationInput.y * RotationSpeed);
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
            Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
            
            // Create the roll rotation
            Quaternion rollRot = Quaternion.Euler(0, 0, _currentRollAngle);
            
            // Combine all rotations (Yaw, Pitch, and Roll) and smoothly apply them
            Quaternion targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot * rollRot, 1f - Mathf.Exp(-RotationSharpness * deltaTime));

            // Apply rotation
            Transform.rotation = targetRotation;

            // Process distance input
            if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f)
            {
                TargetDistance = _currentDistance;
            }
            TargetDistance += zoomInput * DistanceMovementSpeed;
            TargetDistance = Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);

            // Find the smoothed follow position
            _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1f - Mathf.Exp(-FollowingSharpness * deltaTime));

            // Handle obstructions
            {
                RaycastHit closestHit = new RaycastHit();
                closestHit.distance = Mathf.Infinity;
                _obstructionCount = Physics.SphereCastNonAlloc(_currentFollowPosition, ObstructionCheckRadius, -Transform.forward, _obstructions, TargetDistance, ObstructionLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < _obstructionCount; i++)
                {
                    bool isIgnored = false;
                    for (int j = 0; j < IgnoredColliders.Count; j++)
                    {
                        if (IgnoredColliders[j] == _obstructions[i].collider)
                        {
                            isIgnored = true;
                            break;
                        }
                    }
                    for (int j = 0; j < IgnoredColliders.Count; j++)
                    {
                        if (IgnoredColliders[j] == _obstructions[i].collider)
                        {
                            isIgnored = true;
                            break;
                        }
                    }

                    if (!isIgnored && _obstructions[i].distance < closestHit.distance && _obstructions[i].distance > 0)
                    {
                        closestHit = _obstructions[i];
                    }
                }

                // If obstructions detected
                if (closestHit.distance < Mathf.Infinity)
                {
                    _distanceIsObstructed = true;
                    _currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance, 1 - Mathf.Exp(-ObstructionSharpness * deltaTime));
                }
                // If no obstruction
                else
                {
                    _distanceIsObstructed = false;
                    _currentDistance = Mathf.Lerp(_currentDistance, TargetDistance, 1 - Mathf.Exp(-DistanceMovementSharpness * deltaTime));
                }
            }

            // Find the smoothed camera orbit position
            Vector3 targetPosition = _currentFollowPosition - ((targetRotation * Vector3.forward) * _currentDistance);

            // Handle framing
            targetPosition += Transform.right * FollowPointFraming.x;
            targetPosition += Transform.up * FollowPointFraming.y;

            // Apply position
            Transform.position = targetPosition;
        }
    }
}