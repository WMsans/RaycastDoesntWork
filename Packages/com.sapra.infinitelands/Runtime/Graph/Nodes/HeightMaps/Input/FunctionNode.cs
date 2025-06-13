using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
namespace sapra.InfiniteLands
{
    [CustomNode("Function", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/function")]
    public class FunctionNode : InfiniteLandsNode
    {
        public enum FunctionType
        {
            Sine,
            Square,
            Triangle,
            SawTooth
        }

        public FunctionType functionType;
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        public float YRotation = 0;
        [Min(0.01f)] public float Period = 100;

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
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();

            GridData gridData = gridBranch.GetGridData();      
            NativeArray<float3x4> vectorized = gridData.grid.Reinterpret<float3x4>(sizeof(float)*3);
            JobHandle dependancy = gridData.jobHandle;

            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            var targetMap = map.Reinterpret<float4>(sizeof(float));
            targetSpace.UpdateLength(branch.meshSettings.Resolution);

            JobHandle jobHandle;
            switch (functionType)
            {
                case FunctionType.Square:
                    jobHandle = FunctionJob<FSquare>.ScheduleParallel(vectorized,
                        targetMap, new Vector2(MinMaxHeight.x + .01f, MinMaxHeight.y - 0.01f),
                        branch.terrain.Position,
                        Period, YRotation, targetSpace, branch.meshSettings.Resolution,
                        dependancy);
                        break;
                case FunctionType.Triangle:
                    jobHandle = FunctionJob<FTriangle>.ScheduleParallel(vectorized,
                        targetMap, MinMaxHeight, branch.terrain.Position,
                        Period, YRotation, targetSpace, branch.meshSettings.Resolution,
                        dependancy);
                        break;
                case FunctionType.SawTooth:
                    jobHandle = FunctionJob<FSawTooth>.ScheduleParallel(vectorized,
                        targetMap, MinMaxHeight, branch.terrain.Position,
                        Period, YRotation, targetSpace, branch.meshSettings.Resolution,
                        dependancy);
                        break;
                default:
                    jobHandle = FunctionJob<FSine>.ScheduleParallel(vectorized,
                        targetMap, MinMaxHeight, branch.terrain.Position,
                        Period, YRotation, targetSpace, branch.meshSettings.Resolution,
                        dependancy);
                        break;
            }
            Output = new HeightData(jobHandle, targetSpace, new Vector2(MinMaxHeight.x, MinMaxHeight.y));
        }

        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}