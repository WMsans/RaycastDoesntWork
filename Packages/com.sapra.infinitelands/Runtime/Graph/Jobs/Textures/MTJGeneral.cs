using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct MTJGeneral : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<Color32> textureArray;
        [ReadOnly] NativeArray<float> MinMaxValue;

        [ReadOnly] NativeArray<float> globalArray;
        IndexAndResolution origin;
        int resolution;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, resolution, origin.Resolution);
            float current = globalArray[origin.IndexOffset + index];
            float colorValue = 0;
            float min = MinMaxValue[0];
            float max = MinMaxValue[1];

            if (max - min != 0)
            {
                colorValue = (current - min) / (max - min);
            }
            textureArray[i] = JobExtensions.toColor(saturate(colorValue));
        }

        public static JobHandle ScheduleParallel(NativeArray<Color32> textureArray, NativeArray<float> MinMaxValue,
            NativeArray<float> globalArray, IndexAndResolution origin,
            int resolution, JobHandle dependency) => new MTJGeneral(){
                textureArray = textureArray,
                globalArray = globalArray,
                MinMaxValue = MinMaxValue,
                origin = origin,
                resolution = resolution,
        }.ScheduleParallel(textureArray.Length, resolution, dependency);
    }
}