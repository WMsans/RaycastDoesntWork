using UnityEngine;
using Unity.Jobs;

using System;
namespace sapra.InfiniteLands
{
    [CustomNode("Blur", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/blur")]
    public class BlurNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public HeightData HeightMap;
        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;

        [Min(0.01f)]public float BlurSize = .01f;
        private const string ChannelX = "channelX";

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {
            int Size = Mathf.CeilToInt(meshSettings.ratio*BlurSize);
            int heightResolution = currentBranch.ApplyInputPadding(this, nameof(HeightMap), Size, acomulatedResolution);
            if(IsAssigned(nameof(Mask)))
                heightResolution = Mathf.Max(heightResolution, currentBranch.AllocateInput(this, nameof(Mask), acomulatedResolution));
            currentBranch.AllocateOutputs(this, acomulatedResolution);
            currentBranch.AllocateOutputSpace(this, ChannelX, acomulatedResolution);
            return heightResolution;
        }

        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out HeightMap, nameof(HeightMap));
            TryGetInputData(branch, out Mask, nameof(Mask));
        }

        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);

            float Size =  Mathf.Min(branch.meshSettings.ratio*BlurSize, MapTools.MaxIncreaseSize);
            int EffectSize = Mathf.Max(1, Mathf.FloorToInt(Size));
            float ExtraSize = Mathf.Clamp01(Size-EffectSize);
            float averageMa = (EffectSize+ExtraSize)*2+1;

            IndexAndResolution current = HeightMap.indexData;
            IndexAndResolution channelX = heightBranch.GetAllocationSpace(this, ChannelX);
            channelX = IndexAndResolution.OffsetResolution(channelX, current.Resolution);
            JobHandle job;
            
            bool maskAssigned = IsAssigned(nameof(Mask));      
            if(maskAssigned){
                JobHandle onceCompleted = JobHandle.CombineDependencies(Mask.jobHandle, HeightMap.jobHandle);
                JobHandle checkX = BlurJob<BlurItJobX>.ScheduleParallel(map, EffectSize, ExtraSize, averageMa, channelX, current, onceCompleted);
                job = BlurJobMasked<BlurItJobY>.ScheduleParallel(map, EffectSize, ExtraSize, averageMa, targetSpace, channelX, Mask.indexData, current, checkX);
            }
            else{
                JobHandle checkX = BlurJob<BlurItJobX>.ScheduleParallel(map, EffectSize, ExtraSize, averageMa, channelX, current, HeightMap.jobHandle);
                job = BlurJob<BlurItJobY>.ScheduleParallel(map, EffectSize, ExtraSize, averageMa, targetSpace, channelX, checkX);
            }

            Output = new HeightData(job, targetSpace, HeightMap.minMaxValue);
        }
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}