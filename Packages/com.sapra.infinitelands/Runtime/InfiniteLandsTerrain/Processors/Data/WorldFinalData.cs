using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class WorldFinalData : ProcessableData
    {
        public NativeArray<float> ChunkMinMax;
        public NativeArray<Vertex> FinalPositions;
        public Vector2 MinMaxHeight;
        public JobHandle jobHandle;
        private ReturnablePack returnableData;

        public void ReuseData(ReturnablePack returnableData, NativeArray<float> chunkMinMax, NativeArray<Vertex> finalPositions, Vector2 minMaxHeight, JobHandle job){
            MinMaxHeight = minMaxHeight;
            ChunkMinMax = chunkMinMax;
            FinalPositions = finalPositions;
            jobHandle = job;
            this.returnableData = returnableData;
        }

        protected override void OnFinalisedProcessors()
        {
            returnableData.Release();
            GenericPoolLight.Release(this);
        }
    }
}