using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;

using System;

namespace sapra.InfiniteLands
{
    [CustomNode("Layers", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/operations/layers")]
    public class LayersNode : InfiniteLandsNode, IHeightMapConnector
    {
        [Input] public List<HeightData> Input = new();
        [Output(match_list_name:nameof(Input))] public List<HeightData> Weights = new();
        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution)
        {
            int inputsResolution = currentBranch.AllocateInput(this, nameof(Input), acomulatedResolution);
            currentBranch.AllocateOutputSpace(this, nameof(Weights), GetCountOfNodesInInput(nameof(Input)), acomulatedResolution);
            return inputsResolution;
        }
        
        protected override void SetInputValues(BranchData branch)
        {
            TryGetInputData(branch, ref Input, nameof(Input));
        }

        protected override void Process(BranchData branch)
        {
            HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
            int length = Input.Count;
        
            
            NativeArray<JobHandle> combinedJobs = new NativeArray<JobHandle>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<HeightData> heightdatas = new NativeArray<HeightData>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                combinedJobs[i] = Input[i].jobHandle;
                heightdatas[i] = Input[i];
            }
            JobHandle onceChild = JobHandle.CombineDependencies(combinedJobs);
            combinedJobs.Dispose();

            var weigthSpace = heightBranch.GetAllocationSpace(this, nameof(Weights), out var map);
            NativeArray<IndexAndResolution> weightDatas = new NativeArray<IndexAndResolution>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int x = 0; x < length; x++)
            {
                IndexAndResolution nIndex = IndexAndResolution.CopyAndOffset(weigthSpace, x);
                weightDatas[x] = nIndex;
            }

            JobHandle combineJob = LayersJob.ScheduleParallel(map, heightdatas, weigthSpace, weightDatas, length, onceChild);
            Weights.Clear();
            for (int i = 0; i < length; i++)
            {
                Weights.Add(new HeightData(combineJob, weightDatas[i], new Vector2(0, 1)));
            }

            weightDatas.Dispose(combineJob);
            heightdatas.Dispose(combineJob);
        }

        protected override void CacheOutputValues(BranchData branch)
        {
            CacheOutputValue<HeightData>(branch, Weights, nameof(Weights));
        }
    }
}