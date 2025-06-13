using Unity.Jobs;
using UnityEngine;
using static sapra.InfiniteLands.Noise;

using System;
using Unity.Mathematics;
using Unity.Collections;

namespace sapra.InfiniteLands
{
    [CustomNode("Voronoi Noise", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/voronoi_noise.html")]
    public class VoronoiNoiseNode : InfiniteLandsNode
    {
        public enum VoronoiType{
            VoronoiF1,
            VoronoiF2,
            VoronoiF1F2,
            VoronoiIndex
        };
        public VoronoiType NoiseType = VoronoiType.VoronoiF1;
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        [Min(0.001f)] public float TileSize = 100;

        [Min(1)] public int Octaves = 1;
        public Vector3 Rotation;

        [ShowIf(nameof(octavesEnabled))][Range(1,10)]public int Lacunarity = 2;
        [ShowIf(nameof(octavesEnabled))][Range(0f, 1f)] public float Persistence = .5f;
        [Range(0,1)] [Min(0.001f)] public float SmoothVoronoi = .001f;
        public bool RidgeMode;
        public bool FixedSeed;
        [ShowIf(nameof(FixedSeed))] public int SeedOffset;

        private bool octavesEnabled => Octaves > 1;

        [Output] public HeightData Output;

        NoiseSettings getsettings()
        {
            return new NoiseSettings()
            {
                scale = TileSize,
                octaves = Octaves,
                minmaxValue = MinMaxHeight,
                lacunarity = Lacunarity,
                persistence = Persistence,
                SmoothVoronoi = SmoothVoronoi,
                rotation = Rotation,
                ridgeMode = RidgeMode
            };
        }
        
        protected override bool WaitingDependencies(BranchData branch)
        {
            GridBranch gridBranch = branch.GetData<GridBranch>();

            return gridBranch.ProcessGrid(out _);  
        }

        protected override void Process(BranchData branch)
        {
            GridBranch gridBranch = branch.GetData<GridBranch>();
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            GridData gridData = gridBranch.GetGridData();
            NativeArray<float3x4> vectorized = gridData.grid.Reinterpret<float3x4>(sizeof(float)*3);
            JobHandle dependancy = gridData.jobHandle;

            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            var targetMap = map.Reinterpret<float4>(sizeof(float));
            targetSpace.UpdateLength(branch.meshSettings.Resolution);

            NoiseSettings noiseSettings = getsettings();           
            int indexOffset = FixedSeed?SeedOffset:GetRandomIndex();
            float maxOctaves = Mathf.Pow(int.MaxValue, 1f/Lacunarity);
            noiseSettings.octaves = Mathf.Max(1,Mathf.Min(noiseSettings.octaves, Mathf.FloorToInt(maxOctaves)));

            JobHandle jobHandle;
            switch (NoiseType)
            {
                case VoronoiType.VoronoiF1:
                    jobHandle = NoiseJob<Voronoi2D<F1>>.ScheduleParallel(vectorized, 
                        targetMap,
                        noiseSettings, branch.terrain.Position, 
                        branch.meshSettings.Resolution, targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
                case VoronoiType.VoronoiF2:
                    jobHandle = NoiseJob<Voronoi2D<F2>>.ScheduleParallel(vectorized, 
                        targetMap,
                        noiseSettings, branch.terrain.Position, 
                        branch.meshSettings.Resolution, targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
                case VoronoiType.VoronoiF1F2:
                    jobHandle = NoiseJob<F2MinusF1>.ScheduleParallel(vectorized, 
                        targetMap,
                        noiseSettings, branch.terrain.Position, 
                        branch.meshSettings.Resolution, targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
                case VoronoiType.VoronoiIndex:
                    jobHandle = NoiseJob<VoronoiIndex>.ScheduleParallel(vectorized, 
                        targetMap,
                        noiseSettings, branch.terrain.Position, 
                        branch.meshSettings.Resolution, targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
                default:
                    jobHandle = NoiseJob<Voronoi2D<F1>>.ScheduleParallel(vectorized, 
                        targetMap,
                        noiseSettings, branch.terrain.Position, 
                        branch.meshSettings.Resolution, targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
            }
            
            Output = new HeightData(jobHandle, targetSpace, MinMaxHeight);
        }

        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}