using UnityEngine;

namespace sapra.InfiniteLands{
    public readonly struct ObjectData{
        public readonly bool SpawnsObject;
        public readonly GameObject gameObject;
        public readonly bool loadAllObjects;
        public ObjectData(bool SpawnsObject, GameObject gameObject, bool loadAllObjects){
            this.SpawnsObject = SpawnsObject;
            this.gameObject = gameObject;
            this.loadAllObjects = loadAllObjects;
        }
    }
}