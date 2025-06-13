using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct PositionJob : IJobFor
    {
        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float4> globalMap;
        IndexAndResolution target;
        
        float3 offset;
        int ogResolution;
        bool XValue;

        public void Execute(int i)
        {
            int reinterpreted = MapTools.RemapIndex(i*4, target.Resolution, ogResolution)/4;
            float4x3 pt = transpose(vertices[reinterpreted]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;

            globalMap[i + target.IndexOffset/4] = XValue ? pt.c0 : pt.c2;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeArray<float4> globalMap,
            float3 offset, bool XValue,
            IndexAndResolution target, int ogResolution, JobHandle dependency) => new PositionJob()
        {
            globalMap = globalMap,
            target = target,
            vertices = vertices,
            offset = offset,
            XValue = XValue,
            ogResolution = ogResolution,
        }.ScheduleParallel(target.Length/4, target.Resolution, dependency);
    }
}