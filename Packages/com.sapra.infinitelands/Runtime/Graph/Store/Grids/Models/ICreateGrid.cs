
namespace sapra.InfiniteLands
{
    // Modifies the points that are used for the generation of that branch
    public interface ICreateGrid{
        public AwaitableData<GridData> GetMeshGrid(BranchData settings, GridBranch parentGrid);
        public bool RecalculateIfDifferentSeed();
    }

}