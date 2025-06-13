using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class WorldGenerator 
    {        
        public InfiniteLandsNode[] nodes;
        public IAsset[] assets;
        public IGraph graph;

        public HeightOutputNode output;
        public Dictionary<IAsset, ILoadAsset[]> AssetLoaders;
        public InfiniteLandsNode[] mainBranchNodes;
        public InfiniteLandsNode[] separateBranchNodes;

        public HeightToWorld heightToWorldNode;
        public StringObjectStore<object> store;
        public HeightMapManager heightMapManager;
        public ReturnableManager returnableArrays;
        public GridManager gridManager;
        public PointManager pointManager;
        public CompletitionToken token;
        
        public bool ValidGenerator{ get; private set; }
        public bool SepartedOutputs{ get; private set; }

        public WorldGenerator(IGraph generator, bool separateOutputs)
        {
            graph = generator;
            if (generator == null)
                return;
            ReCollectItems();
            InitializeComponents();

            heightToWorldNode = new HeightToWorld(graph, output);
            if (separateOutputs)
            {
                if (heightToWorldNode.isValid)
                    mainBranchNodes = new InfiniteLandsNode[] { heightToWorldNode };
                else
                    mainBranchNodes = new InfiniteLandsNode[0];

                IEnumerable<InfiniteLandsNode> outputs = nodes.Where(a => typeof(IOutput).IsAssignableFrom(a.GetType()))
                    .Where(a => a.isValid);
                separateBranchNodes = outputs.ToArray();
            }
            else
            {
                IEnumerable<InfiniteLandsNode> outputs = nodes.Where(a => typeof(IOutput).IsAssignableFrom(a.GetType()))
                    .Concat(new InfiniteLandsNode[] { heightToWorldNode })
                    .Where(a => a.isValid);
                mainBranchNodes = outputs.ToArray();
                separateBranchNodes = null;
            }

            SepartedOutputs = separateOutputs && separateBranchNodes != null && separateBranchNodes.Length > 0;

            if (mainBranchNodes.Length <= 0 || !heightToWorldNode.isValid)
            {
                Debug.LogErrorFormat("There's no valid node in {0}. Ensure the Height Output Node has a valid connection", graph.name);
                ValidGenerator = false;
            }
            else
                ValidGenerator = true;
        }

        public void ReCollectItems(){
            nodes = graph.GetAllNodes().ToArray();
            output = graph.GetOutputNode();
            assets = graph.GetAssets().ToArray();
            AssetLoaders = new();
            foreach(var asset in assets){
                AssetLoaders.Add(asset, graph.GetAsetLoaderOf(asset).ToArray());
            }
        }
        private void InitializeComponents(){
            store = new StringObjectStore<object>();
            returnableArrays = new ReturnableManager(store); 
            heightMapManager = new HeightMapManager(graph);
            gridManager = new GridManager();
            pointManager = new PointManager(store);
            token = new CompletitionToken();

            store.AddData(returnableArrays);
            store.AddData(heightMapManager);
            store.AddData(pointManager);
            store.AddData(gridManager);
            store.AddData(token);
        }

        public void Dispose(JobHandle job){
            store.Dispose(job);
            graph.OnValuesChanged -= ReCollectItems;
        }

        public void DisposeReturned(){
            store.DisposeReturned();
        }
    }
}