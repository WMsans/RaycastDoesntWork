namespace sapra.InfiniteLands
{
    [CustomNode("MISSING", canCreate = false, canDelete = true, docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/special/missingnode.html", customType = "MISSING")]
    public class MissingNode : InfiniteLandsNode
    {
        protected override void Process(BranchData branch)
        {
        }
        protected override void CacheOutputValues(BranchData branch)
        {
        }
        protected override void SetInputValues(BranchData branch)
        {
        }
    }
}