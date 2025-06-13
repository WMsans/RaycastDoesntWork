using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Height Output", canCreate = false, canDelete = false, startCollapsed = true, docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/output/heightoutput")]
    public class HeightOutputNode : InfiniteLandsNode, ICreateGrid
    {
        [Input] public HeightData HeightMap;
        [Output, HideIf(nameof(HideOutput)), Disabled] public HeightData FinalTerrain;
        public bool HideOutput => Graph.GetType().Equals(typeof(WorldTree));
        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out HeightMap, nameof(HeightMap));
        }
        protected override void Process(BranchData branch)
        {           
            FinalTerrain = HeightMap;
        }
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, FinalTerrain, nameof(FinalTerrain));
        }
        public AwaitableData<GridData> GetMeshGrid(BranchData settings, GridBranch parentPoints)
        {
            var heightOutput = GenericPoolLight<HeightOutputData>.Get();
            heightOutput.Reuse(settings);
            return heightOutput;
        }

        public bool RecalculateIfDifferentSeed() => false;

        public class HeightOutputData : AwaitableData<GridData>
        {
            public GridData Result{get; private set;}
            private int SubState = 0;
            public void Reuse(BranchData branch){    

                HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
                ReturnableBranch returnableBranch = branch.GetData<ReturnableBranch>();
                int pointsLength = heightBranch.GetFinalLength();
                var points = returnableBranch.GetData<float3>(pointsLength);
                var result = SimpleGridMap.ScheduleParallel(points,
                        branch.meshSettings.Resolution, branch.meshSettings.MeshScale, default);
                
                Result = new GridData(points, result);
                SubState = 0;
            }
            public bool ProcessData(){
                if(SubState == 0){
                    GenericPoolLight.Release(this);
                    SubState++;
                }
                return true;
            }
        }
    }
}