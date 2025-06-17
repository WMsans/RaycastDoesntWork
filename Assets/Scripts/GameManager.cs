using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("More than one GameManager is active");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Physics.IgnoreLayerCollision(3,8);
        Physics.IgnoreLayerCollision(7,8);
    }
}
