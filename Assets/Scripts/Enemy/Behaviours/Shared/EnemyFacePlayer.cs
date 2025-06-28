using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class EnemyFacePlayer : EnemyAction
{
    [SerializeField] private float time;
    private bool _ended;
    public override void OnStart()
    {
        _ended = false;
        var playerPos = CharacterControllerStateMachine.Instance.motor.Capsule.bounds.center;
        transform.DORotate(Quaternion.LookRotation(playerPos - transform.position).eulerAngles, time).OnComplete(()=>_ended = true);
    }
    public override TaskStatus OnUpdate()
    {
        return _ended ? TaskStatus.Success : TaskStatus.Running;
    }
}
