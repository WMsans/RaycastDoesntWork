namespace sapra.InfiniteLands
{
    public class CheckPoint
    {
        public CheckPoint previousCheckpoint;
        public InfiniteLandsNode StartAtNode;
        public BranchData branchData;

        public void Reuse(CheckPoint previousCheckpoint, BranchData branchData, InfiniteLandsNode StartAtNode)
        {
            this.previousCheckpoint = previousCheckpoint;
            this.branchData = branchData;
            this.StartAtNode = StartAtNode;
        }

        public bool ProcessNode()
        {
            bool finished = StartAtNode.ProcessNodeGlobal(branchData);
            if (finished)
            {
                Traveller.SwapCheckpoint(previousCheckpoint, this);
            }
            return finished;
        }
    }

}