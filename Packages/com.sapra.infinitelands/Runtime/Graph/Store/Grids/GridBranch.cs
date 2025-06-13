using UnityEngine;

namespace sapra.InfiniteLands{
    public class GridBranch{
        public ICreateGrid GridCreator; 
        public BranchData Branch;
        public GridBranch parentGrid;

        private AwaitableData<GridData> grid;
        private GridData result;
        private bool Generated;
        
        public void Reuse(ICreateGrid gridCreator, BranchData settings, GridBranch parentSettings){
            GridCreator = gridCreator;
            Branch = settings;
            parentGrid = parentSettings;
            Generated = false;
            grid = null;
        }

        public bool ProcessGrid(out GridData gridData){
            if(!Generated){
                if(grid == null){
                    grid = GridCreator.GetMeshGrid(Branch, parentGrid);   
                }  
                
                if(grid.ProcessData()){
                    result = grid.Result;
                    Generated = true;
                    grid = null;
                }
            }      

            gridData = result;
            return Generated;
        }

        public GridData GetGridData(){
            if(!Generated)
                Debug.Log("The Grid hasn't been completed, ensure to first process the Grid");
            return result;
        }

        public void Release(){
            if(Branch != null){
                GenericPoolLight.Release(this);
                Branch = null;
            }
        }
    }
}