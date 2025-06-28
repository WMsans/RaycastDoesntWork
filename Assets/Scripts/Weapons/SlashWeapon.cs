using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A melee weapon that creates a temporary hitbox to damage enemies on attack.
/// </summary>
public class SlashWeapon : Weapon
{
    [Header("Slash Settings")]
    [Tooltip("The prefab for the slash visual effect and collider.")]
    [SerializeField] private GameObject slashPrefab;
    [Tooltip("The duration in seconds that the slash hitbox is active.")]
    [SerializeField] private float slashDuration = 0.3f;
    [Tooltip("Damage dealt by a single slash.")]
    [SerializeField] private int damage = 25;
    [Tooltip("The time between consecutive attacks.")]
    [SerializeField] private float attackCooldown = 0.5f;
    [Tooltip("Layers that contain enemies.")]
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("The offset from the weapon's pivot where the slash hitbox is centered.")]
    [SerializeField] private Vector3 slashOffset = new Vector3(0, 0, 1.5f);
    [Tooltip("The size of the box used for hit detection.")]
    [SerializeField] private Vector3 slashHalfExtents = new Vector3(0.5f, 1f, 1.5f);

    private float _nextAttackTime;
    private Coroutine _attackCoroutine;
    private GameObject _activeSlashInstance;

    #region Weapon Overrides

    public override void OnAttackDown()
    {
        Debug.Log("Down");
        // Check if the cooldown has passed and no attack is currently active
        if (Time.time >= _nextAttackTime && _attackCoroutine == null)
        {
            _nextAttackTime = Time.time + attackCooldown;
            _attackCoroutine = StartCoroutine(AttackCoroutine());
        }
    }

    public override void OnAttackHold() { }
    public override void OnAttackUp() { }

    /// <summary>
    /// Cleans up the attack if the weapon is switched mid-slash.
    /// </summary>
    public override void OnDisabledWeapon()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        if (_activeSlashInstance != null)
        {
            Destroy(_activeSlashInstance);
        }
    }

    #endregion

    /// <summary>
    /// Manages the lifecycle of a single slash attack.
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        // 1. Instantiate the slash effect
        if (slashPrefab != null)
        {
            var spawnTransform = transform; 
            _activeSlashInstance = Instantiate(slashPrefab, spawnTransform.position, spawnTransform.rotation, spawnTransform);
        }

        // 2. Perform hit detection over the slash duration
        var alreadyHit = new List<Harmable>();
        var startTime = Time.time;

        while (Time.time - startTime < slashDuration)
        {
            DetectAndDamageEnemies(alreadyHit);
            yield return null; // Wait for the next frame
        }

        // 3. Clean up
        if (_activeSlashInstance != null)
        {
            Destroy(_activeSlashInstance);
        }
        _attackCoroutine = null; // Mark the coroutine as finished
    }

    /// <summary>
    /// Uses an OverlapBox to find and damage enemies within the slash area.
    /// </summary>
    /// <param name="alreadyHit">A list of enemies already hit by this slash to prevent multi-hits.</param>
    private void DetectAndDamageEnemies(List<Harmable> alreadyHit)
    {
        var boxCenter = transform.position + transform.rotation * slashOffset;
        var hits = Physics.OverlapBox(boxCenter, slashHalfExtents, transform.rotation, enemyLayers);

        foreach (var hit in hits)
        {
            if(!hit.transform.root.CompareTag("Enemy")) continue;
            var harmable = hit.transform.root.GetComponent<Harmable>();

            // Check if the object is harmable and hasn't been hit by this slash yet
            if (harmable == null || alreadyHit.Contains(harmable)) continue;
            harmable.TakeDamage(damage);
            alreadyHit.Add(harmable); // Add to the list to prevent hitting it again
        }
    }

    /// <summary>
    /// Draws a gizmo in the editor to visualize the slash hitbox.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red and semi-transparent
        
        // Match the Gizmo's rotation and position to the weapon
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // Draw the wireframe cube representing the hitbox
        Gizmos.DrawWireCube(slashOffset, slashHalfExtents * 2);
    }
}