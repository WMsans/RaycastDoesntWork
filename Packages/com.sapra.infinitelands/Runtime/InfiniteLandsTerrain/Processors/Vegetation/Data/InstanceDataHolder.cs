using UnityEngine;

namespace sapra.InfiniteLands{
    public class InstanceDataHolder : MonoBehaviour
    {
        [field: SerializeField] public InstanceData instanceData{get; private set;}
        [field: SerializeField] public int InstanceID{get; private set;}
        [field: SerializeField] public Vector2Int VegetationChunkID{get; private set;}
        public IHoldVegetation Asset{get; private set;}

        public void UseData(InstanceData data, int instanceID, Vector2Int chunkID, IHoldVegetation asset){
            transform.position = data.GetPosition();
            transform.rotation = data.GetRotation();
            transform.localScale = data.GetScale();
            instanceData = data;
            InstanceID = instanceID;
            VegetationChunkID = chunkID;
            Asset = asset;
        }

        public void OriginShift(Vector3 offset){
            instanceData.PerformShift(offset);
            transform.position += offset;
        }
    }
}