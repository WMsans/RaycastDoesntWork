using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct TextureJob : IJobFor
    {
        [ReadOnly] NativeArray<float3> vertices;
        [ReadOnly] NativeArray<float> textureColorData;
        int textureResolution;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float> globalMap;
        IndexAndResolution target;
        
        float3 offset;
        int ogResolution;
        float Size;
        float2 minMaxValue;

        public void Execute(int i)
        {
            // Remap index and get position
            int reinterpreted = MapTools.RemapIndex(i, target.Resolution, ogResolution);
            float3 position = vertices[reinterpreted];
            
            // Apply offset
            position.x += offset.x;
            position.z += offset.z;

            // Convert position to texture UV coordinates [0,1] based on Size
            float2 uv = (float2(position.xz) + Size * 0.5f) / Size; // Center at (0,0)
            
            // Scale UV to texture resolution
            int2 texCoord = (int2)math.clamp(uv * textureResolution, 0, textureResolution - 1);
            
            // Convert 2D coordinate to 1D index
            int textureIndex = MapTools.VectorToIndex(texCoord, textureResolution-1);
            float textureValue = 0;
            if(textureIndex < textureColorData.Length)
                textureValue = textureColorData[textureIndex];

            globalMap[i + target.IndexOffset] = math.lerp(minMaxValue.x, minMaxValue.y, textureValue);
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> vertices, NativeArray<float> globalMap,
            NativeArray<float> textureColorData, int textureResolution, float3 offset, float2 minMaxValue,
            IndexAndResolution target, int ogResolution, float size, JobHandle dependency) => new TextureJob()
        {
            globalMap = globalMap,
            target = target,
            vertices = vertices,
            offset = offset,
            minMaxValue = minMaxValue,
            textureColorData = textureColorData,
            textureResolution = textureResolution,
            ogResolution = ogResolution,
            Size = size
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}