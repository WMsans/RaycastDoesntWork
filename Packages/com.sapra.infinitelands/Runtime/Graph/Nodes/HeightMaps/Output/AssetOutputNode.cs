using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Asset Output", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/output/assetoutput", synonims = new string[]{"Vegetation", "Textures","Pack"})]
    public class AssetOutputNode : InfiniteLandsNode, ILoadAsset, ISetAsset
    {
        public InfiniteLandsAsset Asset;

        [Input] public HeightData Density;
        [Output, Hide] public HeightData Output;

        [field: SerializeField] public ILoadAsset.Operation action{get; private set;}

        public IEnumerable<IAsset> GetAssets(){
            if(Asset is IHoldManyAssets manyAssets){
                return manyAssets.GetAssets();
            }else{
                return new[]{Asset};
            }
        }
        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, out Density, nameof(Density));
        }

        protected override void Process(BranchData branch)
        {
            Output = Density;
        }

        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue(branch, Output, nameof(Output));
        }

        public bool AssetExists(IAsset asset) => GetAssets().Contains(asset);
        public void SetAsset(InfiniteLandsAsset asset) => Asset = asset;
        public HeightData GetOutput(BranchData settings, IAsset asset){
            TryGetOutputData(settings, out Output, nameof(Output));
            return Output;
        }
    }
}