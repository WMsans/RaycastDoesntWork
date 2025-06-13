using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Local Input", docs ="https://ensapra.com/packages/infinite_lands/nodes/utility/local/local", startCollapsed = true, synonims = new string[]{"Portal"})]
    public class LocalInputNode : InfiniteLandsNode
    {
        [Input, Hide] public object Input;
        [Output(match_type_name: nameof(Input))] public object Output;

        protected override void Process(BranchData branch)
        {
        }
        protected override void CacheOutputValues(BranchData branch)
        {
        }
        protected override void SetInputValues(BranchData branch)
        {
        }
        public override bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            return TryGetInputData(branch, out data, nameof(Input));
        }
    }
}