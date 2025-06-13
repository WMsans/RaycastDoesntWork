using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Select Mask", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/mask/selectmask", synonims = new string[]{"Selector"})]
    public class SelectMaskNode : InfiniteLandsNode
    {
        [MinMax(0,1)] public Vector2 Range = new Vector2(0, 1);
        [Range(0, 1)] public float BlendFactor = .1f;

        [Input] public HeightData Input;
        [Output] public HeightData Output;
        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out Input, nameof(Input));
        }
        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();           
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);

            JobHandle job;
            float2 range = new float2(0, 1);
            if(math.all(Input.minMaxValue == range)){
                job = RangeSelectorJob.ScheduleParallel(map, Input.indexData, targetSpace,
                    Range, BlendFactor, 
                    Input.jobHandle);
            }
            else{
                job = NormalizeRangeSelectorJob.ScheduleParallel(map, Input.indexData, targetSpace,
                    Range, BlendFactor, Input.minMaxValue,  
                    Input.jobHandle);
            }

            Output = new HeightData(job, targetSpace, range);
        }
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}