using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using static sapra.InfiniteLands.Noise;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct RandomJob : IJobFor
    {
        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float4> globalMap;
        
        IndexAndResolution target;
        
        float2 FromTo;
        float3 offset;
        float frequency;
        int ogResolution;
        int seed;

        public void Execute(int i)
        {
            int reinterpreted = MapTools.RemapIndex(i*4, target.Resolution, ogResolution)/4;
            float4x3 pt = transpose(vertices[reinterpreted]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;

            var hash = SmallXXHash4.Seed(seed);
            float4 value = default(Random<Value>).GetNoise4(pt, hash, frequency, 0);

            globalMap[i + target.IndexOffset/4] = lerp(FromTo.x, FromTo.y, value);
        }
       
        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeArray<float4> globalMap,
            float2 FromTo, float3 offset, float size, 
            IndexAndResolution target, int ogResolution, JobHandle dependency) => new RandomJob()
        {
            globalMap = globalMap,
            target = target,
            FromTo = FromTo,
            vertices = vertices,
            offset = offset,
            ogResolution = ogResolution,
            frequency = 1f / size,
        }.ScheduleParallel(target.Length/4, target.Resolution, dependency);
    }
}