using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookController : MonoBehaviour
{
    [SerializeField] private GameObject hookPrefab;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private Transform hookSpawnPoint;
    [SerializeField] private CharacterControllerStateMachine controllerStateMachine;
    [SerializeField] private OmniCharacterController omniCCharacterController;
    [SerializeField] private NormalCharacterController normalCharacterController; 
    [SerializeField] private KinematicCharacterController.KinematicCharacterMotor motor;
    private float _attackDownTime;
    private float _attackUpTime;
    
    private Hook _hook;
    public void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        if (inputs.HookDown)
        {
            _attackDownTime = Time.time;
        }
        else if(inputs.HookUp)
        {
            _attackUpTime = Time.time;
        }
    }

    public void OnHookHit(Vector3 hitPoint)
    {
        controllerStateMachine.SetCharacterController(omniCCharacterController);
        omniCCharacterController.StartReeling(hitPoint);
    }

    private void Update()
    {
        if (Time.time - _attackDownTime < coyoteTime && _hook == null)
        {
            var hookGo = Instantiate(hookPrefab, hookSpawnPoint.position, transform.rotation);
            var hookBehaviour = hookGo.GetComponent<Hook>();
            hookBehaviour.Init(this, hookSpawnPoint, Camera.main.transform.forward);
            _attackDownTime = -coyoteTime;
            if(_hook!=null) Destroy(_hook.gameObject);
            _hook = hookBehaviour;
        }

        if (Time.time - _attackUpTime < coyoteTime && _hook != null)
        {
            _hook.SetHookState(Hook.HookState.In);
            _hook = null;
            OnHookDetached(); // Call OnHookDetached
            _attackUpTime = -coyoteTime;
        }
    }

    public void OnHookDetached()
    {
        omniCCharacterController.StopReeling();
        controllerStateMachine.SetCharacterController(normalCharacterController);
        _hook?.SetHookState(Hook.HookState.In);
        _hook = null;
    }
}