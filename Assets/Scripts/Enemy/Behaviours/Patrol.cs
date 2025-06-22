using System.Collections;
using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class Patrol : EnemyAction
{
    [Header("Patrol Behavior")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float patrolRadius = 20f;
    [SerializeField] private float waypointReachedDistance = 1f;

    [Header("Ground Avoidance")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundAvoidanceDistance = 3f;
    [SerializeField] private float groundAvoidanceUpwardForce = 2f;


    private Vector3 initialPosition;
    private Vector3 targetPatrolPosition;
    private bool destinationSet = false;

    public override void OnStart()
    {
        initialPosition = rb.position;
    }

    public override TaskStatus OnUpdate()
    {
        /*if (!destinationSet || Vector3.Distance(rb.position, targetPatrolPosition) < waypointReachedDistance)
        {
            SetNewRandomDestination();
        }

        HandleMovementAndRotation();
        CheckForGround();*/

        return TaskStatus.Running; // This action is always running
    }

    private void SetNewRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        targetPatrolPosition = initialPosition + randomDirection;
        destinationSet = true;
    }

    private void HandleMovementAndRotation()
    {
        if (targetPatrolPosition != Vector3.zero)
        {
            // --- Rotation ---
            Vector3 directionToTarget = (targetPatrolPosition - rb.position).normalized;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // --- Movement ---
            rb.linearVelocity = transform.forward * movementSpeed;
        }
    }

    private void CheckForGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(rb.position, Vector3.down, out hit, groundAvoidanceDistance, groundLayers))
        {
            // If too close to the ground, set a new destination that is higher up
            Vector3 newDirection = transform.forward + (Vector3.up * groundAvoidanceUpwardForce);
            targetPatrolPosition = rb.position + newDirection * patrolRadius;
        }
    }
}
