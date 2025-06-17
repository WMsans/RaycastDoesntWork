using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A component for any GameObject that can take damage and has health.
/// Provides events for taking damage, healing, and dying.
/// </summary>
public class Harmable : MonoBehaviour
{
    [Tooltip("The maximum health of this object.")]
    [SerializeField] private int maxHealth = 100;

    // The current health of the object. Readonly property to prevent outside modification.
    public int CurrentHealth { get; private set; }

    // --- UNITY EVENTS ---
    // These events allow you to hook up other scripts and effects in the Unity Editor
    // without writing any extra code.

    /// <summary>
    /// Event invoked when the object takes damage.
    /// Passes the amount of damage taken and the new current health.
    /// </summary>
    [System.Serializable]
    public class DamageEvent : UnityEvent<int, int> { }
    public DamageEvent OnDamage;

    /// <summary>
    /// Event invoked when the object is healed.
    /// Passes the amount of health restored and the new current health.
    /// </summary>
    [System.Serializable]
    public class HealEvent : UnityEvent<int, int> { }
    public HealEvent OnHeal;

    /// <summary>
    /// Event invoked when the object's health reaches zero.
    /// </summary>
    public UnityEvent OnDeath;


    private void Awake()
    {
        // Initialize current health to the maximum health when the object is created.
        CurrentHealth = maxHealth;
    }

    /// <summary>
    /// Reduces the object's health by a specified amount.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to inflict. Must be positive.</param>
    public void TakeDamage(int damageAmount)
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
    public void Heal(int healAmount)
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
    public void SetHealth(int healthValue)
    {
        CurrentHealth = Mathf.Clamp(healthValue, 0, maxHealth);
    }
}
