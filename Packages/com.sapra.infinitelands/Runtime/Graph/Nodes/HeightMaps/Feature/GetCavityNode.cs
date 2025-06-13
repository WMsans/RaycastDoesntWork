using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Get Cavity", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/features/getcavity")]
    public class GetCavityNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Min(0.01f)] public float CavitySize = 10;

        [Input] public HeightData Input;
        [Output] public HeightData Output;

        public GenerationModeNode FeatureMode = GenerationModeNode.Default;

        private const string channelX = "ChannelX";
        private const string channelZ = "ChannelZ";

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {
            int Size = Mathf.CeilToInt(meshSettings.ratio*CavitySize);
            int maxResolution = currentBranch.ApplyInputPadding(this, nameof(Input), Size, acomulatedResolution);
            maxResolution = currentBranch.ApplyInputNormalMap(this, nameof(Input), maxResolution);

            currentBranch.AllocateOutputs(this, acomulatedResolution);
            currentBranch.AllocateOutputSpace(this, channelX, acomulatedResolution);
            currentBranch.AllocateOutputSpace(this, channelZ, acomulatedResolution);

            return maxResolution;
        }

        protected override bool ProcessNode(BranchData branch)
        {
            var reuser = new GenericNodeReuser<GetCavityNode,GetCavityData>(branch, this);
            return AwaitableTools.WaitNode<GetCavityData, HeightData, GenericNodeReuser<GetCavityNode,GetCavityData>>(branch, ref reuser, out Output, this, nameof(Output));
        }

        public class GetCavityData : AwaitableData<HeightData>, INodeReusable<GetCavityNode>
        {
            public HeightData Result{get; private set;}
            private BranchData branch;
            private GetCavityNode node;

            private int SubState;
            private HeightMapBranch heightBranch;
            private HeightData inputData;
            private NormalMapData normals;

            private InfiniteLandsNode originHeightNode;
            private string originHeightName;

            public void Reuse(GetCavityNode node, BranchData branch)
            {
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

                    float Size = Mathf.Min(branch.meshSettings.ratio*node.CavitySize, MapTools.MaxIncreaseSize);
                    int EffectSize = Mathf.Max(1, Mathf.FloorToInt(Size));
                    float ExtraSize = Mathf.Clamp01(Size-EffectSize);

                    IndexAndResolution channelTargetX = heightBranch.GetAllocationSpace(node, channelX);
                    IndexAndResolution channelTargetZ = heightBranch.GetAllocationSpace(node, channelZ);

                    channelTargetX = IndexAndResolution.OffsetResolution(channelTargetX, MapTools.IncreaseResolution(targetSpace.Resolution,-1));
                    channelTargetZ = IndexAndResolution.OffsetResolution(channelTargetZ, MapTools.IncreaseResolution(targetSpace.Resolution,-1));

                    Matrix4x4 targetMatrix = branch.GetVectorMatrix(node.FeatureMode);

                    JobHandle separateMaps = CalculateChannels.ScheduleParallel(normals.NormalMap, normals.indexData,
                        map,targetMatrix, channelTargetX, channelTargetZ,
                        normals.jobHandle);
                    
                    JobHandle calculateCavities = GetCavityJob.ScheduleParallel(map, targetSpace, channelTargetX, channelTargetZ,
                        EffectSize, ExtraSize,  separateMaps);

                    Result = new HeightData(calculateCavities, targetSpace, new Vector2(0,1));
                    SubState++;
                }

                return SubState == 3;
            }
        }
    }
}