using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public class WorldGenerationData : AwaitableData<ChunkData>
    {
        public ChunkData Result{get; private set;}
        public TerrainConfiguration terrain{get; private set;}
        public MeshSettings meshSettings{get; private set;}
        
        private int SubState = 0;

        private TreeData tree;
        private TreeData separateOutputTree;
        private MeshSettings separetOutputMeshSettings;


        private StringObjectStore<object> store;
        private HeightToWorld heightToWorldNode;
        private WorldGenerator worldGenerator;
        private CompletitionToken token;

        private JobHandle jobHandle;
        private TypeStore<ProcessableData> resultingStore;
        public void Reuse(WorldGenerator worldGenerator, TerrainConfiguration terrain, MeshSettings meshSettings)
        {
            this.worldGenerator = worldGenerator;
            this.store = worldGenerator.store;
            this.token = worldGenerator.token;
            heightToWorldNode = worldGenerator.heightToWorldNode;

            this.meshSettings = meshSettings;
            this.worldGenerator = worldGenerator;
            this.terrain = terrain;
            SubState = 0;
            tree = TreeData.NewTree(store, meshSettings, terrain, worldGenerator.output, worldGenerator.mainBranchNodes);
            if(worldGenerator.SepartedOutputs){
                separetOutputMeshSettings = meshSettings;
                separetOutputMeshSettings.Resolution = meshSettings.TextureResolution;
                separateOutputTree = TreeData.NewTree(store, separetOutputMeshSettings, terrain, worldGenerator.output, worldGenerator.separateBranchNodes);
            }else{
                separateOutputTree = null;
            }
            Result = null;
        }

        public bool ProcessData()
        {
            if (tree == null) return true;
            if (SubState == 0)
            {
                if (!tree.ProcessTree()) return false;
                if (separateOutputTree != null && !separateOutputTree.ProcessTree()) return false;
                SubState++;
            }

            if(SubState == 1){
                //tree.Complete();
                WorldFinalData biomeOutput;
                if(!heightToWorldNode.TryGetOutputData(tree.GetTrunk(), out biomeOutput, nameof(heightToWorldNode.Output)))
                {
                    Debug.LogError("System not finished");
                    SubState = 3;
                    return false;
                }
                AssetData assetsData = GenericPoolLight<AssetData>.Get();
                if(separateOutputTree != null)
                    assetsData.ReuseData(separateOutputTree, worldGenerator, separetOutputMeshSettings, terrain);
                else
                    assetsData.ReuseData(tree, worldGenerator, meshSettings, terrain);

                if (biomeOutput == null)
                {
                    SubState = 3;
                    Result = null;
                    return false;
                }
                JobHandle everythingGenerated = JobHandle.CombineDependencies(assetsData.jobHandle, biomeOutput.jobHandle);
                
                CoordinateDataHolder coordinate = GenericPoolLight<CoordinateDataHolder>.Get();
                coordinate.ReuseData(tree, biomeOutput,everythingGenerated, meshSettings, terrain);

                LandmarkData landmarkData = GenericPoolLight<LandmarkData>.Get();
                landmarkData.ReuseData(tree, worldGenerator);

                jobHandle = coordinate.finalJob;
                resultingStore = GenericPoolLight<TypeStore<ProcessableData>>.Get();
                resultingStore.Reuse();

                resultingStore.AddData(assetsData);
                resultingStore.AddData(biomeOutput);
                resultingStore.AddData(coordinate);
                resultingStore.AddData(landmarkData);
                SubState++;
            }

            if(SubState == 2){
                if(!jobHandle.IsCompleted && !token.complete) return false;
                jobHandle.Complete();  
                Result = GenericPoolLight<ChunkData>.Get();
                Result.Reuse(terrain, meshSettings, tree, resultingStore, separateOutputTree);
                SubState++;
            }

            return SubState == 3;
        }

        public bool ForceComplete(){
            token?.Complete();
            return ProcessData() && tree != null;
        }
    }
}