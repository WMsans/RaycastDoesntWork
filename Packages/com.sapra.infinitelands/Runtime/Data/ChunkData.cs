using UnityEngine;

namespace sapra.InfiniteLands
{
    public class ChunkData
    {
        public Vector3Int ID{get; private set;}
        public TerrainConfiguration terrainConfig{get; private set;}
        public MeshSettings meshSettings{get; private set;}
        
        public Bounds ObjectSpaceBounds{get; private set;}
        public Bounds WorldSpaceBounds{get; private set;}

        private TreeData treeData;
        private TreeData separateExtraTree;
        private TypeStore<ProcessableData> typeStore;
        private bool InScope;        
        public void Return()
        {
            if (treeData != null)
            {
                treeData.CloseTree();
                treeData = null;
            }
            if (separateExtraTree != null)
            {
                separateExtraTree.CloseTree();
                separateExtraTree = null;
            }
            if (typeStore != null)
            {
                var processable = typeStore.GetManyDataRaw();
                foreach (var ProcessableData in processable)
                {
                    ProcessableData.RemoveProcessor(typeStore);
                }
                typeStore.Release();
                GenericPoolLight.Release(typeStore);
                typeStore = null;
            }
            InScope = false;
        }

        public T GetData<T>(bool required = false) where T : ProcessableData{
            if(typeStore == null || !InScope){
                Debug.LogError("Getting data from an already returned chunk. Too late!");
                return default;
            }
            return typeStore.GetData<T>(required);
        }

        public void Reuse(TerrainConfiguration _terrainConfig, MeshSettings _meshSettings, TreeData treeData, TypeStore<ProcessableData> typeStore, TreeData separateExtraTree){
            InScope = true;
            this.treeData = treeData;
            this.separateExtraTree = separateExtraTree;

            this.ID = _terrainConfig.ID;
            this.terrainConfig = _terrainConfig;
            this.meshSettings = _meshSettings;
            this.typeStore = typeStore;
            
            var processable = typeStore.GetManyDataRaw();
            foreach(var ProcessableData in processable){
                ProcessableData.AddProcessor(typeStore);
            }

            var worldFinalData = typeStore.GetData<WorldFinalData>();
            float MinValue = worldFinalData.ChunkMinMax[0];
            float MaxValue = worldFinalData.ChunkMinMax[1];

            float verticalOffset = (MaxValue + MinValue)/2f;
            float displacement = MaxValue - MinValue;
            WorldSpaceBounds = new Bounds(terrainConfig.Position+verticalOffset*Vector3.up, new Vector3(_meshSettings.MeshScale, displacement, _meshSettings.MeshScale));
            ObjectSpaceBounds = new Bounds(verticalOffset*Vector3.up, new Vector3(_meshSettings.MeshScale, displacement, _meshSettings.MeshScale));
        }
    }
}