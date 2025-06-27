using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class CanSeePlayer : EnemyConditional
{
    [Header("Sight Settings")]
    [Tooltip("The point from which the visibility raycast is fired.")]
    [SerializeField] private Transform raycastPoint;

    [Tooltip("The maximum distance the enemy can see the player.")]
    [SerializeField] private float raycastDistance;

    [Tooltip("The total angle of the enemy's field of view in degrees. The enemy can see half of this angle to the left and half to the right.")]
    [SerializeField] private float fieldOfView = 90f;

    [Tooltip("The layers that can obstruct the enemy's line of sight (e.g., 'Walls', 'Environment').")]
    [SerializeField] private LayerMask groundLayers;

    public override TaskStatus OnUpdate()
    {
        var player = CharacterControllerStateMachine.Instance;

        if (player == null)
        {
            return TaskStatus.Failure;
        }

        Vector3 originPosition = raycastPoint.position;
        Vector3 playerTargetPosition = player.motor.Capsule.bounds.center;

        float distanceToPlayer = Vector3.Distance(originPosition, playerTargetPosition);

        if (distanceToPlayer > raycastDistance)
        {
            return TaskStatus.Failure;
        }

        Vector3 directionToPlayer = (playerTargetPosition - originPosition).normalized;

        float angleToPlayer = Vector3.Angle(raycastPoint.forward, directionToPlayer);

        if (angleToPlayer > fieldOfView / 2)
        {
            Debug.Log(angleToPlayer + " " + fieldOfView);
            return TaskStatus.Failure;
        }

        if (Physics.Linecast(originPosition, playerTargetPosition, groundLayers))
        {
            return TaskStatus.Failure;
        }

        return TaskStatus.Success;
    }
}