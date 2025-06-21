using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using UnityEngine;

public class CharacterControllerStateMachine : MonoBehaviour
{
    public static CharacterControllerStateMachine Instance { get; private set; }
    public BaseCharacterController defaultCharacterController;
    public KinematicCharacterMotor motor;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one player statemachine is active");
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
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
