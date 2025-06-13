using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Global Output", typeof(BiomeTree), docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/global/globaloutput", startCollapsed = true)]
    public class GlobalOutputNode : InfiniteLandsNode
    {
        [HideInInspector] public string InputName = "Output";
        [Input(namefield: nameof(InputName))] public object Input;
        [Output(match_type_name: nameof(Input)), Disabled] public object Default;

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
            return TryGetInputData(branch, out data, nameof(Default));
        }
    }
}