using UnityEngine;
using Unity.Jobs;

namespace sapra.InfiniteLands
{
    [CustomNode("Move Origin", docs ="https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/moveorigin")]
    public class MoveOriginNode : InfiniteLandsNode, IHeightMapConnector
    {
        public Vector2 NewPosition;

        [Input] public HeightData Input;
        [Output] public HeightData Output;

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {           
            currentBranch.AllocateOutputs(this, acomulatedResolution);
            return acomulatedResolution;
        }

        protected override bool ProcessNode(BranchData branch)
        {
            var positionSettings = BranchData.NewPositionSettings(NewPosition, branch, GetNodesInInput(nameof(Input)));
            return AwaitableTools.CopyHeightMapFromBranchTo(branch, this, nameof(Input), ref positionSettings, out Output, nameof(Output));
        }
    }
}