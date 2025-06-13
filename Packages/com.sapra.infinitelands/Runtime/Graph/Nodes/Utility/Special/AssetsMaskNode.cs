using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{
    [CustomNode("Assets Mask", docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/assetsmask.html", singleInstance = true)]
    public class AssetsMaskNode : InfiniteLandsNode
    {
        [Input, Hide, Disabled] public HeightData Input;
        [Input] public HeightData Mask;
        [Output, Hide] public HeightData Output;
        private Vector2 GetMinMaxValue(Vector2 InputMinMax) {
            return new Vector2(Mathf.Min(0, InputMinMax.x),
                Mathf.Max(0, InputMinMax.y));
        }

        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            JobHandle job = ApplyMaskJob<Zero>.ScheduleParallel(map,
                Mask.indexData, Input.indexData, targetSpace, Input.minMaxValue,

            JobHandle.CombineDependencies(Input.jobHandle, Mask.jobHandle));

            Output = new HeightData(job, targetSpace, GetMinMaxValue(Input.minMaxValue));
        }

        private struct Zero : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return value*saturate(mask);
            }
        }

    }
}