using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    // Asset is provided that can be later retrieved
    public interface ILoadAsset : IOutput
    {
        public enum Operation{Add, Remove};
        public Operation action{get;}
        public HeightData GetOutput(BranchData settings, IAsset asset);
        public bool AssetExists(IAsset asset);
        public IEnumerable<IAsset> GetAssets();

    }
}