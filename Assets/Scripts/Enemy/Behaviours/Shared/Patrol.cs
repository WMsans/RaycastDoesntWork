using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

/// <summary>
/// Defines a patrol action for a flying enemy. The enemy will move to random points
/// within a specified radius of its starting position.
/// This action assumes the enemy uses a Rigidbody for movement and is not affected by gravity.
/// </summary>
public class Patrol : EnemyAction
{
    [Header("Patrol Behavior")]
    [Tooltip("The radius around the initial position to patrol within.")]
    [SerializeField] private float patrolRadius = 15f;
    [Tooltip("How close the enemy needs to be to a patrol point to consider it 'reached'.")]
    [SerializeField] private float waypointReachedDistance = 2f;
    [Tooltip("The time to wait at a patrol point before moving to the next one.")]
    [SerializeField] private float waitTimeAtPoint = 1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement")]
    [Tooltip("The movement speed of the enemy.")]
    [SerializeField] private float movementSpeed = 5f;
    [Tooltip("The rotational speed of the enemy as it turns to face its target.")]
    [SerializeField] private float rotationSpeed = 2f;

    // The position where the enemy starts its patrol.
    private Vector3 initialPosition;
    // The current destination point for the patrol.
    private Vector3 targetPatrolPosition;
    // A timer to track how long the enemy has waited at a waypoint.
    private float waitTimer;

    /// <summary>
    /// Called once when the task begins.
    /// </summary>
    public override void OnStart()
    {
        // Store the starting position. All random points will be relative to this.
        initialPosition = rb.position;
        waitTimer = 0f;

        // Set the first destination immediately to begin movement.
        SetNewPatrolDestination();
    }

    /// <summary>
    /// Called every frame by the behavior tree to update the action's status.
    /// </summary>
    /// <returns>Always returns Running, as patrolling is a continuous action.</returns>
    public override TaskStatus OnUpdate()
    {
        // Check if we have reached the destination using SqrMagnitude for efficiency.
        if (Vector3.SqrMagnitude(transform.position - targetPatrolPosition) < waypointReachedDistance * waypointReachedDistance)
        {
            // We've arrived at the patrol point. Stop moving and start the wait timer.
            rb.linearVelocity = Vector3.zero;
            waitTimer += Time.deltaTime;

            // If we have waited long enough, find a new patrol point.
            if (waitTimer >= waitTimeAtPoint)
            {
                waitTimer = 0f;
                SetNewPatrolDestination();
            }
        }
        else
        {
            // If not at the destination, continue moving towards it.
            HandleMovement(targetPatrolPosition);
        }

        // Patrolling is a continuous action that doesn't have a success or failure
        // condition in this context, so it should always be 'Running'.
        return TaskStatus.Running;
    }

    /// <summary>
    /// Stops all movement when this task is interrupted or ends.
    /// </summary>
    public override void OnEnd()
    {
        if (rb != null)
        {
           rb.linearVelocity = Vector3.zero;
           rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Calculates a new random destination within the patrolRadius.
    /// </summary>
    private void SetNewPatrolDestination()
    {
        // Generate a random point within a sphere and add it to the initial position.
        var randomDirection = Random.insideUnitSphere * patrolRadius;
        var newTarget = initialPosition + randomDirection;
        if (Physics.Linecast(rb.position, newTarget, out var info, groundLayer, QueryTriggerInteraction.UseGlobal))
        {
            newTarget = info.distance * info.distance > randomDirection.sqrMagnitude ? initialPosition : Vector3.MoveTowards(newTarget, (rb.position - newTarget).normalized, info.distance + 2f);
        }

        targetPatrolPosition = newTarget;
    }

    /// <summary>
    /// Manages the enemy's movement and rotation towards a target position.
    /// This logic is adapted from your reference DroneMoveToEngagePosition script.
    /// </summary>
    /// <param name="targetPosition">The position to move towards.</param>
    private void HandleMovement(Vector3 targetPosition)
    {
        // 1. Determine the ideal direction towards the target patrol point.
        var desiredDirection = (targetPosition - transform.position).normalized;
        
        // Note: For more advanced behavior, obstacle avoidance logic (like the SphereCast
        // from your reference script) could be added here.

        // 2. Apply Rotation
        // Smoothly rotate to look in the direction of travel.
        if (desiredDirection != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 3. Apply Movement
        // Move the enemy forward in the direction it is now facing.
        // This creates a natural, smooth turning arc.
        rb.linearVelocity = transform.forward * movementSpeed;
    }
}