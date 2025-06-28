using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHarmable : MonoSingleton<PlayerHarmable>, IHarmable
{
    [SerializeField] private float maxHealth;
    [SerializeField] private bool decreaseSpeedAfterHit;
    [SerializeField] private KinematicCharacterMotor motor;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    
    /// <summary>
    /// Event invoked when the object takes damage.
    /// Passes the amount of damage taken and the new current health.
    /// </summary>
    [System.Serializable]
    public class DamageEvent : UnityEvent<float, float> { }
    public DamageEvent OnDamage;

    /// <summary>
    /// Event invoked when the object is healed.
    /// Passes the amount of health restored and the new current health.
    /// </summary>
    [System.Serializable]
    public class HealEvent : UnityEvent<float, float> { }
    public HealEvent OnHeal;

    /// <summary>
    /// Event invoked when the object's health reaches zero.
    /// </summary>
    public UnityEvent OnDeath;


    private void Start()
    {
        // Initialize current health to the maximum health when the object is created.
        CurrentHealth = maxHealth;
    }
    public void TakeDamage(float damageAmount)
    {
        // Ensure damage is not negative.
        if (damageAmount <= 0) return;

        // Don't process damage if already dead.
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damageAmount;

        // Clamp health to a minimum of 0.
        if (CurrentHealth < 0)
        {
            CurrentHealth = 0;
        }

        // Invoke the OnDamage event, passing the damage amount and the new health.
        OnDamage?.Invoke(damageAmount, CurrentHealth);
        if (decreaseSpeedAfterHit)
        {
            var controller = CharacterControllerStateMachine.Instance.CurrentCharacterController;
            var currentVelocity = CharacterControllerStateMachine.Instance.motor.Velocity;
            controller.AddVelocity(currentVelocity * -0.7f);
        }

        // Check if the object has died.
        if (CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Increases the object's health by a specified amount.
    /// </summary>
    /// <param name="healAmount">The amount of health to restore. Must be positive.</param>
    public void Heal(float healAmount)
    {
        // Ensure heal amount is not negative.
        if (healAmount <= 0) return;

        // Don't process healing if already dead.
        if (CurrentHealth <= 0) return;

        CurrentHealth += healAmount;

        // Clamp health to a maximum of maxHealth.
        if (CurrentHealth > maxHealth)
        {
            CurrentHealth = maxHealth;
        }

        // Invoke the OnHeal event, passing the heal amount and the new health.
        OnHeal?.Invoke(healAmount, CurrentHealth);
    }

    /// <summary>
    /// Instantly sets the health to a specific value, bypassing normal damage/heal logic.
    /// Good for initialization or special game mechanics.
    /// </summary>
    /// <param name="healthValue">The value to set current health to.</param>
    public void SetHealth(float healthValue)
    {
        CurrentHealth = Mathf.Clamp(healthValue, 0, maxHealth);
    }
}
