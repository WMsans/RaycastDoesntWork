using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace sapra.InfiniteLands{  
    public abstract class UpdateableSO : ScriptableObject
    {
        [HideInInspector] public Action OnValuesUpdated;

        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () => OnValuesUpdated?.Invoke();
#endif
        }
    }
}