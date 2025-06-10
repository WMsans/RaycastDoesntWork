using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using UnityEngine;

public class CharacterControllerStateMachine : MonoBehaviour
{
    [SerializeField] private BaseCharacterController defaultCharacterController;
    [SerializeField] private KinematicCharacterMotor motor;
    private BaseCharacterController _currentCharacterController;

    public void SetCharacterController(BaseCharacterController characterController)
    {
        if (_currentCharacterController == characterController || characterController == null) return;
        _currentCharacterController?.OnDisableController();
        _currentCharacterController = characterController;
        motor.CharacterController = characterController;
        _currentCharacterController.SetMotor(motor);
        _currentCharacterController.OnEnableController();
    }

    private void Start()
    {
        SetCharacterController(defaultCharacterController);
    }

    public virtual void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        _currentCharacterController.SetInputs(ref inputs);
    }
}
