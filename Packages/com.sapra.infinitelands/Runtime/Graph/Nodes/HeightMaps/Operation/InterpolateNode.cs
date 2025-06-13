using Unity.Jobs;
using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [CustomNode("Interpolate", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/interpolate")]
    public class InterpolateNode : InfiniteLandsNode
    {
        [Input] public HeightData InputAt0;
        [Input] public HeightData InputAt1;
        [Input] public HeightData Strength;

        [Output] public HeightData Output;
        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out InputAt0, nameof(InputAt0));
            TryGetInputData(branch, out InputAt1, nameof(InputAt1));
            TryGetInputData(branch, out Strength, nameof(Strength));
        }
        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();            
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            
            Vector2 minMax = new Vector2(Mathf.Min(InputAt0.minMaxValue.x, InputAt1.minMaxValue.x),
                Mathf.Max(InputAt0.minMaxValue.y, InputAt1.minMaxValue.y));

            JobHandle job = InterpolateJob.ScheduleParallel(map,
                Strength.indexData, InputAt0.indexData, InputAt1.indexData, targetSpace, Strength.minMaxValue,
                 JobHandle.CombineDependencies(InputAt1.jobHandle, Strength.jobHandle,
                    InputAt0.jobHandle));

            Output = new HeightData(job, targetSpace, minMax);
        }
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
    }
}