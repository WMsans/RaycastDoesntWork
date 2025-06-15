using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hook : MonoBehaviour
{
    public enum HookState
    {
        Out,
        In,
        Stabilized,
    }
    [SerializeField] private Rigidbody hookRigidbody;
    [SerializeField] private Collider hookCollider;
    [SerializeField] private float hookSpeed = 10f;
    [SerializeField] private float hookDistance = 100f; // Increased distance
    [SerializeField] private LayerMask hookColliderLayers;
    [SerializeField] private float hookRadius = .3f;
    [SerializeField] private LineRenderer hookLineRenderer;
    public HookState CurrentState { get; private set; } = HookState.Out;
    
    private HookController _hookController;
    private Vector3 _hookDirection;
    private Transform _startPosition;
    private float _startTime;

    public void Init(HookController hookController, Transform startPosition, Vector3 hookDirection)
    {
        this._hookController = hookController;
        hookRigidbody.linearVelocity = hookDirection * hookSpeed;
        _startPosition = startPosition;
        this._hookDirection = hookDirection;
        CurrentState = HookState.Out;
        _startTime = Time.time;
        
        hookLineRenderer.positionCount = 2;
        hookLineRenderer.SetPosition(0, startPosition.position);
        hookLineRenderer.SetPosition(1, transform.position);
    }

    private void Update()
    {
        if (CurrentState == HookState.Out)
        {
            OutUpdate();
        }
        else if (CurrentState == HookState.In)
        {
            InUpdate();
        }
        else if (CurrentState == HookState.Stabilized)
        {
            StabilizedUpdate();
        }
        hookLineRenderer.SetPosition(0, _startPosition.position);
        hookLineRenderer.SetPosition(1, transform.position);
    }

    private void OutUpdate()
    {
        hookRigidbody.linearVelocity = _hookDirection * hookSpeed;
        if (Vector3.Distance(hookRigidbody.position, _startPosition.position) > hookDistance)
        {
            CurrentState = HookState.In;
        }
    }
    private void InUpdate()
    {
        hookRigidbody.isKinematic = false;
        hookRigidbody.linearVelocity = Vector3.zero;
        hookRigidbody.position = Vector3.MoveTowards(hookRigidbody.position, _startPosition.position, hookSpeed * Time.deltaTime);
        if(Vector3.Distance(hookRigidbody.position, _startPosition.position) < 1f)
        {
            Destroy(gameObject);
        }
    }

    private void StabilizedUpdate()
    {
        hookRigidbody.isKinematic = true;
    }

    public void SetHookState(HookState state)
    {
        CurrentState = state;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (CurrentState == HookState.Out && ( hookColliderLayers & (1 << other.gameObject.layer)) != 0)
        {
            CurrentState = HookState.Stabilized;
            _hookController.OnHookHit(hookRigidbody.position);
            hookRigidbody.linearVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hookRadius);
    }
}