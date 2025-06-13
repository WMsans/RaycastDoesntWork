using UnityEngine;


using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
namespace sapra.InfiniteLands
{
    [CustomNode("Random Noise", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/random")]
    public class RandomNoiseNode : InfiniteLandsNode
    {
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        [Min(0.01f)] public float Size = 100;

        [Output] public HeightData Output;

        protected override bool WaitingDependencies(BranchData branch)
        {
            GridBranch gridBranch = branch.GetData<GridBranch>();
            return gridBranch.ProcessGrid(out _);  
        }

        protected override void Process(BranchData branch)
        {
            if (MinMaxHeight.x >= MinMaxHeight.y)
                MinMaxHeight.x = MinMaxHeight.y - 0.1f;
            
            GridBranch gridBranch = branch.GetData<GridBranch>();

            GridData gridData = gridBranch.GetGridData();
            NativeArray<float3x4> vectorized = gridData.grid.Reinterpret<float3x4>(sizeof(float)*3);
            JobHandle dependancy = gridData.jobHandle;

            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();

            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            var targetMap = map.Reinterpret<float4>(sizeof(float));
            targetSpace.UpdateLength(branch.meshSettings.Resolution);

            JobHandle jobHandle = RandomJob.ScheduleParallel(vectorized,
                        targetMap, new Vector2(MinMaxHeight.x + .01f, MinMaxHeight.y - 0.01f),
                        branch.terrain.Position,
                        Size, targetSpace, branch.meshSettings.Resolution,
                        dependancy);
            Output = new HeightData(jobHandle, targetSpace, new Vector2(MinMaxHeight.x, MinMaxHeight.y));
        }

        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}