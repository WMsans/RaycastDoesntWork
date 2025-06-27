using System.Collections;
using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class EnemyStopMoving : EnemyAction
{
    [SerializeField] private float decelerate;
    [SerializeField] private float angleDecelerate;

    public override TaskStatus OnUpdate()
    {
        // Decelerate linear velocity
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, decelerate * Time.deltaTime);

        // Decelerate angular velocity
        rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, Vector3.zero, angleDecelerate * Time.deltaTime);

        // Check if both linear and angular velocities are close to zero
        if (rb.linearVelocity.sqrMagnitude < 0.1f && rb.angularVelocity.sqrMagnitude < 0.1f)
        {
            rb.linearVelocity = rb.angularVelocity = Vector3.zero;
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }
}
