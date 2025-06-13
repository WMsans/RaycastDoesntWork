using UnityEngine;
using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    [CustomNode("Grid", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/input/grid")]
    public class GridPointsNode : InfiniteLandsNode, IProcessPoints
    {
        [Output] public PointInstance Output;
        [Min(40)] public float GridSize = 140;

        public string processorID => guid;
        public AwaitableData<List<PointTransform>> ProcessDataSpace(PointInstance currentPoints, PointGenerationSettings pointSettings)
        {
            GridPoints findByTag = GenericPoolLight<GridPoints>.Get();
            findByTag.Reuse(pointSettings);
            return findByTag;
        }

        protected override void Process(BranchData branch)
        {
            PointManager manager = branch.GetGlobalData<PointManager>();
            GridBranch gridBranch = branch.GetData<GridBranch>();
            Output = manager.GetPointInstance(this, gridBranch.GridCreator, GridSize, null, branch.meshSettings.Seed);
        }
        
        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }

        private class GridPoints : AwaitableData<List<PointTransform>>
        {
            public List<PointTransform> Result{get; private set;}
            public GridPoints(){
                Result = new();
            }
            public bool ProcessData() => true;

            public void Reuse(PointGenerationSettings pointSettings){
                Result.Clear();
                Result.Add(new PointTransform(){
                    Position = pointSettings.Origin,
                    Scale = 1,
                    YRotation = 0
                });   
                GenericPoolLight.Release(this);
            }
        }

    }
}