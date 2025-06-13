using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct ConstantJob : IJobFor
    {
        float Value;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float4> globalMap;

        IndexAndResolution target;
        public void Execute(int i)
        {
            globalMap[i + target.IndexOffset/4] = Value;
        }

        public static JobHandle ScheduleParallel(NativeArray<float4> globalMap, 
            float Value, IndexAndResolution target,
            JobHandle dependency) => new ConstantJob()
        {
            globalMap = globalMap,
            target = target,
            Value = Value,
        }.ScheduleParallel(target.Length/4, target.Resolution/4, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct ConstantJobSlow : IJobFor
    {
        float Value;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float> globalMap;

        IndexAndResolution target;
        
        public void Execute(int i)
        {
            globalMap[i + target.IndexOffset] = Value;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            float Value, IndexAndResolution target,
            JobHandle dependency) => new ConstantJobSlow()
        {
            globalMap = globalMap,
            target = target,
            Value = Value,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}