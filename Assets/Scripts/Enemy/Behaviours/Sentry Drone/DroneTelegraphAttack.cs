using System.Collections;
using System.Collections.Generic;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class DroneTelegraphAttack : EnemyAction
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform ShootPoint;

    public override TaskStatus OnUpdate()
    {
        Object.Instantiate(bulletPrefab, ShootPoint.position, ShootPoint.rotation);
        return TaskStatus.Success;
    }
}
