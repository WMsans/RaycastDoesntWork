using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class HeightToWorld : InfiniteLandsNode, IHeightMapConnector, IOutput
    {
        [Input] public HeightData HeightMap;
        [Output] public WorldFinalData Output;
        private HeightOutputNode outputNode;
        public HeightToWorld(IGraph graph, HeightOutputNode outputNode){
            SetupNode(graph, "-1WorldOutput", Vector2.zero);
            this.outputNode = outputNode;
            this.isValid = graph != null && outputNode != null && outputNode.isValid;
        }

        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {
            return currentBranch.RecursiveNodeAllocation(MapTools.IncreaseResolution(acomulatedResolution, 1), outputNode);
        }

        protected override bool ProcessNode(BranchData branch)
        {
            var reuser = new HeightToWorldDataReuser(branch, outputNode, this);
            return AwaitableTools.WaitNode<HeightToWorldData, WorldFinalData, HeightToWorldDataReuser>(branch, ref reuser, out Output, this, nameof(Output));
        }
        private struct HeightToWorldDataReuser : IReuseObject<HeightToWorldData>
        {
            private BranchData branch;
            private HeightOutputNode outputNode;
            private HeightToWorld node;
            public HeightToWorldDataReuser(BranchData branch, HeightOutputNode heightOutputNode, HeightToWorld node){
                this.branch = branch;
                this.outputNode = heightOutputNode;
                this.node = node;
            }
            public void Reuse(HeightToWorldData instance)
            {
                instance.Reuse(outputNode, node, branch);
            }
        }
        private class HeightToWorldData : AwaitableData<WorldFinalData>
        {
            public WorldFinalData Result{get; private set;}
            private int SubState = 0;

            HeightOutputNode outputNode;
            HeightToWorld node;
            BranchData branch;

            private HeightData heightMap;
            private HeightMapBranch heightBranch;
            private NormalMapData normalMapData;
            private float[] edges;

            public void Reuse(HeightOutputNode outputNode, HeightToWorld node, BranchData branch){
                SubState = 0;
                this.outputNode = outputNode;
                this.node = node;
                this.branch = branch;
                heightBranch = branch.GetData<HeightMapBranch>();
                if(edges == null)
                    edges = new float[2];
                
                edges[0] = heightMap.minMaxValue.y;
                edges[1] = heightMap.minMaxValue.x;
            }
            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!outputNode.ProcessNodeGlobal(branch)) return false;
                    if(!outputNode.TryGetOutputData(branch, out heightMap, nameof(outputNode.FinalTerrain)))
                        Debug.LogError("Something went wrong");
                    SubState++;
                }
                if(SubState == 1){
                    if(!heightBranch.GetNormalMapData(outputNode, nameof(outputNode.FinalTerrain), heightMap, out normalMapData)) return false;
                    SubState++;
                }
                if(SubState == 2){
                    ReturnableManager manager = branch.GetGlobalData<ReturnableManager>();
                    
                    var meshSettings = heightBranch.GetOriginalSettings();
                    int length = MapTools.LengthFromResolution(meshSettings.Resolution);
                    var globalMap = heightBranch.GetMap();
                    
                    ReturnablePack holdReturnableData = GenericPoolLight<ReturnablePack>.Get().Reuse();
                    NativeArray<Vertex> finalPositions = manager.GetData<Vertex>(holdReturnableData, length);
                    NativeArray<float> MinMaxHeight = manager.GetData(holdReturnableData, edges);

                    JobHandle applyHeight = ApplyHeightJob.ScheduleParallel(finalPositions, globalMap, 
                        MinMaxHeight, heightMap.indexData, meshSettings.Resolution, meshSettings.MeshScale,
                        normalMapData.NormalMap, normalMapData.indexData, normalMapData.jobHandle);
                    Result = GenericPoolLight<WorldFinalData>.Get();

                    Result.ReuseData(holdReturnableData, MinMaxHeight, finalPositions, heightMap.minMaxValue, applyHeight);
                    SubState++;
                }

                return SubState == 3;
            }
        }
    }
}