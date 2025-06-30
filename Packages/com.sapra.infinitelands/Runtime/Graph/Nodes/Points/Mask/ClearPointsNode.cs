using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace sapra.InfiniteLands
{
    [CustomNode("Clear", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/mask/clear")]
    public class ClearPointsNode : InfiniteLandsNode, IProcessPoints
    {
        [Input] public PointInstance Points;
        [Input] public HeightData Mask;
        [Output] public PointInstance Output;

        public string processorID => guid;

        protected override bool WaitingDependencies(BranchData branch)
        {
            return ProcessDependency(branch, nameof(Points));
        }

        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out Points, nameof(Points));
        }

        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints,  PointGenerationSettings pointSettings)
        {
            ClearPointsData clearing = GenericPoolLight<ClearPointsData>.Get();
            clearing.Reuse(this, currentPoints, pointSettings);
            return clearing;
        }
        protected override void Process(BranchData branch)
        {
            PointManager manager = branch.GetGlobalData<PointManager>();
            GridBranch gridBranch = branch.GetData<GridBranch>();
            Output = manager.GetPointInstance(this, gridBranch.GridCreator, Points.GridSize, Points, branch.meshSettings.Seed);
        }
        
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }

        private class ClearPointsData : AwaitableData<List<PointTransform>>
        {
            public List<PointTransform> Result{get; private set;}

            private List<AwaitingPoint> GeneratedHeights = new();
            private int SubState = 0;
            private ClearPointsNode clearPointsNode;
            private PointInstance previousPoints;
            private PointInstance currentPoints;
            private PointGenerationSettings pointSettings;
            private int Seed;

            private struct AwaitingPoint{
                public HeightAtPoint heightData;
                public PointTransform pointInstance;
            }
            public void Reuse(ClearPointsNode clearPointsNode, PointInstance currentPoints, PointGenerationSettings pointSettings){
                this.pointSettings = pointSettings;
                this.previousPoints = currentPoints.PreviousInstance;
                this.currentPoints = currentPoints;
                this.Seed = currentPoints.Seed;
                this.clearPointsNode = clearPointsNode;
                GeneratedHeights.Clear();
                if(Result == null)
                    Result = new List<PointTransform>();
                else
                    Result.Clear();

                SubState = 0;
            }

            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!previousPoints.GetAllPoints(pointSettings, out List<PointTransform> foundPoints)) return false;
                    foreach(var point in foundPoints){
                        MeshSettings meshSettings = new MeshSettings(){
                            Resolution = 3,
                            MeshScale = 50,
                            Seed = Seed,
                        };
                        var results = currentPoints.GetDataAtPoint(clearPointsNode, nameof(Mask), point.Position, meshSettings);
                        GeneratedHeights.Add(new AwaitingPoint(){
                            heightData = results,
                            pointInstance = point,
                        });
                    }
                    SubState++;
                }

                if(SubState == 1){
                    var applydensity = new ApplyDensity(Result);
                    if(AwaitableTools.IterateOverItems(GeneratedHeights, ref applydensity)){
                        SubState++;
                        GenericPoolLight.Release(this);
                    }
                }
                return SubState == 2;
            }

            private struct ApplyDensity : ICallMethod<AwaitingPoint>
            {
                List<PointTransform> Result;
                public ApplyDensity(List<PointTransform> result){
                    this.Result = result;
                }
                public bool Callback(AwaitingPoint value)
                {
                    if(!value.heightData.ProcessData()) return false;
                    var result = value.heightData.Result;
                    if(result > 0.1f)
                        Result.Add(value.pointInstance);
                    return true;
                }
            }
        }

    }
}