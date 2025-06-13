using Unity.Jobs;
using UnityEngine;

using System;
using Unity.Mathematics;
using Unity.Collections;
using static sapra.InfiniteLands.Noise;

namespace sapra.InfiniteLands
{
    [CustomNode("Perlin Noise", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/perlin_noise.html")]
    public class PerlinNoiseNode : InfiniteLandsNode
    {
        public enum PerlinType{PerlinValue, Perlin}
        public PerlinType NoiseType = PerlinType.Perlin;
        public Vector2 MinMaxHeight = new Vector2(0, 1);
        [Min(0.001f)] public float TileSize = 100;
        [Min(1)] public int Octaves = 1;
        public Vector3 Rotation;

        [ShowIf(nameof(octavesEnabled))][Range(1,10)]public int Lacunarity = 2;
        [ShowIf(nameof(octavesEnabled))][Range(0f, 1f)] public float Persistence = .5f;
        public bool RidgeMode;
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
            NoiseSettings noiseSettings = getsettings();           
            int indexOffset = GetRandomIndex();
            float maxOctaves = Mathf.Pow(int.MaxValue, 1f/Lacunarity);
            noiseSettings.octaves = Mathf.Max(1,Mathf.Min(noiseSettings.octaves, Mathf.FloorToInt(maxOctaves)));

            GridBranch gridBranch = branch.GetData<GridBranch>();

            GridData gridData = gridBranch.GetGridData();
            NativeArray<float3x4> vectorized = gridData.grid.Reinterpret<float3x4>(sizeof(float)*3);
            JobHandle dependancy = gridData.jobHandle;

            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            var targetMap = map.Reinterpret<float4>(sizeof(float));
            targetSpace.UpdateLength(branch.meshSettings.Resolution);
            JobHandle jobHandle;
            switch (NoiseType)
            {
                case PerlinType.PerlinValue:
                    jobHandle = NoiseJob<Perlin2D<Value>>.ScheduleParallel(vectorized,                        
                        targetMap, noiseSettings, branch.terrain.Position, branch.meshSettings.Resolution, targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
                case PerlinType.Perlin:
                    jobHandle = NoiseJob<Perlin2D<Perlin>>.ScheduleParallel(vectorized,                        
                        targetMap, noiseSettings, branch.terrain.Position, branch.meshSettings.Resolution,
                        targetSpace, 
                        branch.meshSettings.Seed + indexOffset,
                        dependancy);
                        break;
                default:
                    jobHandle = NoiseJob<Perlin2D<Perlin>>.ScheduleParallel(vectorized, 
                        targetMap, noiseSettings, branch.terrain.Position, branch.meshSettings.Resolution, targetSpace, 
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