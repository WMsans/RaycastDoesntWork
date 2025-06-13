using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Position", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/position")]
    public class PositionNode : InfiniteLandsNode
    {
        [Output] public HeightData PositionX;
        [Output] public HeightData PositionZ;
        protected override bool WaitingDependencies(BranchData branch)
        {
            GridBranch gridBranch = branch.GetData<GridBranch>();
            return gridBranch.ProcessGrid(out _);  
        }

        protected override void Process(BranchData branch)
        {
            GridBranch gridBranch = branch.GetData<GridBranch>();
            GridData gridData = gridBranch.GetGridData();
            NativeArray<float3x4> vectorized = gridData.grid.Reinterpret<float3x4>(sizeof(float)*3);
            JobHandle dependancy = gridData.jobHandle;

            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var map = heightBranch.GetMap();
            var targetSpaceX = heightBranch.GetAllocationSpace(this, nameof(PositionX));
            var targetSpaceZ = heightBranch.GetAllocationSpace(this, nameof(PositionZ));

            var targetMap = map.Reinterpret<float4>(sizeof(float));
                       
            targetSpaceX.UpdateLength(branch.meshSettings.Resolution);
            targetSpaceZ.UpdateLength(branch.meshSettings.Resolution);

            JobHandle jobX = PositionJob.ScheduleParallel(vectorized,
                        targetMap, branch.terrain.Position, true, targetSpaceX, branch.meshSettings.Resolution,
                        dependancy);
            
            JobHandle jobZ = PositionJob.ScheduleParallel(vectorized,
                        targetMap, branch.terrain.Position, false, targetSpaceZ, branch.meshSettings.Resolution,
                        dependancy);
            
            JobHandle completed = JobHandle.CombineDependencies(jobX, jobZ);

            var position = branch.terrain.Position;
            var scale = branch.meshSettings.MeshScale;
            PositionX = new HeightData(completed, targetSpaceX, new Vector2(position.x-scale, position.x+scale));
            PositionZ = new HeightData(completed, targetSpaceZ, new Vector2(position.z-scale, position.z+scale));
        }
        
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, PositionX, nameof(PositionX));
            CacheOutputValue(branch, PositionZ, nameof(PositionZ));
        }
    }
}