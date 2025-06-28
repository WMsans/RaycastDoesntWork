using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one instance of " + nameof(T) + " is in the scene");
            Destroy(gameObject);
            return;
        }
        Instance = this as T;
    }
}
