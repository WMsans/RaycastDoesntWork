using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class AssetData : ProcessableData
    {
        public (IAsset, int)[] assetWithIndex{get; private set;}
        public int MapPartLength{get; private set;}
        public NativeArray<float> map{get; private set;}
        public JobHandle jobHandle{get; private set;}

        private ReturnablePack returnablePack;
        private IEnumerable<InfiniteLandsNode> nodes;
        private IAsset[] assets;
        private Dictionary<IAsset, ILoadAsset[]> AssetLoaders;

        private ReturnableManager returnableManager;
        protected override void OnFinalisedProcessors()
        {
            returnablePack.Release();
            GenericPoolLight.Release(this);
        }

        public void ReuseData(TreeData data, WorldGenerator generator, MeshSettings meshSettings, TerrainConfiguration terrainConfiguration){
            returnablePack = GenericPoolLight<ReturnablePack>.Get().Reuse();
            returnableManager = data.GlobalStore.GetData<ReturnableManager>();
            nodes = generator.nodes;
            assets = generator.assets;
            AssetLoaders = generator.AssetLoaders;
            GenerateAssetTextures(data.GetTrunk(), meshSettings, returnableManager);
        }
        
        private struct DataResults{
            public HeightData heightData;
            public ILoadAsset.Operation action;
        }

        private void GenerateAssetTextures(BranchData settings, MeshSettings originalSetings, ReturnableManager manager){
            int count = assets.Length;
            var dataToManage = ListPoolLight<NativeArray<DataToManage>>.Get();
            var jobs = new NativeArray<JobHandle>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int index = 0;
            foreach(var asset in assets){
                ILoadAsset[] loaders = AssetLoaders[asset];
                int loadersLength = loaders.Length;
                NativeArray<JobHandle> internalJobs = new NativeArray<JobHandle>(loadersLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<DataToManage> internalIndices = new NativeArray<DataToManage>(loadersLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for(int i = 0; i < loadersLength; i++){
                    var loader = loaders[i];
                    HeightData output = loader.GetOutput(settings, asset);
                    internalJobs[i] = output.jobHandle;
                    internalIndices[i] = new DataToManage(output.indexData, loader.action);
                }

                jobs[index] = JobHandle.CombineDependencies(internalJobs);
                internalJobs.Dispose();
                
                dataToManage.Add(internalIndices);    
                index++;    
            }

            JobHandle afterVegetationCreated = JobHandle.CombineDependencies(jobs);
            jobs.Dispose();

            int assetLength = count;
            int dataLength = dataToManage.Count;

            var textureLength = MapTools.LengthFromResolution(originalSetings.Resolution);
            NativeArray<float> finalTargetMap = manager.GetData<float>(returnablePack,assetLength*textureLength);
            NativeArray<JobHandle> CombineJobs = new NativeArray<JobHandle>(dataLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if(assetWithIndex == null || assetWithIndex.Length != count)
                assetWithIndex = new (IAsset, int)[count];
                
            index = 0;
            var heightBranch = settings.GetData<HeightMapBranch>();
            var map = heightBranch.GetMap();
            foreach(IAsset asset in assets){
                IndexAndResolution targetSpace = new IndexAndResolution(index, originalSetings.Resolution);
                targetSpace.SetIndexOffset(textureLength);
                CombineJobs[index] = MJDensityCombine.ScheduleParallel(map, finalTargetMap, dataToManage[index], targetSpace, afterVegetationCreated);

                assetWithIndex[index] = (asset, index);
                index++;
            }

            JobHandle afterCombined = JobHandle.CombineDependencies(CombineJobs);
            CombineJobs.Dispose();

            for (int i = 0; i < dataLength; i++)
            {
                dataToManage[i].Dispose(afterCombined);
            }
            ListPoolLight<NativeArray<DataToManage>>.Release(dataToManage);

            this.map = finalTargetMap;
            this.MapPartLength = textureLength;
            this.jobHandle = afterCombined;
        }
    }
}