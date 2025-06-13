using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands{
    public class HeightMapBranch{
        private PathData path;
        private NativeArray<float> Map;
        public BranchData referenceBranch;

        public void Reuse(PathData path, BranchData branch){
            this.path = path;
            this.referenceBranch = branch;
            this.Map = branch.GetData<ReturnableBranch>().GetData<float>(path.TotalLength);
        }
        
        public MeshSettings GetOriginalSettings() => path.OriginalSettings;
        public int GetFinalLength() => path.SingleMapLength;

        public bool GetTheoreticalMinMax(BranchData branch, InfiniteLandsNode node, string inputName, out float2 Result){
            return path.GetTheoreticalMinMax(branch, node, inputName, out Result);
        }

        public bool GetNormalMapData(InfiniteLandsNode node, string fieldName, HeightData inputData, out NormalMapData normalMapData)
        {   
            var reuser = new NormalMapGeneratorReuser(referenceBranch, Map, inputData);
            var key = HeightMapManager.GetNormalMapName(fieldName);
            return AwaitableTools.WaitNode<NormalMapGenerator, NormalMapData, NormalMapGeneratorReuser>(referenceBranch, ref reuser, out normalMapData, node, key);
        }

        public NativeArray<float> GetMap() => Map;

        /// <summary>
        /// Returns an array with floats to be set and read, used by all the nodes that will calculate data in that branch
        /// </summary>
        /// <param name="branchID">ID representing the branch</param>
        /// <param name="ID">ID representing the generation call</param>
        public IndexAndResolution GetAllocationSpace(InfiniteLandsNode node, string fieldName, out NativeArray<float> map){
            map = Map;
            return path.GetSpace(node, fieldName);
        }

        public IndexAndResolution GetAllocationSpace(InfiniteLandsNode node, string fieldName){
            return path.GetSpace(node, fieldName);
        }

        private struct NormalMapGeneratorReuser : IReuseObject<NormalMapGenerator>
        {
            BranchData branchData;
            NativeArray<float> Map;
            HeightData inputData;
            public NormalMapGeneratorReuser(BranchData branchData, NativeArray<float> Map, HeightData inputData){
                this.branchData = branchData;
                this.Map = Map;
                this.inputData = inputData;
            }
            public void Reuse(NormalMapGenerator instance)
            {
                instance.Reuse(branchData, Map, inputData);
            }
        }

        private class NormalMapGenerator : AwaitableData<NormalMapData>
        {
            public NormalMapData Result{get; private set;}
            
            private GridBranch awaitableGridData;
            private HeightData inputData;
            private BranchData settings;
            private NativeArray<float> map;
            private int SubState;

            public void Reuse(BranchData settings, NativeArray<float> map, HeightData inputData){
                awaitableGridData = settings.GetData<GridBranch>();
                this.inputData = inputData;
                this.settings = settings;
                this.map = map;
                this.SubState = 0;
                this.Result = default;
            }
            public bool ProcessData()
            {
                
                if(SubState == 0){
                    if(!awaitableGridData.ProcessGrid(out var gridData)) return false;

                    int length = MapTools.LengthFromResolution(inputData.indexData.Resolution);

                    IndexAndResolution real = inputData.indexData;
                    NormalMapData resultingData = new NormalMapData();
                    resultingData.indexData = new IndexAndResolution(0, MapTools.IncreaseResolution(real.Resolution,-1));
                    resultingData.indexData.SetIndexOffset(length);
                    resultingData.NormalMap = settings.GetData<ReturnableBranch>().GetData<float3>(length);  
                    JobHandle combined = JobHandle.CombineDependencies(gridData.jobHandle, inputData.jobHandle);

                    resultingData.jobHandle = GenerateNormalMap.ScheduleParallel(gridData.grid, resultingData.NormalMap, map, resultingData.indexData, real, 
                        settings.meshSettings.Resolution, combined);
                    Result = resultingData;
                    SubState++;
                }
                return SubState == 1;
            }
        }
    }
}