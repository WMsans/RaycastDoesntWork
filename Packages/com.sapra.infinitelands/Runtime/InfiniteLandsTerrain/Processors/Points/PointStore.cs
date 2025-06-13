using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [ExecuteInEditMode]
    public class PointStore : ChunkProcessor<ChunkData>, IGenerate<CoordinateDataHolder>
    {
        [Header("Base configuration")] 
        private Dictionary<Vector3Int, CoordinateDataHolder> _chunksGenerated = new();

        public Action<CoordinateDataHolder> onProcessDone { get; set; }
        public Action<CoordinateDataHolder> onProcessRemoved { get; set; }

        protected override void DisableProcessor()
        {
            foreach(KeyValuePair<Vector3Int, CoordinateDataHolder> values in _chunksGenerated){
                values.Value?.RemoveProcessor(this);
            }
            _chunksGenerated.Clear();
        }

        protected override void InitializeProcessor()
        {
            _chunksGenerated = new();
        }
        protected override void OnProcessRemoved(ChunkData chunk) => RemoveChunk(chunk);
        protected override void OnProcessAdded(ChunkData chunk) => AddChunk(chunk);
        public void RemoveChunk(ChunkData chunk)
        {
            Vector3Int finalCord = chunk.ID;
            if(_chunksGenerated.TryGetValue(finalCord, out CoordinateDataHolder data)){
                if(data != null){
                    data.RemoveProcessor(this);   
                    onProcessRemoved?.Invoke(data);
                }
                _chunksGenerated.Remove(finalCord);
            }
        }

        public void AddChunk(ChunkData chunk){
            Vector3Int finalCord = chunk.ID;
            var coordinateData = chunk.GetData<CoordinateDataHolder>();
            coordinateData?.AddProcessor(this);        
            if (_chunksGenerated.TryAdd(finalCord, coordinateData)){
                onProcessDone?.Invoke(coordinateData);
            }
            else 
                Debug.LogWarning("Can't add new chunk " + finalCord.ToString());

        }

        #region APIs
        /// <summary>
        /// Get all the CoordinateDataHolder of a chunk in a grid position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public CoordinateDataHolder GetHolderAtGrid(Vector2 position)
        {
            if(infiniteLands.TryGetChunkDataAtGridPosition(position, _chunksGenerated, out var chunk)){
                return chunk;
            }
            return null;
        }

        /// <summary>
        /// Get all the CoordinateDataHolder of a chunk in a world position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public CoordinateDataHolder GetHolderAt(Vector3 position)
        {
            Vector3 flattened = infiniteLands.WorldToLocalPoint(position);
            CoordinateDataHolder result = GetHolderAtGrid(new Vector2(flattened.x, flattened.z));
            return result;
        }

        /// <summary>
        /// Get a single CoordinateData in a specific grid position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="foundData"></param>
        /// <param name="interpolated"></param>
        /// <returns></returns>
        public CoordinateData GetCoordinateDataAtGrid(Vector2 position, out bool foundData, bool interpolated)
        {
            CoordinateDataHolder coordinateDataHolder = GetHolderAtGrid(position);
            if(coordinateDataHolder != null){
                foundData = true;
                return coordinateDataHolder.GetCoordinateDataAtGrid(position, interpolated);
            }
            foundData = false;
            return CoordinateData.Default;
        }

        /// <summary>
        /// Get a single CoordinateData in a specific world position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="foundData"></param>
        /// <param name="interpolated"></param>
        /// <param name="inWorldSpace"></param>
        /// <returns></returns>
        public CoordinateData GetCoordinateDataAt(Vector3 position, out bool foundData, bool interpolated = false, bool inWorldSpace = true)
        {
            if(infiniteLands==null){
                foundData = false;
                return default;
            }
            Vector3 flattened = infiniteLands.WorldToLocalPoint(position);
            CoordinateData result = GetCoordinateDataAtGrid(new Vector2(flattened.x, flattened.z), out foundData, interpolated);

            if(inWorldSpace)
                return result.ApplyMatrix(infiniteLands.localToWorldMatrix);
            return result;
        }

        #endregion
    }
}