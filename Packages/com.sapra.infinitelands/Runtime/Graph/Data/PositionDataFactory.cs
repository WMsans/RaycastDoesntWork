using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct NewPositionSettingsFactory : IFactory<BranchData>
    {
        Vector2 offset;
        BranchData original;
        InfiniteLandsNode[] startingNodes;
        public NewPositionSettingsFactory(Vector2 offset, BranchData original, InfiniteLandsNode[] startingNodes){
            this.offset = offset;
            this.original = original;
            this.startingNodes = startingNodes;
        }
        public BranchData Create()
        {
            TerrainConfiguration newPosition = new TerrainConfiguration(original.terrain);
            newPosition.Position -= new Vector3(offset.x, 0, offset.y);

            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, original.meshSettings, newPosition, startingNodes);
            BranchData.InitializeBranch(settings, original);
            return settings;
        }
    }
}