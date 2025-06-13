using UnityEngine;

namespace sapra.InfiniteLands.MeshProcess{
    public readonly struct MeshProcess{
        public readonly MeshSettings meshSettings;
        public readonly TerrainConfiguration terrainConfiguration;
        public readonly Bounds ObjectBounds;
        public readonly WorldFinalData worldFinalData;
        public MeshProcess(ChunkData chunk, WorldFinalData worldFinalData){
            this.worldFinalData = worldFinalData;
            this.meshSettings = chunk.meshSettings;
            this.terrainConfiguration = chunk.terrainConfig;
            this.ObjectBounds = chunk.ObjectSpaceBounds;                
        }
    }
}