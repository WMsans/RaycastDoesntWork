using Unity.Jobs;
using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [CustomNode("Get Slope", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/features/getslope")]
    public class GetSlopeNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public HeightData Input;
        [Output] public HeightData Output;

        public GenerationModeNode FeatureMode = GenerationModeNode.Default;

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {
            int maxResolution = currentBranch.ApplyInputNormalMap(this, nameof(Input), acomulatedResolution);
            currentBranch.AllocateOutputs(this, acomulatedResolution);
            return maxResolution;        
        }

        protected override bool ProcessNode(BranchData branch)
        {   
            var reuser = new GetSlopeDataReuser(branch, this);
            return AwaitableTools.WaitNode<GetSlopeData, HeightData, GetSlopeDataReuser>(branch, ref reuser, out Output, this, nameof(Output));
        }

        private struct GetSlopeDataReuser : IReuseObject<GetSlopeData>
        {
            private BranchData branch;
            private GetSlopeNode node;
            public GetSlopeDataReuser(BranchData branchData, GetSlopeNode node){
                this.branch = branchData;
                this.node = node;
            }
            public void Reuse(GetSlopeData instance)
            {
                instance.Reuse(branch, node);
            }
        }

        public class GetSlopeData : AwaitableData<HeightData>
        {
            public HeightData Result{get; private set;}
            private BranchData branch;
            private GetSlopeNode node;

            private int SubState;
            private HeightMapBranch heightBranch;
            private HeightData inputData;
            private NormalMapData normals;

            private InfiniteLandsNode originHeightNode;
            private string originHeightName;
            public void Reuse(BranchData branch, GetSlopeNode node){
                this.branch = branch;
                this.node = node;
                this.SubState = 0;
                heightBranch = branch.GetData<HeightMapBranch>();
                node.ExtractNameNode(nameof(Input), out originHeightName, out originHeightNode);
            }
            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!node.ProcessDependency(branch, nameof(node.Input))) return false;         
                    node.TryGetInputData(branch, out inputData, nameof(node.Input));          
                    SubState++;
                }
                if(SubState == 1){
                    if(!heightBranch.GetNormalMapData(originHeightNode, originHeightName, inputData, out normals)) return false;
                    SubState++;
                }

                if(SubState == 2){
                    var targetSpace = heightBranch.GetAllocationSpace(node, nameof(Output), out var map);  
                    Matrix4x4 targetMatrix = branch.GetVectorMatrix(node.FeatureMode);
                    JobHandle job = GetSlope.ScheduleParallel(normals.NormalMap, normals.indexData,
                        map, targetMatrix,
                        targetSpace,
                        normals.jobHandle);
                    Result = new HeightData(job, targetSpace, new Vector2(0,1));
                    SubState++;
                }

                return SubState == 3;
            }
        }
    }
}