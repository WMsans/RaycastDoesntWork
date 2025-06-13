using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace sapra.InfiniteLands{
    public interface IExportTextures{
        public string description{get;}
        public int GetTextureResolution();
        public void SetExporterResolution(int resolution);
        public ExportedMultiResult GenerateHeightTexture(WorldFinalData worldData);
        public ExportedMultiResult GenerateDensityTextures(IEnumerable<(IAsset, int)> assets, AssetData data);
        
        public void DestroyTextures(Action<UnityEngine.Object> Destroy);
    }
}
