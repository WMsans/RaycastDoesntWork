using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public partial class InfiniteLandsNode
    {
        #region Inputs
        public bool TryGetInputData<T>(BranchData branch, out T data, string fieldName)
        {
            var portsToInput = PortsToInputWithNode[fieldName];
            var found = portsToInput.Length > 0;
            if (!found)
            {
                data = default;
                return false;
            }
            var targetPort = portsToInput[0];
            return TryGetInputData(branch, targetPort, out data);
        }

        public bool TryGetInputData<T>(BranchData branch, ref List<T> data, string fieldName)
        {
            var ports = GetPortsToInput(fieldName);
            data.Clear();
            for (int i = 0; i < ports.Length; i++)
            {
                data.Add(default);
            }

            for (int i = 0; i < ports.Length; i++)
            {
                if (TryGetInputData(branch, ports[i], out T result))
                {
                    var index = ports[i].localPort.listIndex;
                    data[index] = result;
                }
                else
                    return false;
            }
            return true;
        }

        private bool TryGetInputData<T>(BranchData branch, CachedPort targetPort, out T data)
        {
            return targetPort.node.TryGetOutputData(branch, out data, targetPort.originPort.fieldName, targetPort.originPort.listIndex);
        }

        #endregion

        #region Outputs
        public virtual bool TryGetOutputData<T>(BranchData branch, out T data, string fieldName, int listIndex = -1)
        {
            if (listIndex < 0)
                return branch.GetNodeStore(this).TryGetData(fieldName, out data);
            else
                return branch.GetNodeStore(this).TryGetData(fieldName, listIndex, out data);
        }

        protected void CacheOutputValue<T>(BranchData branch, T data, string fieldName)
        {
            branch.GetNodeStore(this).AddData(fieldName, data);
        }

        protected void CacheOutputValue<T>(BranchData branch, IEnumerable<T> data, string fieldName)
        {
            branch.GetNodeStore(this).AddData(fieldName, data);
        }
        #endregion


        #region Internals
        protected NodeState GetState(BranchData branch)
        {
            if (branch.isClosed)
                Debug.LogError("Doing calculations on an already closed branch!");
            return branch.GetNodeStore(this).GetState();
        }
        
        #endregion
    }
}