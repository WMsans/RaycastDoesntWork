using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Global Input", typeof(BiomeTree), docs = "https://ensapra.com/packages/infinite_lands/nodes/utility/global/globalinput", startCollapsed = true)]
    public class GlobalInputNode : InfiniteLandsNode
    {
        [HideInInspector] public string OutputName = "Input";
        [Input] public object Default;
        [Output(match_type_name: nameof(Default), namefield: nameof(OutputName))] public object Output;

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