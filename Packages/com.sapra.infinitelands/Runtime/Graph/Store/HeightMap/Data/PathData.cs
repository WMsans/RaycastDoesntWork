using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class PathData{
        private struct NodeOutputSpace
        {
            public Dictionary<string, IndexAndResolution> Outputs;
            public List<string> AllKeys;
            public InfiniteLandsNode node;

            public NodeOutputSpace(InfiniteLandsNode node, Dictionary<string, IndexAndResolution> outputs, List<string> keys)
            {
                Outputs = outputs;
                AllKeys = keys;
                this.node = node;
            }

            public void Add(string key, IndexAndResolution indexAndResolution)
            {
                AllKeys.Add(key);
                Outputs.Add(key, indexAndResolution);
            }

            public bool TryGetValue(string key, out IndexAndResolution indexAndResolution)
            {
                return Outputs.TryGetValue(key, out indexAndResolution);
            }

            public bool ContainsKey(string key)
            {
                return Outputs.ContainsKey(key);
            }

            public int UpdateSpaceLengths(int currentMaxResolution, PathData pather)
            {
                int currentMax = -1;
                bool requiresUpdating = false;
                foreach (string key in AllKeys)
                {
                    var foundIndex = Outputs[key];
                    if (foundIndex.UpdateLength(currentMaxResolution))
                    {
                        Outputs[key] = foundIndex;
                        requiresUpdating = true;
                    }
                    else
                        currentMax = Mathf.Max(currentMax, foundIndex.Resolution);
                }
                if (requiresUpdating)
                    return pather.RecursiveNodeAllocation(currentMaxResolution, node);
                else
                    return currentMax;
            }
        }
        public IEnumerable<InfiniteLandsNode> startingNodes;
        private Dictionary<int, NodeOutputSpace> NodeSpace = new();
        private static Dictionary<(string guid, string field), string> TheoreticalNames = new();
        private static string GetTheoreticalName(string nodeguid, string fieldName){
            if(!TheoreticalNames.TryGetValue((nodeguid, fieldName), out var key)){
                key = nodeguid+fieldName;
                TheoreticalNames.Add((nodeguid, fieldName), key);
            }
            return key;
        }

        private int SpaceCount = 0;
        public int SingleMapLength{get; private set;}
        public int TotalLength => SpaceCount*SingleMapLength;
        public int FinalResolution{get; private set;}

        public HeightMapManager manager{get; private set;}
        public MeshSettings OriginalSettings{get; private set;}
        private IGraph graph;

        public PathData(HeightMapManager manager, MeshSettings settings, IEnumerable<InfiniteLandsNode> startedNodes){
            this.manager = manager;
            this.OriginalSettings = settings;
            this.startingNodes = startedNodes;
            this.graph = manager.graph;
        }

        public bool GetTheoreticalMinMax(BranchData branch, InfiniteLandsNode node, string inputName, out float2 Result){
            var key = GetTheoreticalName(node.guid, inputName);
            var reuser = new ExtractHeightReuser(branch, node, inputName);
            return AwaitableTools.WaitGlobal<ExtractHeight,float2,ExtractHeightReuser>(branch, ref reuser, out Result, key);
        }
        private struct ExtractHeightReuser : IReuseObject<ExtractHeight>
        {
            BranchData branchData;
            InfiniteLandsNode node;
            string inputName;
            public ExtractHeightReuser(BranchData branchData, InfiniteLandsNode node, string inputName){
                this.branchData = branchData;
                this.node = node;
                this.inputName = inputName;
            }
            public void Reuse(ExtractHeight instance)
            {
                instance.Reuse(branchData, node, inputName);
            }
        }
        private NodeOutputSpace GetNodeOutputSpace(InfiniteLandsNode node, bool createStore = true){
            if (!NodeSpace.TryGetValue(node.small_index, out var space))
            {
                var outputs = new Dictionary<string, IndexAndResolution>();
                var keys = new List<string>();
                space = new NodeOutputSpace(node, outputs, keys);
                NodeSpace.Add(node.small_index, space);
                if (!createStore)
                {
                    Debug.LogErrorFormat("Store wasn't created for node {0} : {1} and it doesn't want to be created. Something went wrong", node.GetType(), node.small_index);
                }
            }
            return space;
        }
        #region Allocation
        private IndexAndResolution AllocateOutputSpaceDirect(InfiniteLandsNode node, string key, int resolution){
            IndexAndResolution indexAndResolution = new IndexAndResolution(SpaceCount, resolution);
            GetNodeOutputSpace(node).Add(key, indexAndResolution);
            SpaceCount++;
            return indexAndResolution;
        }
               
        /// <summary>
        /// Allocates space with the provided key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int AllocateOutputSpace(InfiniteLandsNode node, string key, int resolution)
        {
            var nodeSpace = GetNodeOutputSpace(node);
            if (!nodeSpace.ContainsKey(key))
            {
                AllocateOutputSpaceDirect(node, key, resolution);
            }
            return nodeSpace.UpdateSpaceLengths(resolution, this);
        }
        /// <summary>
        /// Allocates space and increases the amount of items stored by count
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int AllocateOutputSpace(InfiniteLandsNode node, string key, int count, int resolution){
            var nodeSpace = GetNodeOutputSpace(node);
            if (!nodeSpace.ContainsKey(key))
            {
                AllocateOutputSpaceDirect(node, key, resolution);
                SpaceCount += count;
            } 
            return nodeSpace.UpdateSpaceLengths(resolution, this);
        }

        /// <summary>
        /// Allocates all the outputs defined in a node
        /// </summary>
        /// <param name="node"></param>
        public int AllocateOutputs(InfiniteLandsNode node, int resolution)
        {
            int maxResolution = resolution;
            var outputFields = node.GetOutputFields();
            foreach (var ndOutput in outputFields)
            {
                if (ndOutput.FieldType == typeof(HeightData))
                {
                    var newRes = AllocateOutputSpace(node, ndOutput.Name, resolution);
                    maxResolution = Mathf.Max(maxResolution, newRes);
                }
            }
            return maxResolution;
        }
        

        public int FromInputToOutput(InfiniteLandsNode ogNode, string ogFieldName, int currentMaxResolution)
        {
            var edgesOfNode = ogNode.GetPortsToInput(ogFieldName);
            foreach (var outputPort in edgesOfNode)
            {
                var connectedNode = outputPort.node;
                int currentRes = RecursiveNodeAllocation(currentMaxResolution, connectedNode);
                currentMaxResolution = Mathf.Max(currentRes, currentMaxResolution);
            }
            return currentMaxResolution;
        }

        public int AllocateInput(InfiniteLandsNode node, string ogFieldName, int resolution)
        {
            return FromInputToOutput(node, ogFieldName, resolution);
        }

        /// <summary>
        /// Allocates space with padding in the resolution
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldName"></param>
        /// <param name="padding"></param>
        /// <param name="currentMaxResolution"></param>
        /// <returns></returns>
        public int ApplyInputPadding(InfiniteLandsNode node, string fieldName, int padding, int currentMaxResolution)
        {
            return FromInputToOutput(node, fieldName, MapTools.IncreaseResolution(currentMaxResolution, padding));
        }

        /// <summary>
        /// Allocates space used for normal map retrieval
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fieldName"></param>
        /// <param name="currentMaxResolution"></param>
        /// <returns></returns>
        public int ApplyInputNormalMap(InfiniteLandsNode node, string fieldName, int currentMaxResolution)
        {
            return FromInputToOutput(node, fieldName, MapTools.IncreaseResolution(currentMaxResolution, 1));
        }

        #endregion

        public IndexAndResolution GetSpace(InfiniteLandsNode node, string fieldName){
            if(GetNodeOutputSpace(node, false).TryGetValue(fieldName, out IndexAndResolution space)){
                return space.SetIndexOffset(SingleMapLength);
            }

            Debug.LogErrorFormat("Trying to get space that wasn't preallocated {0} for {1} in {2}:{3}", fieldName, node.small_index, GetHashCode(), node.GetType());
            return default;
        }

        public void StartNodeApplication(){ //Optimiazable
            var InitialResolution = OriginalSettings.Resolution;
            int currentMaxResolution = InitialResolution;
            foreach(var starterNodeGuid in startingNodes){               
                int recursiveResolution = RecursiveNodeAllocation(InitialResolution, starterNodeGuid);
                currentMaxResolution = Mathf.Max(recursiveResolution, currentMaxResolution);

                if(NodeSpace.TryGetValue(starterNodeGuid.small_index, out var fromNodes)){
                    fromNodes.UpdateSpaceLengths(InitialResolution, this);
                }
            }
            FinalResolution = currentMaxResolution;
            SingleMapLength = MapTools.LengthFromResolution(currentMaxResolution);
        } 
        
        public int RecursiveNodeAllocation(int acomulatedResolution, InfiniteLandsNode node){
            if(node is IHeightMapConnector customImplementation){
                return customImplementation.ConnectHeightMap(this, OriginalSettings, acomulatedResolution);
            }
            else
                return DefaultImplementation(acomulatedResolution, node);
        }

        private int DefaultImplementation(int resolution, InfiniteLandsNode node){
            AllocateOutputs(node, resolution); //Allocate all the outputs of that node

            IEnumerable<PortData> connectionsToNode = node.GetPortsToInput(); //Go through each input node and allcoate the necessary outputs
            int currentMaxRequest = resolution;
            foreach(var outputPort in connectionsToNode){
                var connectedNode = graph.GetNodeFromGUID(outputPort.nodeGuid);
                var recievedMax = RecursiveNodeAllocation(resolution, connectedNode);
                currentMaxRequest = Mathf.Max(currentMaxRequest, recievedMax);
            }
            return currentMaxRequest;
        }

        private class ExtractHeight : AwaitableData<float2>
        {
            public float2 Result{get; private set;}

            private BranchData LowQualityQuickSettings;
            private BranchData ogBranch;
            private TreeData OgTree;
            private InfiniteLandsNode node;
            private int SubState;
            private string InputFieldName;
            private HeightData heightData;

            private TreeData independentTree;

            public void Reuse(BranchData settings, InfiniteLandsNode node, string inputName){
                this.node = node;
                SubState = 0;
                Result = default;
                InputFieldName = inputName;
                heightData = default;
                MeshSettings meshSettings = new MeshSettings(){
                    Resolution = 1,
                    MeshScale = 100000,
                    Seed = settings.meshSettings.Seed,
                };
                
                TerrainConfiguration terrain = new TerrainConfiguration(default, default, Vector3.zero);
                OgTree = settings.treeData;
                ogBranch = OgTree.GetTrunk();

                var gridCreator = ogBranch.GetData<GridBranch>().GridCreator;
                independentTree = TreeData.NewTree(OgTree.GlobalStore, meshSettings, terrain, gridCreator, node.GetNodesInInput(InputFieldName));
                LowQualityQuickSettings = independentTree.GetTrunk();
            }
           
            public bool ProcessData()
            {
                if(SubState == 0){
                    if(!node.ProcessDependency(LowQualityQuickSettings, InputFieldName)) return false;
                    SubState++;
                }
                if(SubState == 1){
                    node.TryGetInputData(LowQualityQuickSettings, out heightData, InputFieldName);
                    Result = heightData.minMaxValue;
                    SubState++;
                }
                if(SubState == 2){  
                    if(!heightData.jobHandle.IsCompleted && !independentTree.ForceComplete) return false;
                    if(!independentTree.ProcessTree()) return false;

                    //independentTree.Complete();
                    heightData.jobHandle.Complete();
                    independentTree.CloseTree();
                    SubState++;
                }

                return SubState == 3;
            }
        }

    }
}