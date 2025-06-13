using UnityEngine;

namespace sapra.InfiniteLands.UnityTerrain{
    public class UnityTerrainChunk : ChunkControl
    {
        public Terrain terrain;
        public TerrainCollider terrainCollider;
        public string GetGraphName() => infiniteLands.graph.name;

        private IGenerate<TerrainWithTextures> finalTerrain;

        public override void UnsubscribeEvents()
        {
            if(finalTerrain != null)
                finalTerrain.onProcessDone -= OnMeshGenerated;  
        }

        protected override void DisableIt()
        {
            if(terrain != null)
                terrain.terrainData = null;
        }

        public void OnMeshGenerated(TerrainWithTextures request){
            if(!request.ID.Equals(config.ID))
                    return;
            
            if(terrain == null)
                return;
            
            var chunk = request.chunk;
            float meshScale = chunk.meshSettings.MeshScale;
            WorldFinalData worldFinalData = chunk.GetData<WorldFinalData>();
            float vertical = worldFinalData.MinMaxHeight.x;
            terrain.terrainData = request.terrainData;
            Vector3 ps = terrain.transform.localPosition;
            ps.y = vertical;
            ps.x = -meshScale/2f;
            ps.z = -meshScale/2f;
            terrain.transform.localPosition = ps;
            terrain.materialTemplate = request.groundMaterial;
            terrainCollider.terrainData = request.terrainData;
        }

        
        protected override void CleanVisuals()
        {
            terrain.terrainData = null;
        }

        public override void UpdateVisuals(bool enabled)
        {
            terrain.enabled = enabled;
        }

        public override bool VisualsDone() => terrain.terrainData != null;

        protected override void InitializeChunk()
        {
            terrain = GetComponentInChildren<Terrain>();
            GameObject sub;
            if(terrain == null)
                sub = RuntimeTools.FindOrCreateObject("Terrain", transform);
            else
                sub = terrain.gameObject;

            terrain = GetOrAddComponent(ref terrain, sub);
            terrainCollider = GetOrAddComponent(ref terrainCollider, sub);
            finalTerrain = infiniteLands.GetInternalComponent<IGenerate<TerrainWithTextures>>();

            if(finalTerrain != null)
                finalTerrain.onProcessDone += OnMeshGenerated;
        }
    }
}