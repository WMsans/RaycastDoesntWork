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
    public struct BlurJob<T> : IJobFor where T : ChannelBlurFast
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;
        
        IndexAndResolution target;
        IndexAndResolution current;
        
        int EffectSize;
        float ExtraStrength;
        float averageMax;
        public void Execute(int x)
        {   
            float CT = 0;
            T blurInstance = default;
            for(int y = 0; y < target.Resolution+1; y++){
                int2 vector = blurInstance.Flip(int2(x, y));
                float average = blurInstance.BlurValue(globalMap, vector, target,current, EffectSize, ExtraStrength, averageMax, ref CT);
                int index = MapTools.VectorToIndex(vector, target.Resolution);
                globalMap[target.IndexOffset + index] = average;
            }
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, int EffectSize, float ExtraStrength, float averageMax,
            IndexAndResolution target, IndexAndResolution current, JobHandle dependency) => new BlurJob<T>(){
                globalMap = globalMap,
                EffectSize = EffectSize,
                ExtraStrength = ExtraStrength,
                target = target,
                current = current,
                averageMax = averageMax,
            }.ScheduleParallel(target.Resolution+1, 32, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurJobMasked<T> : IJobFor where T : ChannelBlurFast
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;
        
        IndexAndResolution target;
        IndexAndResolution current;
        IndexAndResolution original;
        IndexAndResolution mask;
        
        int EffectSize;
        float ExtraStrength;
        float averageMax;
        public void Execute(int x)
        {           
            float CT = 0;
            T blurInstance = default;

            for(int y = 0; y < target.Resolution+1; y++){
                int2 vector = blurInstance.Flip(int2(x, y));

                float average = blurInstance.BlurValue(globalMap, vector, target,current, EffectSize, ExtraStrength, averageMax, ref CT);
                int index = MapTools.VectorToIndex(vector, target.Resolution);

                int maskIndex = MapTools.RemapIndex(index, target.Resolution, mask.Resolution);
                int originalIndex = MapTools.RemapIndex(index, target.Resolution, original.Resolution);

                float maskValue = globalMap[mask.IndexOffset + maskIndex];
                float originalValue = globalMap[original.IndexOffset + originalIndex]; 
                globalMap[target.IndexOffset + index] = lerp(originalValue, average,maskValue);
            }
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, int EffectSize, float ExtraStrength, float averageMax,
            IndexAndResolution target, IndexAndResolution current, IndexAndResolution mask,IndexAndResolution original, JobHandle dependency) => new BlurJobMasked<T>(){
                globalMap = globalMap,
                EffectSize = EffectSize,
                ExtraStrength = ExtraStrength,
                target = target,
                current = current,
                averageMax = averageMax,
                mask = mask,
                original = original,
            }.ScheduleParallel(target.Resolution+1, 32, dependency);
    }


    public interface ChannelBlurFast{
        public float BlurValue(NativeArray<float> globalMap, int2 vector, IndexAndResolution target, IndexAndResolution current, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal);
        public int2 Flip(int2 val);
    }

    public static class BlurMethods{
                    
        public static float getValueAtIndex(NativeArray<float> globalMap, int2 coord, IndexAndResolution target, IndexAndResolution current){
            int index = MapTools.GetFlatIndex(coord, target.Resolution, current.Resolution);
            index = Mathf.Clamp(index, 0, current.Length-1);
            return globalMap[current.IndexOffset + index];
        }

        public static float BlurValue(NativeArray<float> globalMap, int2 vector, IndexAndResolution target, IndexAndResolution current, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal, bool isXJob){
            int primary = isXJob ? vector.x : vector.y;
            int secondary = isXJob ? vector.y : vector.x;

            if(secondary == 0)
            {
                currentTotal = 0;
                for(int z = -EffectSize; z <= EffectSize; z++){
                    int n = secondary+z;
                    currentTotal += getValueAtIndex(globalMap,makeCoord(primary, n, isXJob),target, current);
                    int s = (int)sign(z);
                    if(z == -EffectSize || z == EffectSize)
                    {
                        float next = getValueAtIndex(globalMap,makeCoord(primary, n+s, isXJob),target, current);
                        currentTotal += next*ExtraStrength;
                    }
                }
            }
            else{
                //Fully remove the previous edge
                currentTotal -= getValueAtIndex(globalMap,makeCoord(primary, secondary-EffectSize-2, isXJob),target, current)*ExtraStrength;
                
                //Remove the full edge and at the half one
                currentTotal -= getValueAtIndex(globalMap,makeCoord(primary, secondary-EffectSize-1, isXJob),target, current);
                currentTotal += getValueAtIndex(globalMap,makeCoord(primary, secondary-EffectSize-1, isXJob),target, current)*ExtraStrength;
                
                //Remove the half edge, and add it fully
                currentTotal -= getValueAtIndex(globalMap,makeCoord(primary, secondary+EffectSize, isXJob),target, current)*ExtraStrength;
                currentTotal += getValueAtIndex(globalMap,makeCoord(primary, secondary+EffectSize, isXJob),target, current);
                
                //Add the new edge
                currentTotal += getValueAtIndex(globalMap,makeCoord(primary, secondary+EffectSize+1, isXJob),target, current)*ExtraStrength;
            }
            
            return currentTotal/averageMax;
        }

        private static int2 makeCoord(int primary, int secondary, bool isXJob) {
            return isXJob ? new int2(primary, secondary) : new int2(secondary, primary);
        }

    }
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurItJobX : ChannelBlurFast{
        public float BlurValue(NativeArray<float> globalMap, int2 vector, IndexAndResolution target, IndexAndResolution current, 
            int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal){
            return BlurMethods.BlurValue(globalMap,  vector, target, current, EffectSize, ExtraStrength, averageMax,ref currentTotal, true);
        }

        public int2 Flip(int2 val) => new int2(val.x, val.y);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct BlurItJobY : ChannelBlurFast{
        public float BlurValue(NativeArray<float> globalMap, int2 vector, IndexAndResolution target, IndexAndResolution current, int EffectSize, float ExtraStrength, float averageMax, ref float currentTotal){
            return BlurMethods.BlurValue(globalMap,  vector, target, current, EffectSize, ExtraStrength, averageMax,ref currentTotal, false);
        }
        
        public int2 Flip(int2 val) => new int2(val.y, val.x);

    }
}