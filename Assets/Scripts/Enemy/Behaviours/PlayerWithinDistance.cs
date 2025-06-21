using System.Collections;
using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class PlayerWithinDistance : EnemyConditional
{
    [SerializeField] private float distance;

    public override TaskStatus OnUpdate()
    {
        return Vector3.Distance(CharacterControllerStateMachine.Instance.transform.position,
            gameObject.transform.position) < distance
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
