using System.Collections;
using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class DroneMoveToEngagePosition : EnemyAction
{
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Engagement & Avoidance")]
    [SerializeField] private float engagementDistance = 10f;
    [SerializeField] private float groundAvoidanceDistance = 3f;
    [SerializeField] private LayerMask groundLayers;

    [Header("AI Tuning")]
    [Tooltip("How strongly the drone will steer to avoid obstacles.")]
    [SerializeField] private float avoidanceForceMultiplier = 5f;
    [Tooltip("The radius of the drone, used for more accurate obstacle detection.")]
    [SerializeField] private float droneRadius = 1f;

    /// <summary>
    /// Called every frame by the behavior tree to update the action's status.
    /// </summary>
    /// <returns>The current status of the task (Success, Running, or Failure).</returns>
    public override TaskStatus OnUpdate()
    {
        // Ensure we have a valid reference to the player
        if (CharacterControllerStateMachine.Instance == null)
        {
            Debug.LogWarning("Player instance not found. Halting movement.");
            rb.linearVelocity = Vector3.zero;
            return TaskStatus.Failure;
        }

        var playerPos = CharacterControllerStateMachine.Instance.transform.position;
        var vectorToPlayer = playerPos - transform.position;

        // Check if we are within the desired engagement distance
        // Using sqrMagnitude is more efficient than Magnitude as it avoids a square root calculation.
        if (vectorToPlayer.sqrMagnitude < engagementDistance * engagementDistance)
        {
            rb.linearVelocity = Vector3.zero; // Stop moving when in engagement range
            rb.angularVelocity = Vector3.zero; // Stop rotating as well
            return TaskStatus.Success; // Task is complete
        }

        // If not in range, continue moving towards the player
        HandleMovement(playerPos);

        return TaskStatus.Running; // Task is still in progress
    }

    /// <summary>
    /// Manages the drone's movement, rotation, and obstacle avoidance.
    /// </summary>
    /// <param name="targetPosition">The position of the target (player) to move towards.</param>
    private void HandleMovement(Vector3 targetPosition)
    {
        // 1. Determine the ideal direction towards the player
        Vector3 desiredDirection = (targetPosition - transform.position).normalized;
        Vector3 finalDirection = desiredDirection;

        // 2. Perform a SphereCast to detect potential obstacles in the path
        // A SphereCast is like a thick Raycast, which is better for detecting obstacles for a non-point object.
        if (Physics.SphereCast(transform.position, droneRadius, desiredDirection, out RaycastHit hit, groundAvoidanceDistance, groundLayers))
        {
            // An obstacle was detected. We need to adjust our direction.
            // The hit normal vector points directly away from the obstacle's surface.
            // By adding the hit normal to our desired direction, we create a new path that steers away from the wall.
            Vector3 avoidanceVector = hit.normal * avoidanceForceMultiplier;
            finalDirection = (desiredDirection + avoidanceVector).normalized;
        }

        // 3. Apply Rotation
        // The drone should smoothly rotate to look in its direction of travel.
        if (finalDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 4. Apply Movement
        // Move the drone forward in the direction it is now facing. This creates a natural turning arc.
        rb.linearVelocity = transform.forward * movementSpeed;
    }
}