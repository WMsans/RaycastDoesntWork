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
    public struct CalculateChannels : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;

        [ReadOnly] NativeArray<float3> normalMap;
        IndexAndResolution normalIndex;
        IndexAndResolution targetX;
        IndexAndResolution targetZ;

        float4x4 localToWorld;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, targetX.Resolution, normalIndex.Resolution);
            float3 normal = normalMap[index];
            float4 inWorldSpace = mul(float4(normal.xyz, 1.0f),localToWorld);
            globalMap[targetX.IndexOffset + i] = inWorldSpace.x;
            globalMap[targetZ.IndexOffset + i] = inWorldSpace.z;
        }
        
        public static JobHandle ScheduleParallel(
            NativeArray<float3> normalMap, IndexAndResolution normalIndex,
            NativeArray<float> globalMap, float4x4 localToWorld,
            IndexAndResolution targetIndexX,IndexAndResolution targetIndexZ,
            JobHandle dependency) => new CalculateChannels()
        {
            localToWorld = localToWorld,
            normalMap = normalMap,
            normalIndex = normalIndex,
            targetX = targetIndexX,
            targetZ = targetIndexZ,
            globalMap = globalMap,
        }.ScheduleParallel(targetIndexX.Length, targetIndexX.Resolution, dependency);
    }



    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct GetCavityJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;

        IndexAndResolution target;
        IndexAndResolution channelIndexX;
        IndexAndResolution channelIndexZ;

        int EffectSize;
        float ExtraStrength;
        public void Execute(int i)
        {
            int2 vectorIndex = MapTools.IndexToVector(i, target.Resolution);
            float finalCountour = calculateDataRedMatrix(vectorIndex) + calculateDataBlueMatrix(vectorIndex);
            finalCountour = (finalCountour+2f)/4f;
            globalMap[target.IndexOffset + i] = saturate(finalCountour);//(clamp(finalCountour,-totalAmount*2,totalAmount*2)+totalAmount*2)/(totalAmount*4f);
        }

        float calculateDataRedMatrix(int2 index){
            float result = 0;
            for(int dx = -EffectSize; dx <= EffectSize; dx++){
                int sng = (int)sign(dx);
                float currentNormal = getValueAtIndex(index.x+dx, index.y, channelIndexX)*sign(dx);
                result += currentNormal;
                if(dx == -EffectSize || dx == EffectSize){
                    float next = getValueAtIndex(index.x+dx+sng, index.y, channelIndexX)*sng;
                    result += next*ExtraStrength;
                }
            }
            result /= EffectSize+ExtraStrength;

            return result;
        }
        float calculateDataBlueMatrix(int2 index){          
            float result = 0;
            for(int dy = -EffectSize; dy <= EffectSize; dy++){
                int sng = (int)sign(dy);
                float currentNormal = getValueAtIndex(index.x, index.y+dy, channelIndexZ)*sign(dy);
                result += currentNormal;

                if(dy == -EffectSize || dy == EffectSize){
                    float next = getValueAtIndex(index.x, index.y+dy+sng, channelIndexZ)*sng;
                    result += next*ExtraStrength;
                }
                
            }
            result /= EffectSize+ExtraStrength;

            return result;
        }
        
        
        float getValueAtIndex(int x, int y, IndexAndResolution channel){
            int index = MapTools.GetFlatIndex(int2(x, y), target.Resolution, channel.Resolution);
            return globalMap[channel.IndexOffset + index];
        }


        public static JobHandle ScheduleParallel(NativeArray<float> globalMap,      
            IndexAndResolution target, IndexAndResolution channelIndexX, IndexAndResolution channelIndexZ,
            int EffectSize, float ExtraStrength,
            JobHandle dependency) => new GetCavityJob()
        {
            globalMap = globalMap,
            target = target,
            channelIndexX = channelIndexX,
            EffectSize = EffectSize,
            ExtraStrength = ExtraStrength,
            channelIndexZ = channelIndexZ,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}