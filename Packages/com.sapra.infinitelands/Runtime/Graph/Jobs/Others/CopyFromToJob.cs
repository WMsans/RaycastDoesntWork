using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{        
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CopyToFrom : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> targetGlobalMap;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float> originalGlobalMap;

        IndexAndResolution original;
        IndexAndResolution target;
        public void Execute(int i)
        {            
            int index = MapTools.RemapIndex(i, target.Resolution, original.Resolution);
            targetGlobalMap[target.IndexOffset+i] = originalGlobalMap[original.IndexOffset+index];
        }

        public static JobHandle ScheduleParallel(NativeArray<float> targetGlobalMap, NativeArray<float> originalGlobalMap,
            IndexAndResolution target, IndexAndResolution original, JobHandle dependency) => new CopyToFrom()
        {
            targetGlobalMap = targetGlobalMap,
            originalGlobalMap = originalGlobalMap,
            original = original,
            target = target,

        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}