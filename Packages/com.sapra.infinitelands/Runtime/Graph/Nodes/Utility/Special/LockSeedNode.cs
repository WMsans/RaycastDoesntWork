using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Windows;

namespace sapra.InfiniteLands
{
    [CustomNode("Lock Seed", docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/lockseed")]
    public class LockSeedNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public object Input;
        [Output(match_type_name: nameof(Input))] public object Output;
        public int Seed;

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {
            //var inputType = GetTypeOfInput(nameof(Input));
            var currntMaxRes = acomulatedResolution;
            if (true)//inputType.Equals(typeof(HeightData)))
            {
                /*                 currentBranch.AllocateInputDirect(nameof(Output), this, acomulatedResolution);
                                currentBranch.AllocateOutputs(this, acomulatedResolution); */
                currntMaxRes = currentBranch.AllocateInput(this, nameof(Input), acomulatedResolution);
                currentBranch.AllocateOutputSpace(this, nameof(Output), acomulatedResolution);
            }
            return currntMaxRes;
        }
        public override bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            var dataCreated = base.TryGetOutputData<object>(branch, out var result, fieldName, listIndex);
            data = (T)result;
            return dataCreated;
        } 
        protected override bool ProcessNode(BranchData branch)
        {
            var reuser = new GenericNodeReuser<LockSeedNode, LockSeedData>(branch, this);
            return AwaitableTools.WaitNode<LockSeedData, object, GenericNodeReuser<LockSeedNode, LockSeedData>>(branch, ref reuser, out Output, this, nameof(Output));
        }

        public class LockSeedData : AwaitableData<object>, INodeReusable<LockSeedNode>
        {
            public object Result { get; private set; }
            LockSeedNode node;

            BranchData newBranch;
            BranchData ogBranch;

            int SubState;
            Type targetType;

            public void Reuse(LockSeedNode node, BranchData originalData)
            {
                this.node = node;
                ogBranch = originalData;
                SubState = 0;
                newBranch = BranchData.NewSeedSettings(node.Seed, originalData, node.GetNodesInInput(nameof(Input)), true);
                targetType = RuntimeTools.GetTypeFromInputField(nameof(node.Input), node, node.Graph);
            }

            public bool ProcessData()
            {
                if (SubState == 0)
                {
                    if (!node.ProcessDependency(newBranch, nameof(Input))) return false;
                    SubState++;
                }

                if (SubState == 1)
                {
                    if (targetType == typeof(HeightData))
                    {
                        if (!node.TryGetInputData<HeightData>(newBranch, out var heightData, nameof(node.Input)))
                        {
                            Debug.LogError("something went wrong");
                            return true;
                        }

                        HeightMapBranch fromHeightBranch = newBranch.GetData<HeightMapBranch>();
                        var from = fromHeightBranch.GetMap();
                        HeightMapBranch heightBranch = ogBranch.GetData<HeightMapBranch>();
                        var targetSpace = heightBranch.GetAllocationSpace(node, nameof(Output), out var to);
                        JobHandle job = CopyToFrom.ScheduleParallel(to, from,
                            targetSpace, heightData.indexData,
                            heightData.jobHandle);

                        Result = new HeightData(job, targetSpace, heightData.minMaxValue);
                    }
                    else
                    {
                        if (!node.TryGetInputData<object>(newBranch, out var result, nameof(node.Input)))
                            Debug.Log("sad");
                        Result = result;
                    }

                    SubState++;
                }

                return SubState == 2;
            }
        }
    }
}
