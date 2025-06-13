using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class TreeData
    {
        private CompletitionToken token;
        public StringObjectStore<object> GlobalStore;
        private BranchData Trunk;
        private List<InfiniteLandsNode> outputNodes = new List<InfiniteLandsNode>();
        private List<BranchData> SubBranches = new List<BranchData>();
        public List<IInitializeBranch> BranchInitializers = new();
        public List<ICloseBranch> BranchClosers = new();


        public void Reuse(StringObjectStore<object> store, InfiniteLandsNode[] startingNodes)
        {
            this.outputNodes.Clear();
            this.SubBranches.Clear();
            for (int i = 0; i < startingNodes.Length; i++)
            {
                this.outputNodes.Add(startingNodes[i]);
            }
            this.token = store.GetData<CompletitionToken>();
            this.GlobalStore = store;

            BranchInitializers.Clear();
            BranchClosers.Clear();
            var objects = store.GetManyDataRaw();
            foreach (var initialzier in objects)
            {
                if (initialzier is IInitializeBranch initializer)
                {
                    BranchInitializers.Add(initializer);
                }

                if (initialzier is ICloseBranch closer)
                {
                    BranchClosers.Add(closer);
                }
            }
            this.Trunk = null;
        }

        public bool ForceComplete => token.complete;
        public int AddBranch(BranchData branch)
        {
            if (SubBranches.Count <= 0)
                Trunk = branch;
            var cnt = SubBranches.Count;
            SubBranches.Add(branch);
            return cnt;
        }
        public BranchData GetTrunk() => Trunk;
        public bool ProcessTree()
        {
            var processor = new ProcessNode(Trunk);
            return AwaitableTools.IterateOverItems(outputNodes, ref processor);
        }

        public void CloseTree()
        {
            foreach (var branch in SubBranches)
            {
                branch.CloseBranch();
            }
            GenericPoolLight.Release(this);
        }

        public static TreeData NewTree(StringObjectStore<object> store,
                MeshSettings meshSettings, TerrainConfiguration terrain,
                ICreateGrid gridCreator, InfiniteLandsNode[] startingNodes)
        {
            TreeData treeSettings = GenericPoolLight<TreeData>.Get();
            treeSettings.Reuse(store, startingNodes);
            BranchData.NewBranch(treeSettings, meshSettings, terrain, gridCreator, startingNodes);
            return treeSettings;
        }
        
        
        private struct ProcessNode : ICallMethod<InfiniteLandsNode>
        {
            BranchData trunk;
            public ProcessNode(BranchData trunk){
                this.trunk = trunk;
            }
            public bool Callback(InfiniteLandsNode value)
            {
                if (value.isValid)
                    return value.ProcessNodeGlobal(trunk);
                return true;
            }
        }
    }
}