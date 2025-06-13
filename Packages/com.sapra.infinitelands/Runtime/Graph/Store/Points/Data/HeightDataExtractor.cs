using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct HeighDataExtractor : AwaitableData<HeightData>
    {
        public HeightData Result{get; private set;}
        TreeData newTree;
        BranchData branchData;

        InfiniteLandsNode node;
        private string fieldName;
        private int SubState;

        public HeighDataExtractor(InfiniteLandsNode node, string fieldName, StringObjectStore<object> globalStore, ICreateGrid createGrid, Vector3 position, MeshSettings meshSettings, out TreeData newTree)
        {
            this.node = node;
            this.fieldName = fieldName;
            SubState = 0;
            Result = default;

            TerrainConfiguration terrain = new TerrainConfiguration(default, default, position);
            var related = node.GetNodesInInput(fieldName);
            newTree = TreeData.NewTree(globalStore ,meshSettings, terrain, createGrid, related);
            this.newTree = newTree;
            this.branchData = this.newTree.GetTrunk();
        }
        public bool ProcessData()
        {
            if(SubState == 0){
                if(!node.ProcessDependency(branchData, fieldName)) return false;
                SubState++;
            }
            
            if(SubState == 1){
                var found = node.TryGetInputData(branchData, out HeightData resultsHeight, fieldName);
                if(!found){
                    Debug.LogError("Something went wrong");
                    return true;
                }
                Result = resultsHeight;
                SubState++;
            }
            
            return SubState == 2;
        }
    }
}