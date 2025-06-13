using UnityEngine;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Clamp", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/clamp", synonims = new string[]{"Saturate"})]
    public class ClampNode : InfiniteLandsNode
    {
        public Vector2 ClampMinMax = new Vector2(0, 1);

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
            
            JobHandle job = ClampJob.ScheduleParallel(map, Input.indexData, targetSpace,
                ClampMinMax, Input.jobHandle);

            Output = new HeightData(job, targetSpace, new Vector2(
                    Mathf.Max(Input.minMaxValue.x, ClampMinMax.x),
                    Mathf.Min(Input.minMaxValue.y, ClampMinMax.y)));        
        }
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}