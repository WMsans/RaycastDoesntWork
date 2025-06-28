using System;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    private void Start()
    {
        Physics.IgnoreLayerCollision(3,8);
        Physics.IgnoreLayerCollision(7,8);
    }
}
