using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;

namespace sapra.InfiniteLands
{
    [CustomNode("Warp", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/filter/warp")]
    public class WarpNode : InfiniteLandsNode, IHeightMapConnector, ICreateGrid
    {
        [Input] public HeightData HeightMap;
        [Input] public HeightData Warp;
        [Input, Disabled] public HeightData Mask;
        [Output] public HeightData Output;

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {           
            MeshSettings newSettings = meshSettings.ModifyResolution(acomulatedResolution);
            var pathData = currentBranch.manager.GetPathData(newSettings, GetNodesInInput(nameof(HeightMap)));
            int finalResolution = pathData.FinalResolution;
            int warpMaxResolution = currentBranch.AllocateInput(this, nameof(Warp), finalResolution);
            if(IsAssigned(nameof(Mask)))
                warpMaxResolution = Mathf.Max(warpMaxResolution, currentBranch.AllocateInput(this, nameof(Mask), finalResolution));
            currentBranch.AllocateOutputs(this, acomulatedResolution);
            return warpMaxResolution;
        }
        public bool RecalculateIfDifferentSeed() => true;

        public AwaitableData<GridData> GetMeshGrid(BranchData settings, GridBranch parentMaker)
        {
            WarpNodeData warpNode = GenericPoolLight<WarpNodeData>.Get();
            warpNode.Reuse(settings, this, parentMaker);
            return warpNode;
        }

        protected override bool ProcessNode(BranchData branch)
        {
            var factory = new DefaultBranchFactory<WarpNode>(this, branch, nameof(HeightMap), nameof(Output));
            return AwaitableTools.CopyHeightMapFromBranchTo(branch, this, nameof(HeightMap), ref factory, out Output, nameof(Output));
        }
               
        public class WarpNodeData : AwaitableData<GridData>{
            public GridData Result{get; private set;}
            private WarpNode warpNode;
            
            private int SubState;

            private BranchData settings;
            private BranchData generationY;
            private BranchData generationX;
            bool maskAssigned;

            private GridBranch parentGridDataX;
            public void Reuse(BranchData settings, WarpNode warpNode, GridBranch parentMaker){
                this.warpNode = warpNode;

                maskAssigned = warpNode.IsAssigned(nameof(Mask));      

                if(parentMaker == null){
                    Debug.LogError("Warp node requires parent point generator. Something went wrong");
                }
                this.settings = settings;
                this.SubState = 0;

                generationX = parentMaker.Branch;
                generationY = BranchData.NewSeedSettings(1, generationX, warpNode.GetNodesInInput(nameof(Warp)));
                parentGridDataX = generationX.GetData<GridBranch>();
            }

            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!warpNode.ProcessDependency(generationX, nameof(Warp))) return false;
                    if(!warpNode.ProcessDependency(generationY, nameof(Warp))) return false;
                    if(maskAssigned && !warpNode.ProcessDependency(generationX, nameof(Mask))) return false;
                    if(!parentGridDataX.ProcessGrid(out _)) return false;
                    SubState++;
                }

                if(SubState == 1){
                    bool foundX = warpNode.TryGetInputData(generationX, out HeightData xData, nameof(Warp));
                    bool foundY = warpNode.TryGetInputData(generationY, out HeightData yData, nameof(Warp));

                    if(!foundX || !foundY){
                        Debug.LogError("Something went wrong");
                        return true;                
                    }

                    HeightMapBranch heightBranchX = generationX.GetData<HeightMapBranch>();
                    HeightMapBranch heightBranchY = generationY.GetData<HeightMapBranch>();
                    var map = heightBranchX.GetMap();
                    var variantMap = heightBranchY.GetMap();

                    IndexAndResolution warpXIndex = xData.indexData;
                    IndexAndResolution warpYIndex = yData.indexData;
                    GridData gridData = parentGridDataX.GetGridData();
                    
                    JobHandle dependancy = gridData.jobHandle;
                    var parentPoints = gridData.grid;
                    var points = settings.GetData<ReturnableBranch>().GetData<float3>(warpXIndex.Length);
                    JobHandle onceFinished = JobHandle.CombineDependencies(xData.jobHandle, yData.jobHandle, dependancy);

                    JobHandle finalJob;
                    if (maskAssigned)
                    {
                        warpNode.TryGetInputData(generationX, out HeightData maskJob, nameof(Mask));
                        JobHandle afterBoth = JobHandle.CombineDependencies(onceFinished, maskJob.jobHandle);
                        finalJob = WarpPointsMaskedJob.ScheduleParallel(parentPoints, points, 
                            map, variantMap, warpXIndex, warpYIndex, maskJob.indexData,
                            settings.meshSettings.Resolution, generationX.meshSettings.Resolution, afterBoth);
                    }
                    else{
                        finalJob = WarpPointsJob.ScheduleParallel(parentPoints, points, 
                            map, variantMap, warpXIndex, warpYIndex,
                            settings.meshSettings.Resolution, generationX.meshSettings.Resolution, onceFinished);
                    }

                    Result = new GridData(points, finalJob);
                    GenericPoolLight.Release(this);
                    SubState++;
                }

                return SubState == 2;
            }
        }
    }
}