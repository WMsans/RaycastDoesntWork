using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [CustomNode("Scale", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/scale")]
    public class ScaleNode : InfiniteLandsNode, ICreateGrid
    {
        public enum ScaleMode{OnlyPoints, OnlyHeight, Both}
        [Input] public HeightData Input;
        [Output] public HeightData Output;

        public bool RecalculateIfDifferentSeed() => false;
        [Min(0.01f)] public float Amount = 1;
        public ScaleMode Mode = ScaleMode.Both;

        private bool ScalesHeight => Mode == ScaleMode.Both || Mode == ScaleMode.OnlyHeight;
        private bool ScalesPoints => Mode == ScaleMode.Both || Mode == ScaleMode.OnlyPoints;

        public AwaitableData<GridData> GetMeshGrid(BranchData settings, GridBranch parentMaker)
        {
            ScaleNodeData scaleNode = GenericPoolLight<ScaleNodeData>.Get();
            scaleNode.Reuse(settings, this, parentMaker);
            return scaleNode;
        }

        protected override bool ProcessNode(BranchData branch)
        {
            var factory = new DefaultBranchFactory<ScaleNode>(this, branch, nameof(Input), nameof(Output));
            return AwaitableTools.CopyHeightMapFromBranchTo(branch, this, nameof(Input), ref factory, out Output, nameof(Output), ScalesHeight ? Amount : 1);
        }
               
        public class ScaleNodeData : AwaitableData<GridData>{
            public GridData Result{get; private set;}
            private ScaleNode scaleNode;
            
            private int SubState;

            private BranchData settings;

            private GridBranch parentGridData;
            private BranchData parentSettings;
            public void Reuse(BranchData settings, ScaleNode warpNode, GridBranch parentMaker)
            {
                this.scaleNode = warpNode;


                if (parentMaker == null)
                {
                    Debug.LogError("Warp node requires parent point generator. Something went wrong");
                }
                this.settings = settings;
                this.SubState = 0;

                parentSettings = parentMaker.Branch;
                parentGridData = parentSettings.GetData<GridBranch>();
            }

            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!parentGridData.ProcessGrid(out _)) return false;
                    SubState++;
                }

                if(SubState == 1){
                    GridData gridData = parentGridData.GetGridData();                    
                    JobHandle dependancy = gridData.jobHandle;
                    var parentPoints = gridData.grid;

                    HeightMapBranch heightBranch = settings.GetData<HeightMapBranch>();
                    ReturnableBranch returnableBranch = settings.GetData<ReturnableBranch>();
                    int pointsLength = heightBranch.GetFinalLength();
                    var points = returnableBranch.GetData<float3>(pointsLength);

                    JobHandle finalJob = ScaleJob.ScheduleParallel(parentPoints, points, 
                            scaleNode.ScalesPoints ? 1f/scaleNode.Amount : 1, parentSettings.terrain.Position,
                            settings.meshSettings.Resolution, parentSettings.meshSettings.Resolution, dependancy);

                    Result = new GridData(points, finalJob);
                    GenericPoolLight.Release(this);
                    SubState++;
                }

                return SubState == 2;
            }
        }
    }
}