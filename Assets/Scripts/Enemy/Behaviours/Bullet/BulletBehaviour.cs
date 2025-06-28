using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float damage;
    [SerializeField] private float velocity;
    [SerializeField] private float bulletLastTime = 15f;
    [SerializeField] private LayerMask groundLayer;
    private CoroutineHandle _destroySelfCoroutine;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = transform.forward * velocity;
        _destroySelfCoroutine = Timing.RunCoroutine(SelfDestructionCoroutine(bulletLastTime).CancelWith(gameObject));
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            OnHit(other.collider);
        }
        if(((1 << other.gameObject.layer) & groundLayer) > 0) Destroy(gameObject);
    }

    private IEnumerator<float> SelfDestructionCoroutine(float countDown)
    {
        yield return Timing.WaitForSeconds(countDown);
        Destroy(gameObject);
    }

    private void OnHit(Collider other)
    {
        if (other.transform.root.TryGetComponent<IHarmable>(out var harmable))
        {
            harmable.TakeDamage(damage);
        }
        var damagedController = other.transform.root.GetComponentInChildren<DamagedCharacterController>();
        if (damagedController != null)
        {
            CharacterControllerStateMachine.Instance.SetCharacterController(damagedController);
        }
        Destroy(gameObject);
    }
}
