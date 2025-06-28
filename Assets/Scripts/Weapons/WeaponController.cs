using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the player's weapons. Handles input delegation and weapon switching.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Tooltip("The weapon the player starts with.")]
    [SerializeField] private Weapon startingWeapon;

    [Tooltip("A transform marking where the weapon should be held.")]
    [SerializeField] private Transform weaponHoldSocket;

    public UnityEvent<Weapon> OnUseWeapon;

    public Weapon CurrentWeapon { get; private set; }

    private void Start()
    {
        if (startingWeapon != null)
        {
            SetWeapon(startingWeapon);
        }
    }

    private void HandleWeaponUsed(Weapon weapon)
    {
        OnUseWeapon?.Invoke(weapon);
    }

    /// <summary>
    /// Sets the currently active weapon.
    /// </summary>
    /// <param name="newWeaponPrefab">The prefab of the weapon to equip.</param>
    public void SetWeapon(Weapon newWeaponPrefab)
    {
        // Destroy the old weapon if one exists
        if (CurrentWeapon != null)
        {
            CurrentWeapon.OnWeaponUsed -= HandleWeaponUsed;
            CurrentWeapon.OnDisabledWeapon();
            Destroy(CurrentWeapon.gameObject);
        }
        // Create and initialize the new weapon
        if (newWeaponPrefab != null)
        {
            CurrentWeapon = Instantiate(newWeaponPrefab, weaponHoldSocket.position, weaponHoldSocket.rotation, weaponHoldSocket);
            CurrentWeapon.Init(this);
            CurrentWeapon.OnWeaponUsed += HandleWeaponUsed;
            CurrentWeapon.OnEnabledWeapon();
        }
    }

    /// <summary>
    /// Processes player inputs and passes them to the current weapon.
    /// </summary>
    public void SetInputs(ref Player.PlayerCharacterInputs inputs)
    {
        if (CurrentWeapon == null) return;

        if (inputs.AttackDown) CurrentWeapon.OnAttackDown();
        if (inputs.AttackHold) CurrentWeapon.OnAttackHold();
        if (inputs.AttackUp) CurrentWeapon.OnAttackUp();
    }
}