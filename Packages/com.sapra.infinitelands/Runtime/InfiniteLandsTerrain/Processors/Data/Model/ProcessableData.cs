using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public abstract class ProcessableData{
        private List<object> processors = new();
        public bool isValid => processors.Count > 0;
        public void AddProcessor(object processor){
            #if UNITY_EDITOR
            if(processors.Contains(processor))
                Debug.LogWarningFormat("{0} already exists!! Adding a duplicate");
            #endif
            processors.Add(processor);
        }

        public void RemoveProcessor(object processor){
            #if UNITY_EDITOR
            if (!processors.Contains(processor))
                Debug.LogWarningFormat("{0} doesn't exist!! Duplicate call");
            #endif
            processors.Remove(processor);
            if(processors.Count <= 0 ){
                OnFinalisedProcessors();
                processors.Clear();
            }
        }

        protected abstract void OnFinalisedProcessors();
    }
}