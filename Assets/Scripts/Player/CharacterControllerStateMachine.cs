using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using UnityEngine;

public class CharacterControllerStateMachine : MonoSingleton<CharacterControllerStateMachine>
{
    public BaseCharacterController defaultCharacterController;
    public KinematicCharacterMotor motor;
    public BaseCharacterController CurrentCharacterController { get; private set; }

    public void SetCharacterController(BaseCharacterController characterController)
    {
        if (CurrentCharacterController == characterController || characterController == null) return;
        CurrentCharacterController?.OnDisableController();
        CurrentCharacterController = characterController;
        motor.CharacterController = characterController;
        CurrentCharacterController.SetMotor(motor);
        CurrentCharacterController.OnEnableController();
    }

    private void Start()
    {
        SetCharacterController(defaultCharacterController);
    }

    public virtual void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        CurrentCharacterController.SetInputs(ref inputs);
    }
}
