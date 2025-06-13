using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Constant", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/input/constant")]
    public class ConstantNode : InfiniteLandsNode
    {
        public float Value;
        [Output] public HeightData Output;

        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();

            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            var targetMap = map.Reinterpret<float4>(sizeof(float));
            targetSpace.UpdateLength(branch.meshSettings.Resolution);
            JobHandle job = ConstantJob.ScheduleParallel(targetMap, Value,
                    targetSpace, default);

            Output = new HeightData(job, targetSpace, new Vector2(Value - 0.001f, Value));
        }

        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}