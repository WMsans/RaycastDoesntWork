using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands.UnityTerrain{
    [ExecuteAlways]
    public class UnityTerrainVegetation : ChunkProcessor<TerrainWithHeightResult>
    {
        public bool UseGPUInstancingInDetails;
        public float DetailTreshold = 1;
        private GameObject PrefabHolder;
        private readonly string HolderName = "Unity Prefabs Holder";
        protected override void InitializeProcessor()
        {
            GetPrefabHolder();
        }

        protected override void DisableProcessor()
        {
            AdaptiveDestroy(PrefabHolder);
        }
        
        void GetPrefabHolder(){
            if(PrefabHolder != null)
                return;
            
            PrefabHolder = RuntimeTools.FindOrCreateObject(HolderName, transform);
            PrefabHolder.SetActive(false);
        }

        protected override void OnProcessAdded(TerrainWithHeightResult chunkWithHeight)
        {
            var chunk = chunkWithHeight.chunk;
            var newTerrain = chunkWithHeight.terrainData;

            AssetData assetsData = chunkWithHeight.chunk.GetData<AssetData>();
            assetsData.AddProcessor(this);

            IEnumerable<(IHoldVegetation, int)> vegetationAssets = assetsData.assetWithIndex
                .Select(cpuAsset => (cpuAsset.Item1 as IHoldVegetation, cpuAsset.Item2))
                .Where(tuple => tuple.Item1 != null); 
            HandleObjects(vegetationAssets, chunk.meshSettings, assetsData, newTerrain);

            assetsData.RemoveProcessor(this);
        }

        private void HandleObjects(IEnumerable<(IHoldVegetation, int)> assets, MeshSettings meshSettings, AssetData assetsData, TerrainData terrainData){
            GetPrefabHolder();

            List<TreePrototype> prototypes = new List<TreePrototype>();
            List<DetailPrototype> details = new List<DetailPrototype>();
            List<int[,]> detailMaps = new List<int[,]>();

            List<TreeInstance> instances = new List<TreeInstance>();

            var rnd = new System.Random(meshSettings.Seed);
            if(assets.Any()){
                foreach(var assetPack in assets){
                    IHoldVegetation asset = assetPack.Item1;
                    var positionData = asset.GetPositionData();
                    var spawnMode = asset.GetSpawningMode();
                    var objectData = asset.GetObjectData();

                    float distanceBetweenItems = positionData.distanceBetweenItems;
                    if(distanceBetweenItems < DetailTreshold){
                        //Debug.LogWarningFormat("{0} has a distance between items of {1} units and is not supported. Minimum distance allowed is {2} unit", asset.name, distanceBetweenItems, 1);
                        DetailPrototype detail = new DetailPrototype();
                        if(spawnMode == IHoldVegetation.SpawnMode.GPUInstancing){
                            detail.prototype = FindOrLoadAssetLOD(asset, false);
                            detail.usePrototypeMesh = true;
                            detail.renderMode = DetailRenderMode.VertexLit;
                            detail.minHeight = positionData.minimumMaximumScale.x;
                            detail.maxHeight = positionData.minimumMaximumScale.y;
                            detail.minWidth = 1;
                            detail.maxWidth = 1;
                            detail.useInstancing = UseGPUInstancingInDetails;
                            details.Add(detail);
                            var detailMap = GenerateDetailMap(meshSettings.TextureResolution, assetsData.map, meshSettings, assetsData.MapPartLength*assetPack.Item2, positionData.distanceBetweenItems);
                            detailMaps.Add(detailMap);
                        }
                        continue;
                    }

                    int rowInstances = Mathf.CeilToInt(meshSettings.MeshScale/distanceBetweenItems);
                    TreePrototype prototype = new TreePrototype();

                    if(spawnMode == IHoldVegetation.SpawnMode.GPUInstancing)
                        prototype.prefab = FindOrLoadAssetLOD(asset, true);
                    else
                        prototype.prefab = objectData.gameObject;

                    for(int x = 0; x < rowInstances; x++){
                        for(int y = 0; y < rowInstances; y++){
                            Vector2 position = new Vector2(y, x)/rowInstances;
                            Vector2 rndm = new Vector2(rnd.Next(100)/100f, rnd.Next(100)/100f);
                            rndm = positionData.positionRandomness*(rndm-Vector2.one*0.5f)*2f;
                            rndm *= (distanceBetweenItems/meshSettings.MeshScale)/1.5f;
                            position += rndm;
                            float height = sampleSplatMap(assetsData.map, position, meshSettings, assetsData.MapPartLength*assetPack.Item2);
                            if(height > 0.1f){
                                TreeInstance instance = new TreeInstance();
                                instance.position = new Vector3(position.x,0,position.y);

                                float sizeRandom = Mathf.Lerp(positionData.minimumMaximumScale.x,positionData.minimumMaximumScale.y,rnd.Next(100)/100f);
                                instance.widthScale = sizeRandom;
                                instance.heightScale = sizeRandom;
                                instance.rotation = 2*Mathf.PI*rnd.Next(100)/100f;
                                instance.prototypeIndex = prototypes.Count;
                                instances.Add(instance);
                            }
                        }
                    }
                    prototypes.Add(prototype);
                }
                terrainData.treePrototypes = prototypes.ToArray();
                terrainData.detailPrototypes = details.ToArray();
                terrainData.SetTreeInstances(instances.ToArray(), true);
                terrainData.SetDetailResolution(meshSettings.TextureResolution, 8);
                for (int i = 0; i < detailMaps.Count; i++)
                {   
                    var detailMap = detailMaps[i];
                    terrainData.SetDetailLayer(0, 0, i, detailMap);
                }
            }
        }

        // Generate detail placement map
        private int[,] GenerateDetailMap(int resolution, NativeArray<float> ogMap, MeshSettings settings, int offset, float dist)
        {
            int[,] map = new int[resolution, resolution];
            float terrainSize = settings.MeshScale; // Terrain width/length in Unity units
            float cellSize = terrainSize / resolution; // Size of one detail map cell
            int resolutionPerPatch = 8; // Match the value in SetDetailResolution
            float patchSize = cellSize * resolutionPerPatch;
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float2 uv = new float2(y,x)/resolution;
                    float heightAtPoint = sampleSplatMap(ogMap, uv, settings, offset);
                    float instancesPerPatch = Mathf.Pow(patchSize / dist, 2) * heightAtPoint;
                    map[x, y] = Mathf.RoundToInt(instancesPerPatch);
                }
            }
            return map;
            
        }

        public float sampleSplatMap(NativeArray<float> map, Vector2 uv, MeshSettings settings, int offset){
            Vector2Int closest = Vector2Int.FloorToInt(math.saturate(uv)*settings.Resolution);
            int index = MapTools.VectorToIndex(new int2(closest.x, closest.y), settings.Resolution);
            return map[offset + index];

        }

        private GameObject FindOrLoadAssetLOD(IHoldVegetation asset, bool withGroup){
            var temp = RuntimeTools.FindOrCreateObject(asset.name, PrefabHolder.transform, out bool justCreated);
            if(justCreated){
                ArgumentsData argumentsData = asset.InitializeMeshes();
                if(withGroup){
                    var group = temp.AddComponent<LODGroup>();
                    MeshLOD[] CurrentLodS = argumentsData.Lods;
                    LOD[] lods = new LOD[CurrentLodS.Length];
                    for(int i = 0; i<lods.Length; i++){
                        var render = FindOrLoadAssetOne(CurrentLodS[i], temp.transform, i);
                        lods[i] = new LOD(1.0F / (i + 2),new Renderer[]{render});
                    }
                    group.SetLODs(lods);
                    group.RecalculateBounds();
                }
                else{
                    var lod = argumentsData.Lods[0];
                    var filter = temp.AddComponent<MeshFilter>();
                    var render = temp.AddComponent<MeshRenderer>();
                    filter.sharedMesh = lod.mesh;
                    render.materials = lod.materials;
                }
            }
            return temp;
        }

        

        private MeshRenderer FindOrLoadAssetOne(MeshLOD lod, Transform parent, int i){
            var lodValue = RuntimeTools.FindOrCreateObject(string.Format("LOD {0}", i), parent, out bool generated);
            if(generated){
                var filter = lodValue.AddComponent<MeshFilter>();
                var render = lodValue.AddComponent<MeshRenderer>();
                filter.sharedMesh = lod.mesh;
                render.materials = lod.materials;
                return render;
            }
            else
                return lodValue.GetComponent<MeshRenderer>();
                
        }
        
        protected override void OnProcessRemoved(TerrainWithHeightResult chunk){}
    }
}