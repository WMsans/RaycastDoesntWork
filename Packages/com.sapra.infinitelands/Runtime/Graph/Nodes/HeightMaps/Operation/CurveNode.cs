using UnityEngine;
using Unity.Jobs;

using UnityEngine.Serialization;

namespace sapra.InfiniteLands
{
    [CustomNode("Curve", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/curve")]
    public class CurveNode : InfiniteLandsNode
    {
        [ShowIf(nameof(showBoundedCurve)), BoundedCurve]
        public AnimationCurve Function =
            new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 0, 0) });
        [HideIf(nameof(showBoundedCurve))]
        public AnimationCurve GlobalCurve =
            new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(100, 100, 0, 0) });

        public enum CurveMode { Normalized, Global }
        public CurveMode curveMode = CurveMode.Normalized;
        private bool showBoundedCurve => curveMode == CurveMode.Normalized;

        [Input] public HeightData Input;
        [Output] public HeightData Output;
        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out Input, nameof(Input));
        }

        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var factory = GetFactory();
            SampledAnimationCurve curve = branch.GetOrCreateGlobalData<SampledAnimationCurve, SampledAnimationCurveFactory>(this.guid, ref factory);
            var targetSpace = heightBranch.GetAllocationSpace(this, nameof(Output), out var map);
            JobHandle job = RemapCurveJob.ScheduleParallel(map, Input.indexData,
                targetSpace, curve, Input.minMaxValue,
                Input.jobHandle);

            Output = new HeightData(job, targetSpace, Input.minMaxValue);
        }
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }
        
        public SampledAnimationCurveFactory GetFactory()
        {
            switch (curveMode)
            {
                case CurveMode.Global:
                    return new SampledAnimationCurveFactory(GlobalCurve, false);
                default:
                    return new SampledAnimationCurveFactory(Function, true);

            }
        }
    }
}