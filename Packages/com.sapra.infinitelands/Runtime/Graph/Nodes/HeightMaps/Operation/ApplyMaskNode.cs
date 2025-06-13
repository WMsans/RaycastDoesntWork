using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections;
using UnityEngine.Windows;

namespace sapra.InfiniteLands
{
    [CustomNode("Apply Mask", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/applymask")]
    public class ApplyMaskNode : InfiniteLandsNode
    {
        public enum ToValue{Minimum, Maximum, Zero}

        public ToValue ValueAtZero = ToValue.Minimum;
        [Input] public HeightData Input;
        [Input] public HeightData Mask;

        [Output] public HeightData Output;
        protected override bool ProcessNode(BranchData branch)
        {
            var reuser = new GenericNodeReuser<ApplyMaskNode, ApplyMaskData>(branch, this);
            return AwaitableTools.WaitNode<ApplyMaskData, HeightData,GenericNodeReuser<ApplyMaskNode, ApplyMaskData>>(branch, ref reuser, out Output, this, nameof(Output));
        }

        private struct Minimum : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return lerp(minMax.x, value, mask);
            }
        }

        private struct Maximum : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return lerp(minMax.y, value, mask);
            }
        }

        private struct Zero : MaskMultiplyMode
        {
            public float GetValue(float2 minMax, float value, float mask)
            {
                return value*mask;
            }
        }

        private class ApplyMaskData : AwaitableData<HeightData>, INodeReusable<ApplyMaskNode>
        {
            public HeightData Result{get; private set;}

            private BranchData branch;
            private ApplyMaskNode node;
            private HeightMapBranch heightBranch;
            private HeightData Mask;

            private JobHandle waitingForMask;
            private int SubState;
            private ToValue ValueAtZero = ToValue.Minimum;

            private float2 OriginMinMax;

            public void Reuse(ApplyMaskNode node, BranchData settings){
                this.node = node;
                this.branch = settings;
                this.ValueAtZero = node.ValueAtZero;
                this.SubState = 0;
                heightBranch = settings.GetData<HeightMapBranch>();
            }

            public bool ProcessData()
            {
                if (SubState == 0)
                {
                    if (!AwaitableTools.ValidateIsInsideThreshold(branch, node, nameof(Mask), 0, out AwaitableTools.MaskResult maskResult)) return false;

                    Mask = maskResult.MaskData;
                    if (maskResult.ContainsData)
                        SubState = 21;
                    else
                        SubState = 11;
                }

                if (SubState == 11)
                {
                    if (heightBranch.GetTheoreticalMinMax(branch, node, nameof(node.Input), out OriginMinMax))
                        SubState++;
                }   

                var targetSpace = heightBranch.GetAllocationSpace(node, nameof(Output), out var map);
                if(SubState == 12){
                    float value;
                    switch (ValueAtZero){
                        case ToValue.Maximum:
                            value = OriginMinMax.y;
                            break;
                        case ToValue.Minimum:
                            value = OriginMinMax.x;
                            break;
                        default:
                            value = 0;
                            break;
                    }
                    JobHandle job = ConstantJobSlow.ScheduleParallel(map, value, targetSpace, waitingForMask);
                    Result = new HeightData(job, targetSpace, GetMinMaxValue(OriginMinMax));
                    SubState = 30;
                }

                if(SubState == 21){
                    if(!node.ProcessDependency(branch, nameof(Input))) return false;
                    SubState++;
                }

                if(SubState == 22){
                    JobHandle job;
                    node.TryGetInputData(branch, out HeightData input, nameof(node.Input));
                    switch (ValueAtZero){
                        case ToValue.Maximum:
                            job = ApplyMaskJob<Maximum>.ScheduleParallel(map,
                                Mask.indexData, input.indexData, targetSpace, input.minMaxValue,input.jobHandle);
                                break;
                        case ToValue.Zero:
                            job = ApplyMaskJob<Zero>.ScheduleParallel(map,
                                Mask.indexData, input.indexData, targetSpace, input.minMaxValue, input.jobHandle);
                                break;
                        default:
                            job = ApplyMaskJob<Minimum>.ScheduleParallel(map,
                                Mask.indexData, input.indexData, targetSpace, input.minMaxValue, input.jobHandle);
                                break;
                    }
                    Result = new HeightData(job, targetSpace, GetMinMaxValue(input.minMaxValue));
                    SubState = 30;
                }
               
                return SubState == 30;
            }

            private Vector2 GetMinMaxValue(Vector2 InputMinMax) {
                if(ValueAtZero == ToValue.Zero)
                    return new Vector2(Mathf.Min(0, InputMinMax.x),
                        Mathf.Max(0, InputMinMax.y));
                return InputMinMax;
            }
        }
    }
}