using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [CustomNode("Directional Warp", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/directional_warp")]
    public class DirectionalWarpNode : InfiniteLandsNode, IHeightMapConnector, ICreateGrid
    {
        [Input] public HeightData HeightMap;
        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;
        public float Strength = 5;

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {           
            MeshSettings newSettings = meshSettings.ModifyResolution(acomulatedResolution);
            var pathData = currentBranch.manager.GetPathData(newSettings, GetNodesInInput(nameof(HeightMap)));
            int normalResolution = currentBranch.ApplyInputNormalMap(this, nameof(HeightMap), pathData.FinalResolution);
            if(IsAssigned(nameof(Mask)))
                normalResolution = Mathf.Max(normalResolution, currentBranch.AllocateInput(this, nameof(Mask), normalResolution));
            currentBranch.AllocateOutputs(this, acomulatedResolution);
            return normalResolution;
        }

        protected override bool ProcessNode(BranchData branch)
        {
            var factory = new DefaultBranchFactory<DirectionalWarpNode>(this, branch, nameof(HeightMap), nameof(Output));
            return AwaitableTools.CopyHeightMapFromBranchTo(branch, this, nameof(HeightMap), ref factory, out Output, nameof(Output));
        }

        public bool RecalculateIfDifferentSeed() => true;
        public AwaitableData<GridData> GetMeshGrid(BranchData branch, GridBranch parentPoints)
        {
            return new DWMeshGridData(branch, this, parentPoints);
        }
        public class DWMeshGridData : AwaitableData<GridData>
        {
            public GridData Result{get; private set;}

            BranchData branch;
            BranchData parentSettings;
            bool maskAssigned;

            private DirectionalWarpNode node;
            private int SubState = 0;
            private GridBranch parentGridData;
            private NormalMapData normalMapData;
            private HeightData heightBase;

            private InfiniteLandsNode originHeightNode;
            private string originHeightName;
            public DWMeshGridData(BranchData branch,DirectionalWarpNode node, GridBranch parentMaker){
                this.branch = branch;
                this.node = node;

                if(parentMaker == null){
                    Debug.LogError("Warp node requires parent point generator. Something went wrong");
                }

                maskAssigned = this.node.IsAssigned(nameof(Mask));      
                parentSettings = parentMaker.Branch;
                parentGridData = parentSettings.GetData<GridBranch>();
                node.ExtractNameNode(nameof(HeightMap), out originHeightName, out originHeightNode);
            }
            public bool ProcessData()
            {                
                if(SubState == 0){
                    if(!node.ProcessDependency(parentSettings, nameof(HeightMap))) return false;
                    if(maskAssigned && !node.ProcessDependency(parentSettings, nameof(Mask))) return false;
                    if(!parentGridData.ProcessGrid(out _)) return false;

                    node.TryGetInputData(parentSettings, out heightBase, nameof(HeightMap));          
                    SubState++;
                }
                if(SubState == 1){                   
                    HeightMapBranch heightBranch = parentSettings.GetData<HeightMapBranch>();
                    if(!heightBranch.GetNormalMapData(originHeightNode, originHeightName, heightBase, out normalMapData)) return false;
                    SubState++;
                }
                if(SubState == 2){
                    float targetStrength = node.Strength/branch.meshSettings.ratio;
                    HeightMapBranch heightBranch = parentSettings.GetData<HeightMapBranch>();
                    var map = heightBranch.GetMap();

                    GridData gridData = parentGridData.GetGridData();
                    NormalMapData normals = normalMapData;
                    JobHandle combined = JobHandle.CombineDependencies(gridData.jobHandle, normals.jobHandle);
                    var points = branch.GetData<ReturnableBranch>().GetData<float3>(heightBase.indexData.Length);

                    JobHandle finalJob;
                    if (maskAssigned)
                    {
                        node.TryGetInputData(parentSettings, out HeightData maskJob, nameof(Mask));          
                        JobHandle afterBoth = JobHandle.CombineDependencies(combined, maskJob.jobHandle);
                        finalJob = WarpPointsNormalMaskedJob.ScheduleParallel(gridData.grid, points, 
                            map, normals.NormalMap, normals.indexData, targetStrength, maskJob.indexData,
                            branch.meshSettings.Resolution, parentSettings.meshSettings.Resolution, afterBoth);
                    }
                    else{
                        finalJob = WarpPointsNormalMapJob.ScheduleParallel(gridData.grid, points, 
                            normals.NormalMap, normals.indexData, targetStrength,
                            branch.meshSettings.Resolution, parentSettings.meshSettings.Resolution, combined);
                    }
                    Result = new GridData(points, finalJob);
                    SubState++;
                }

                if(SubState == 3)
                    return true;
                else
                    return false;
            }
        }
    }
}