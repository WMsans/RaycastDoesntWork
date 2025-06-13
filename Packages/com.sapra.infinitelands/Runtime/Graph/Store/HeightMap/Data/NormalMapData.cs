using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands{
    public struct NormalMapData{
        public JobHandle jobHandle;
        public NativeArray<float3> NormalMap;
        public IndexAndResolution indexData;
    }
    
}