using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using UnityEngine;

public abstract class EnemyAction : Action
{
    protected Rigidbody2D rb;
    protected Animator animator;
    protected bool FacingRight => Mathf.Abs(Mathf.DeltaAngle(transform.rotation.y, 0f)) < 1f;

    public override void OnAwake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponentInChildren<Animator>();
    }
}
