using UnityEngine;

namespace sapra.InfiniteLands
{
    public partial class InfiniteLandsNode
    {
        private bool ProcessNodeInternal(BranchData branch)
        {
            if (Traveller.Block) return false;

            bool shouldCreateCheckpoint = Traveller.IncreaseAndCreateCheckpoint(this);
            var currentState = GetState(branch);

            if (shouldCreateCheckpoint && !currentState.completed)
            {
                Traveller.NewCheckpoint(this, branch);
                return false;
            }

            return ProcessNodeGlobal(branch);
        }
        public bool ProcessNodeGlobal(BranchData branch)
        {
            var currentState = GetState(branch);
            if (!currentState.completed)
            {
                var nodeProcessed = ProcessNode(branch);
                if (nodeProcessed)
                    currentState.SetState(State.Done);
            }

            if (branch.treeData.ForceComplete && !currentState.completed)
            {
                Debug.LogErrorFormat("Something went wrong with {0} : {1}. It should be completed but it's not. Stuck at {2}", guid, this.GetType(), currentState.state);
            }
            return currentState.completed;
        }
        /// <summary>
        /// Processes the node at it's fully. Meaning it will get input values, execute the process, and cache output values
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="store"></param>
        protected virtual bool ProcessNode(BranchData branch)
        {
            var currentState = GetState(branch);
            if (currentState.state == State.Idle)
            {
                currentState.SetState(State.WaitingDependencies);
            }

            if (currentState.state == State.WaitingDependencies)
            {
                if (!WaitingDependencies(branch)) return false;
                currentState.SetState(State.Processing);
            }

            if (currentState.state == State.Processing)
            {
                SetInputValues(branch);
                Process(branch);
                CacheOutputValues(branch);
                currentState.SetState(State.Done);
            }

            return currentState.completed;
        }

        public bool ProcessDependency(BranchData branch, string fieldName){
            var ports = PortsToInputWithNode[fieldName];
            foreach(var port in ports){
                var finished = ProcessDependency(branch, port);
                if(!finished)
                    return false;
            }
            return true;
        }
        
        protected virtual bool WaitingDependencies(BranchData branch)
        {
            var allCompleted = true;
            foreach (var port in Dependencies)
            {
                var result = ProcessDependency(branch, port);
                allCompleted = allCompleted && result;
            }
            return allCompleted;
        }
        private bool ProcessDependency(BranchData branch, CachedPort port)
        {
            return port.node.ProcessNodeInternal(branch);
        }
        
        /// <summary>
        /// Override to define a custom workflow to set the input values. This method ensures that all structs with the Input attribute have data attached to them before calling the process.
        /// </summary>
        /// <param name="branch"></param>
        protected virtual void SetInputValues(BranchData branch)
        {
            if (cacheInputs.Length > 0)
                Debug.LogWarningFormat("No SetInputValues method was implemented for {0}", this.GetType());
        }

        /// <summary>
        /// Override to transform data from input to output. The target of this method is to set a value inside the Output fields
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="store"></param>
        protected virtual void Process(BranchData branch){
            Debug.LogWarningFormat("No Process method was implemented for {0}", this.GetType());
        }
        /// <summary>
        ///  Override to define a custom workflow to store the output values. Ensures that all data created during the Process method is stored inside the generation branch object for later use by other nodes.
        /// </summary>
        /// <param name="branch"></param>
        protected virtual void CacheOutputValues(BranchData branch){
            if(cacheOutputs.Length > 0)
                Debug.LogWarningFormat("No SetInputValues method was implemented for {0}", this.GetType());
                
        }
    }
}