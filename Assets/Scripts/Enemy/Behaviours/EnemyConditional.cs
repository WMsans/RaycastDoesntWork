using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using UnityEngine;

public abstract class EnemyConditional : Conditional
{
    protected Rigidbody2D rb;
    protected Animator animator;
    protected Harmable enemyHarmable;
    protected bool FacingRight => Mathf.Abs(Mathf.DeltaAngle(transform.rotation.y, 0f)) < 1f;

    public override void OnAwake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponentInChildren<Animator>();
        enemyHarmable = gameObject.GetComponentInChildren<Harmable>();
    }
}
