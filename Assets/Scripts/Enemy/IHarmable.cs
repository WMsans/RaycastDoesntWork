using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHarmable
{
    public float MaxHealth { get; }
    public float CurrentHealth { get; }
    public void TakeDamage(float damageAmount);
    public void Heal(float healAmount);
    public void SetHealth(float healthValue);
}
