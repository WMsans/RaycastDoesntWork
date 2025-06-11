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
    private float _attackUpTime;
    
    private Hook _hook;
    public void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        if (inputs.AttackDown)
        {
            _attackDownTime = Time.time;
        }
        else if(inputs.AttackUp)
        {
            _attackUpTime = Time.time;
        }
    }

    public void OnHookHit(Vector3 hitPoint)
    {
        // TODO: Implement hook hit
        // controllerStateMachine.SetCharacterController(omniCCharacterController);
    }

    private void Update()
    {
        if (Time.time - _attackDownTime < coyoteTime)
        {
            var hook = Instantiate(hookPrefab, hookSpawnPoint.position, transform.rotation);
            var hookBehaviour = hook.GetComponent<Hook>();
            hookBehaviour.Init(this, hookSpawnPoint, Camera.main.transform.forward);
            _attackDownTime = -coyoteTime;
            _hook = hookBehaviour;
        }

        if (Time.time - _attackUpTime < coyoteTime && _hook != null)
        {
            _hook.SetHookState(Hook.HookState.In);
            _hook = null;
        }
    }
}
