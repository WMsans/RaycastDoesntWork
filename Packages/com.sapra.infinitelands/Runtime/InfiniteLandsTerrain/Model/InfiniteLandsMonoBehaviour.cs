using UnityEngine;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    public abstract class InfiniteLandsMonoBehaviour : MonoBehaviour, ILandsLifeCycle
    {
        public virtual void Awake()
        {
            var target = transform.root.GetComponent<IControlTerrain>();
            if(target != null){
                target.AddMonoForLifetime(this);
            }
        }
        public virtual void OnDestroy()
        {
            var target = transform.root.GetComponent<IControlTerrain>();
            if(target != null){
                target?.RemoveMonoForLifetime(this);
            }
            Disable();
        }
        
        /// <summary>
        /// Called when the generator starts
        /// </summary>
        /// <param name="lands"></param>
        public abstract void Disable();
        public virtual void OnGraphUpdated(){}

        public abstract void Initialize(IControlTerrain lands);
    }
}