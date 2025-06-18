using UnityEngine;

namespace sapra.InfiniteLands.MeshProcess{
    public readonly struct MeshProcess{
        public readonly MeshSettings meshSettings;
        public readonly MeshMaker.MeshType meshType;
        public readonly int CoreGridSpacing;
        public readonly float NormalReduceThreshold;
        public readonly TerrainConfiguration terrainConfiguration;
        public readonly Bounds ObjectBounds;
        public readonly WorldFinalData worldFinalData;
        public MeshProcess(ChunkData chunk, WorldFinalData worldFinalData,
            MeshMaker.MeshType meshType, int CoreGridSpacing, float NormalReduceThreshold)
        {
            this.worldFinalData = worldFinalData;
            this.meshSettings = chunk.meshSettings;
            this.terrainConfiguration = chunk.terrainConfig;
            this.ObjectBounds = chunk.ObjectSpaceBounds;
            this.meshType = meshType;
            this.CoreGridSpacing = CoreGridSpacing;
            this.NormalReduceThreshold = NormalReduceThreshold;

        }
    }
}