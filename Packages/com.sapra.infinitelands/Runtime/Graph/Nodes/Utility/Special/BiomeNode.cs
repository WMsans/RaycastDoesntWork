using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEditor;

namespace sapra.InfiniteLands
{
    [CustomNode("Biome", typeof(WorldTree), docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/biome")]
    public class BiomeNode : InfiniteLandsNode
    {
        [System.Serializable]
        public struct IndexPort{
            public int index;
            public string targetNodeGuid;
            public IndexPort(int ind, string nd){
                this.index = ind;
                this.targetNodeGuid = nd;
            }
        }
        public string portName => "Biome Data";
        public BiomeTree tree;

        [NonSerialized] private List<InfiniteLandsNode> NodeCopies = null;
        [NonSerialized] private List<EdgeConnection> EdgeCopies = null;
        
        public List<InfiniteLandsNode> GetNodeCopies()
        {
            if (NodeCopies == null)
                PerformDeepCopy();
            return NodeCopies;
        }
        public List<EdgeConnection> GetEdgeCopies(){
            if(EdgeCopies == null)
                PerformDeepCopy();
            return EdgeCopies;
        }

        public void PerformDeepCopy(){
            NodeCopies = new();
            EdgeCopies = new();
            if(tree == null)
                return;

            string biomeHeightGuid = "";
            var heightNode = Graph.GetBaseNodes().OfType<HeightOutputNode>().FirstOrDefault();
            string ogHeightOutputGUID = Graph.GetBaseNodes().OfType<HeightOutputNode>().FirstOrDefault().guid;
            var assetMask = tree.GetBaseNodes().OfType<AssetsMaskNode>().FirstOrDefault();
            EdgeConnection edgeToMask = assetMask != null ? tree.GetBaseEdges().FirstOrDefault(a => a.inputPort.nodeGuid.Equals(assetMask.guid)) : null;
            PortData targetOutput = edgeToMask != null ? edgeToMask.outputPort : default;
            if(edgeToMask != null){
                targetOutput.nodeGuid += this.guid;
            }
            var currentNodes = tree.GetAllNodes();

            HashSet<string> currentGlobalOutpusGuids = new HashSet<string>();
            var newApplyMaskNodes = new List<(ApplyMaskNode, string)?>();

            foreach(var nd in currentNodes){
                string json = JsonUtility.ToJson(nd);
                InfiniteLandsNode result = Activator.CreateInstance(nd.GetType()) as InfiniteLandsNode;
                JsonUtility.FromJsonOverwrite(json, result);
                result.previewPort = default;
                result.guid += this.guid;

                if(nd is HeightOutputNode)
                    biomeHeightGuid = result.guid;
                
                if(nd is GlobalOutputNode)
                    currentGlobalOutpusGuids.Add(result.guid);

                if(result is LocalOutputNode output)
                    output.InputName = result.guid;
                
                if(assetMask != null){
                    if(nd is ILoadAsset){
                        var applyMask = new ApplyMaskNode();
                        applyMask.ValueAtZero = ApplyMaskNode.ToValue.Zero;
                        string newNodeGuid = this.guid+"-extra"+ newApplyMaskNodes.Count();
                        applyMask.SetupNode(tree, newNodeGuid, position);
                        newApplyMaskNodes.Add((applyMask, result.guid));
                        NodeCopies.Add(applyMask);
                    }
                }

                NodeCopies.Add(result);
            }

            var currentEdges = tree.GetAllEdges();
            foreach(var edge in currentEdges){
                string json = JsonUtility.ToJson(edge);
                EdgeConnection copy = JsonUtility.FromJson<EdgeConnection>(json);
                copy.inputPort.nodeGuid += this.guid;
                copy.outputPort.nodeGuid += this.guid;

                if(currentGlobalOutpusGuids.Contains(copy.outputPort.nodeGuid))
                    continue;

                if(copy.outputPort.nodeGuid.Equals(biomeHeightGuid)){
                    copy.outputPort.nodeGuid = ogHeightOutputGUID;
                }

                var edgeForAsset = newApplyMaskNodes.FirstOrDefault(a => a.Value.Item2.Equals(copy.inputPort.nodeGuid));
                if(assetMask != null && edgeForAsset != null){  
                    ApplyMaskNode appplyMaskNode = edgeForAsset.Value.Item1;
                    EdgeConnection copyToInput = new EdgeConnection(copy);
                    EdgeConnection copyFromOutput = new EdgeConnection(copy);
                    EdgeConnection toMask = new EdgeConnection(targetOutput, new PortData(appplyMaskNode.guid, nameof(appplyMaskNode.Mask)));
                    copyToInput.outputPort = new PortData(appplyMaskNode.guid, nameof(appplyMaskNode.Output));
                    copyFromOutput.inputPort = new PortData(appplyMaskNode.guid, nameof(appplyMaskNode.Input));

                    EdgeCopies.Add(copyFromOutput);
                    EdgeCopies.Add(copyToInput);
                    EdgeCopies.Add(toMask);
                }
                else{
                    var similarEdge = Graph.GetBaseEdges().Where(a => a.inputPort.nodeGuid.Equals(copy.inputPort.nodeGuid));
                    if(!similarEdge.Any()){
                        EdgeCopies.Add(copy);
                    }
                }
            }
        }
        
        public Dictionary<IAsset, HeightData> OutputResults = new();

        public override bool ExtraValidations()
        {
            if(tree == null)
                return true;  
            #if UNITY_EDITOR
            tree.OnValuesChanged -= OnValuesChangedInvoke;
            tree.OnValuesChanged += OnValuesChangedInvoke;
            #endif
            return true;
        }

        private void OnValuesChangedInvoke(){
            PerformDeepCopy();
            #if UNITY_EDITOR
            Graph.NotifyValuesChanged();
            #endif
        }

        public HeightData GetOutput(IAsset asset)
        {
            return OutputResults[asset];
        }

        public bool AssetExists(IAsset asset){
            return GetAssets().Any(a => a.Equals(asset));
        }

        public IEnumerable<IAsset> GetAssets() => tree.GetAssets();
    }
}