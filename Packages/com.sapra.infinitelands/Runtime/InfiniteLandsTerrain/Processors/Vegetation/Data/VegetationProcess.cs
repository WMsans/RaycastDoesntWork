using Unity.Jobs;

namespace sapra.InfiniteLands{
    public readonly struct VegetationProcess
    {
        public readonly AssetData assetData;
        public readonly WorldFinalData worldFinalData;
        public readonly JobHandle job;

        public readonly ExportedMultiResult VegetationSplatMap;
        public readonly ExportedMultiResult HeightMap;
        public readonly TerrainConfiguration TerrainConfiguration;
        public readonly MeshSettings MeshSettings;
        public VegetationProcess(AssetData data,WorldFinalData worldFinalData, ExportedMultiResult VegetationSplatMap, ExportedMultiResult HeightMap, 
            TerrainConfiguration TerrainConfiguration, MeshSettings MeshSettings)
        {
            this.assetData = data;
            this.TerrainConfiguration = TerrainConfiguration;
            this.MeshSettings = MeshSettings;
            this.worldFinalData = worldFinalData;
            job = JobHandle.CombineDependencies(VegetationSplatMap.job, HeightMap.job);
            this.VegetationSplatMap = VegetationSplatMap;
            this.HeightMap = HeightMap;
        }
    }
}