using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct FunctionJob<T> : IJobFor where T : struct, IFunctionJob
    {
        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float4> globalMap;
        
        IndexAndResolution target;
        
        float2 FromTo;
        float3 offset;
        float frequency;
        float YRotation;
        int ogResolution;

        public void Execute(int i)
        {
            int reinterpreted = MapTools.RemapIndex(i*4, target.Resolution, ogResolution)/4;
            float4x3 pt = transpose(vertices[reinterpreted]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;

            float4x3 position = RotateY(pt, math.radians(YRotation+90));
            globalMap[i + target.IndexOffset/4] = lerp(FromTo.x, FromTo.y, default(T).GetValue(position, frequency, YRotation));
        }

        // Helper method to rotate points around Y axis
        private float4x3 RotateY(float4x3 points, float angle)
        {
            float cosA = math.cos(angle);
            float sinA = math.sin(angle);

            float4 newX = cosA * points.c0 - sinA * points.c2;
            float4 newY = points.c1;
            float4 newZ = sinA * points.c0 + cosA * points.c2;

            return float4x3(newX, newY, newZ);
        }


        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeArray<float4> globalMap,
            float2 FromTo, float3 offset, float period, float YRotation,
            IndexAndResolution target, int ogResolution, JobHandle dependency) => new FunctionJob<T>()
        {
            globalMap = globalMap,
            target = target,
            FromTo = FromTo,
            vertices = vertices,
            offset = offset,
            YRotation = YRotation,
            ogResolution = ogResolution,
            frequency = 1f / period,
        }.ScheduleParallel(target.Length/4, target.Resolution, dependency);
    }


    public interface IFunctionJob
    {
        public abstract float4 GetValue(float4x3 position, float frequency, float YRotation);
    }

    public struct FSquare : IFunctionJob
    {
        public float4 GetValue(float4x3 position, float frequency, float YRotation)
        {
            float4 xValue = position.c0;
            float4 normalized = xValue * frequency;
            return 2 * floor(normalized) - floor(2 * normalized) + 1;
        }
    }

    public struct FTriangle : IFunctionJob
    {
        public float4 GetValue(float4x3 position, float frequency, float YRotation)
        {
            float4 xValue = position.c0;
            float4 normalized = xValue * frequency + .25f;
            return 2 * abs(normalized - floor(normalized + .5f));
        }
    }

    public struct FSine : IFunctionJob
    {
        public float4 GetValue(float4x3 position, float frequency, float YRotation)
        {
            float4 xValue = position.c0;
            float4 normalized = xValue * frequency;
            return (sin(normalized * 2 * PI) + 1F) / 2f;
        }
    }

    public struct FSawTooth : IFunctionJob
    {
        public float4 GetValue(float4x3 position, float frequency, float YRotation)
        {
            float4 xValue = position.c0;
            float4 normalized = xValue * frequency;
            return normalized - floor(normalized);
        }
    }
}