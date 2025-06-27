using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using UnityEngine;

public abstract class EnemyAction : Action
{
    protected Rigidbody rb;
    protected Animator animator;

    public override void OnAwake()
    {
        rb = GetComponent<Rigidbody>();
        animator = gameObject.GetComponentInChildren<Animator>();
    }
}
