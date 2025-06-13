using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class GridManager : IInitializeBranch, ICloseBranch
    {
        private void CreateGridMesh(BranchData targetBranch, BranchData parentSettings, ICreateGrid gridCreator = null){
            GridBranch newBranch = GenericPoolLight<GridBranch>.Get();
            GridBranch parentPointMaker = parentSettings?.GetData<GridBranch>();

            if(gridCreator != null)
                newBranch.Reuse(gridCreator, targetBranch, parentPointMaker);
            else
                newBranch.Reuse(parentPointMaker.GridCreator, targetBranch,parentPointMaker.parentGrid);
            targetBranch.AddData(newBranch);
        }


        private void CopyGridMesh(BranchData newSettings, BranchData originalSettings){
            var ogGridBranch = originalSettings.GetData<GridBranch>();
            if(ogGridBranch != null){
                newSettings.AddData(ogGridBranch);
            }
            else            
                Debug.LogError("Branch couldn't be copied because the original is not there");
        }
        public void InitializeBranch(BranchData branch, BranchData previousBranch)
        {
            var gridCreator = branch.GetData<ICreateGrid>(false);
            if(gridCreator != null || ModifiedSettings(branch, previousBranch)){
                CreateGridMesh(branch, previousBranch, gridCreator);
            }else{
                CopyGridMesh(branch, previousBranch);
            }
        }

        public void CloseBranch(BranchData branch)
        {
            var generatedBranch = branch.GetData<GridBranch>();
            generatedBranch.Release();
        }

        public static BranchData NewGridBranch(int targetResolution, ICreateGrid newGenerator,
            BranchData original, InfiniteLandsNode[] startingNodes)
        {
            MeshSettings newSettings = original.meshSettings;
            newSettings = newSettings.ModifyResolution(targetResolution);
            var settings = GenericPoolLight<BranchData>.Get();
            settings.Reuse(original.treeData, newSettings, original.terrain, startingNodes);
            settings.AddData(newGenerator);
            BranchData.InitializeBranch(settings, original);
            return settings;
        }

        public static bool ModifiedSettings(BranchData newBranch, BranchData previousBranch){
            if(previousBranch == null) return true;
            if(!newBranch.meshSettings.SoftEqual(previousBranch.meshSettings)) return true;
            if(previousBranch.GetData<GridBranch>().GridCreator.RecalculateIfDifferentSeed() && newBranch.meshSettings.Seed != previousBranch.meshSettings.Seed) return true;
            if(newBranch.terrain.Position != previousBranch.terrain.Position) return true;

            return false;
        }
    }
}