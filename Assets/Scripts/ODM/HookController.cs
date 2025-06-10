using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookController : MonoBehaviour
{
    [SerializeField] private GameObject hookPrefab;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private Transform hookSpawnPoint;
    [SerializeField] private CharacterControllerStateMachine controllerStateMachine;
    [SerializeField] private OmniCHaracterController omniCCharacterController;
    private float _attackDownTime;
    private float _attackHoldTime;
    public void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        if (inputs.AttackDown)
        {
            _attackDownTime = Time.time;
        }
        else if (inputs.AttackHold)
        {
            _attackHoldTime = Time.time;
        }
    }

    public void OnHookHit(Vector3 hitPoint)
    {
        // TODO: Implement hook hit
        controllerStateMachine.SetCharacterController(omniCCharacterController);
    }

    private void Update()
    {
        if (Time.time - _attackDownTime < coyoteTime)
        {
            var hook = Instantiate(hookPrefab, hookSpawnPoint.position, transform.rotation);
            var hookBehaviour = hook.GetComponent<Hook>();
            hookBehaviour.Init(this, hookSpawnPoint, Camera.main.transform.forward);
            _attackDownTime = -coyoteTime;
        }
    }
}
