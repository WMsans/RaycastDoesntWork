using UnityEngine;

/// <summary>
/// Abstract base class for all weapon behaviours.
/// Defines the common interface for how a weapon operates.
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    protected WeaponController Owner { get; private set; }

    /// <summary>
    /// Initializes the weapon with a reference to its owner.
    /// </summary>
    public void Init(WeaponController owner)
    {
        Owner = owner;
    }

    // --- Input Methods ---
    // These are called by the WeaponController based on player input.

    public abstract void OnAttackDown();
    public abstract void OnAttackHold();
    public abstract void OnAttackUp();

    // --- State Methods ---
    // Called when the weapon is equipped or unequipped.

    /// <summary>
    /// Called when this weapon becomes the active weapon.
    /// </summary>
    public virtual void OnEnabledWeapon() { }

    /// <summary>
    /// Called when this weapon is no longer the active weapon.
    /// </summary>
    public virtual void OnDisabledWeapon() { }
}