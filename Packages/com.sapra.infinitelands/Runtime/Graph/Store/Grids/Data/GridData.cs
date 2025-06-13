using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands{
    public struct GridData{
        public JobHandle jobHandle;
        public NativeArray<float3> grid;
        public GridData(NativeArray<float3> grid, JobHandle job){
            this.jobHandle = job;
            this.grid = grid;
        }
    }
}