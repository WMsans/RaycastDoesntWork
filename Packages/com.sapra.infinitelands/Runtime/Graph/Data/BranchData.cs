using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class BranchData : StringObjectStore<object>
    {
        public TreeData treeData{get; private set;}
        public int branchID{get; private set;}
        public TerrainConfiguration terrain{get; private set;}
        public MeshSettings meshSettings;

        public InfiniteLandsNode[] StartingNodes{get; private set;}
        public bool isClosed{get; private set;}
        private Dictionary<int, NodeStore> NodeGeneratedData = new();
        private List<NodeStore> RequestedStores = new();
        public void Reuse(TreeData treeSettings, 
            MeshSettings meshSettings, TerrainConfiguration terrain, InfiniteLandsNode[] startingNodes)
        {
            this.branchID = treeSettings.AddBranch(this);
            this.meshSettings = meshSettings;
            this.terrain = terrain;
            this.treeData = treeSettings;
            this.StartingNodes = startingNodes;
            isClosed = false;
            RequestedStores.Clear();
            NodeGeneratedData.Clear();
            lastNode = null;
            lastStore = null;
            Reuse();
        }

        public void CloseBranch(){
            if(isClosed) return;

            foreach(var val in RequestedStores){
                val.Release();
                GenericPoolLight.Release(val);
            }

            foreach(var item in treeData.BranchClosers){
                item.CloseBranch(this);
            }
            GenericPoolLight.Release(this);
            Release();
            isClosed = true;
        }

        #region Getting Data
        private InfiniteLandsNode lastNode;
        private NodeStore lastStore;

        public NodeStore GetNodeStore(InfiniteLandsNode node)
        {
            if (lastNode == node)
                return lastStore;
            if (!NodeGeneratedData.TryGetValue(node.small_index, out NodeStore store))
            {
                store = GenericPoolLight<NodeStore>.Get();
                store.Reuse();
                RequestedStores.Add(store);
                NodeGeneratedData.Add(node.small_index, store);
            }
            lastNode = node;
            lastStore = store;
            return store;
        }

        public T GetGlobalData<T>(bool required = true){
            return treeData.GlobalStore.GetData<T>(required);
        }
        
        public TResult GetOrCreateGlobalData<TResult, TFactory>(string key, ref TFactory FactoryMaker)
            where TFactory : struct, IFactory<TResult>
        {
            return treeData.GlobalStore.GetOrCreateData<TResult, TFactory>(key, ref FactoryMaker);
        }
        #endregion

        #region Helper Methods
        
        public static BranchData NewSeedSettings(int seedOffset, BranchData original, InfiniteLandsNode[] startingNodes, bool absolute = false){
            MeshSettings newSettings = original.meshSettings;
            if(absolute)
                newSettings.Seed = seedOffset;
            else
                newSettings.Seed += seedOffset;

            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, newSettings, original.terrain, startingNodes);
            InitializeBranch(settings, original);
            return settings;
        }


        public static NewPositionSettingsFactory NewPositionSettings(Vector2 offset, BranchData original, InfiniteLandsNode[] startingNodes){
            return new NewPositionSettingsFactory(offset, original, startingNodes);
        } 

        public static BranchData NewChildBranch(MeshSettings newSettings, TerrainConfiguration newTerrain, BranchData original, InfiniteLandsNode[] startingNodes){
            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, newSettings, newTerrain, startingNodes);
            InitializeBranch(settings, original);
            return settings;
        } 
        
        public static BranchData NewBranch(TreeData tree, MeshSettings meshSettings, TerrainConfiguration terrain, ICreateGrid gridCreator, InfiniteLandsNode[] startingNodes)
        {
            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(tree, meshSettings, terrain, startingNodes);
            settings.AddData(gridCreator);
            InitializeBranch(settings);
            return settings;
        }

        public static void InitializeBranch(BranchData currentBranch, BranchData previousBranch = null){
            foreach(var item in currentBranch.treeData.BranchInitializers){
                item.InitializeBranch(currentBranch, previousBranch);
            }
        }

        #endregion

        public Matrix4x4 GetVectorMatrix(GenerationModeNode nodeMode){
            Vector3 up;
            switch(nodeMode){
                case GenerationModeNode.Default:
                    switch(meshSettings.generationMode){
                        case MeshSettings.GenerationMode.RelativeToWorld:
                            up = terrain.TerrainNormal;
                            break;
                        default:
                            up = Vector3.up;
                            break;
                    }
                    break;
                case GenerationModeNode.RelativeToWorld:
                    up = terrain.TerrainNormal;
                    break;
                default: 
                    up = Vector3.up;
                    break;
            }

            return Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(Vector3.up, up), Vector3.one).inverse; 
        }
    }
}