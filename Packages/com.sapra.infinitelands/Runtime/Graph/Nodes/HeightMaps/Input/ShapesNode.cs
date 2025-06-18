using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Shape", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/shape",
        synonims = new string[]{"Simple Form", "Cube",
            "HalfSphere",
            "Cone",
            "Bump",
            "Pyramid",
            "Cylinder",
            "Torus"})]
    public class ShapesNode : InfiniteLandsNode
    {
        public enum ShapeType
        {
            Cube,
            HalfSphere,
            Cone,
            Bump,
            Pyramid,
            Cylinder,
            Torus
        }
        public ShapeType Shape;

        [Output] public HeightData Output;
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        public Vector2 Origin = Vector2.zero;
        [ShowIf(nameof(Rotable))] public float YRotation = 0;
        [Min(1)] public float Size = 200;
        public bool Rotable => Shape == ShapeType.Cube || Shape == ShapeType.Pyramid;

        protected override bool WaitingDependencies(BranchData branch)
        {
            GridBranch gridBranch = branch.GetData<GridBranch>();
            return gridBranch.ProcessGrid(out _);
        }

        protected override void Process(BranchData branch)
        {
/*             if (MinMaxHeight.x >= MinMaxHeight.y)
                MinMaxHeight.x = MinMaxHeight.y - 0.1f; */
            
            GridBranch gridBranch = branch.GetData<GridBranch>();
            GridData gridData = gridBranch.GetGridData();
            NativeArray<float3x4> vectorized = gridData.grid.Reinterpret<float3x4>(sizeof(float)*3);
            JobHandle dependancy = gridData.jobHandle;

            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();

            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            var targetMap = map.Reinterpret<float4>(sizeof(float));
            targetSpace.UpdateLength(branch.meshSettings.Resolution);

            Vector3 origin = new Vector3(Origin.x, 0, Origin.y);
            JobHandle jobHandle;
            switch (Shape)
            {
                case ShapeType.HalfSphere:
                    jobHandle = ShapeJob<SHalfSphere>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
                    dependancy);
                    break;
                case ShapeType.Cone:
                    jobHandle = ShapeJob<SCone>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
                    dependancy);
                    break;
                case ShapeType.Pyramid:
                    jobHandle = ShapeJob<SPyramid>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
                    dependancy);
                    break;
                case ShapeType.Bump:
                    jobHandle = ShapeJob<SBump>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
                    dependancy);
                    break;
                case ShapeType.Cylinder:
                    jobHandle = ShapeJob<SCylinder>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
                    dependancy);
                    break;
                case ShapeType.Torus:
                    jobHandle = ShapeJob<SHalfTorus>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
                    dependancy);
                    break;
                default:
                    jobHandle = ShapeJob<SSquare>.ScheduleParallel(vectorized,
                    targetMap, MinMaxHeight, branch.terrain.Position-origin,
                    YRotation,Size, targetSpace, branch.meshSettings.Resolution,
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