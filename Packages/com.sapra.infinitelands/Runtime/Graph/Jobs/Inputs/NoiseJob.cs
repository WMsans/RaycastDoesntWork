using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using static sapra.InfiniteLands.Noise;


namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct NoiseJob<T> : IJobFor where T : struct, INoise
    {
        NoiseSettings settings;

        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float4> globalMap;

        IndexAndResolution target;

        float3 offset;
        int seed;
        int resolution;

        public void Execute(int i)
        {
            int reinterpreted = MapTools.RemapIndex(i*4, target.Resolution, resolution)/4;
            float4x3 pt = transpose(vertices[reinterpreted]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;
            float4 result = GetFractalNoise<T>(pt, seed, settings);
            globalMap[i + target.IndexOffset/4] = result;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeArray<float4> globalMap,
            NoiseSettings settings, float3 offset, int ogResolution,
            IndexAndResolution target,
            int seed, JobHandle dependency) => new NoiseJob<T>()
        {
            offset = offset,
            vertices = vertices,
            globalMap = globalMap,
            target = target,
            settings = settings,
            resolution = ogResolution,
            seed = seed,
        }.ScheduleParallel(target.Length/4, target.Resolution/4, dependency);
    }
}