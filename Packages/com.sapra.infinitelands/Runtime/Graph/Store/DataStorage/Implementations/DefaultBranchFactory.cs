namespace sapra.InfiniteLands{
    public struct DefaultBranchFactory<C> : IFactory<BranchData>
        where C : InfiniteLandsNode, ICreateGrid
    {
        private InfiniteLandsNode node;
        private ICreateGrid gridMaker;
        private BranchData branch;
        private string nameInput;
        private string nameOutput;
        public DefaultBranchFactory(C node, BranchData branch, string nameInput, string nameOutput){
            this.node = node;
            this.gridMaker = node;
            this.branch = branch;
            this.nameInput = nameInput;
            this.nameOutput = nameOutput;
        }
        public BranchData Create()
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            var targetSpace = heightBranch.GetAllocationSpace(node, nameOutput);
            return GridManager.NewGridBranch(targetSpace.Resolution, gridMaker, branch, node.GetNodesInInput(nameInput));
        }
    }
}