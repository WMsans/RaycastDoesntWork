using Unity.Jobs;

namespace sapra.InfiniteLands{
    public readonly struct TextureProcess{
        public readonly AssetData assetData;
        public readonly WorldFinalData finalData;
        public readonly MeshSettings meshSettings;
        public readonly TerrainConfiguration terrainConfig;
        public readonly ExportedMultiResult SplatMaps;
        public readonly ExportedMultiResult HeightMap;
        public readonly JobHandle job;

        public TextureProcess(AssetData assetData, WorldFinalData finalData,
            MeshSettings meshSettings, TerrainConfiguration terrainConfig, 
            ExportedMultiResult SplatMaps, ExportedMultiResult HeightMap, JobHandle job)
        {
            this.assetData = assetData;
            this.finalData = finalData;
            this.meshSettings = meshSettings;
            this.terrainConfig = terrainConfig;
            this.SplatMaps = SplatMaps;
            this.HeightMap = HeightMap;
            this.job = job;
        }
    }
}