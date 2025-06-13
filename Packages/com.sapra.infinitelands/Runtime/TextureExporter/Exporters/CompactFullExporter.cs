using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class CompactFullExporter : IExportTextures
    {
        private IBurstTexturePool texturePool;
        private IBurstTexturePool heightPool;

        public void SetExporterResolution(int resolution)
        {
            texturePool = new BurstTexturePool(resolution);
            heightPool = new BurstTexturePool(resolution);
        }
        private string[] vectorizedNames;
        private int[] ogIndices;
        public bool namesInitialized{ get; private set; }
        public bool HasItems => vectorizedNames.Length > 0;
        private string heightName;

        public CompactFullExporter(){}
        public CompactFullExporter(int resolution, int meshRes)
        {
            texturePool = new BurstTexturePool(resolution);
            heightPool = new BurstTexturePool(meshRes);
        }

        public void Reset(){
            namesInitialized = false;
            heightName = null;
        }

        public void InitializeNames(IEnumerable<(IAsset, int)> assets){
            string[] ogNames = assets.Select(a => a.Item1.name).ToArray();
            ogIndices = assets.Select(a => a.Item2).Concat(Enumerable.Repeat(-1, 4)).ToArray();
            vectorizedNames = VectorizeNames(ogNames);
            namesInitialized = true;
        }
        public string description => "Export the textures compacted, keeping the height map at full range";
        public ExportedMultiResult GenerateHeightTexture(WorldFinalData finalData)
        {
            if(heightName == null){
                heightName = string.Format("Normal and Height Map (Min{0}Max{1})", finalData.MinMaxHeight.x, finalData.MinMaxHeight.y);
            }
            List<BurstTexture> burstTexture = heightPool.GetTexture(heightName, FilterMode.Bilinear, TextureFormat.RGBAFloat);
            NativeArray<Vertex4> reinterpreted = finalData.FinalPositions.Reinterpret<Vertex4>(Vertex.size);
            JobHandle finalTextureJob;

            finalTextureJob = MTJHeightJob.ScheduleParallel(reinterpreted,
                burstTexture[0].GetRawData<Color>(), heightPool.GetTextureResolution(), finalData.jobHandle);
            
            return new ExportedMultiResult(burstTexture, heightPool, finalTextureJob);
        }
        
        private string[] VectorizeNames(string[] originals)
        {
            int nameCount = (originals.Length + 3) / 4; // Equivalent to Mathf.CeilToInt(originals.Length / 4f)
            string[] newNames = new string[nameCount];

            for (int i = 0; i < nameCount; i++)
            {
                int startIndex = i * 4;
                string a = startIndex < originals.Length ? originals[startIndex] : "";
                string b = startIndex + 1 < originals.Length ? originals[startIndex + 1] : "";
                string c = startIndex + 2 < originals.Length ? originals[startIndex + 2] : "";
                string d = startIndex + 3 < originals.Length ? originals[startIndex + 3] : "";

                newNames[i] = $"{a} - {b} - {c} - {d}";
            }

            return newNames;
        }

        public ExportedMultiResult GenerateDensityTextures(IEnumerable<(IAsset, int)> assets, AssetData data)
        {
            if (!namesInitialized)
            {
                InitializeNames(assets);
                Debug.Log("Manual initialization");
            }

            List<BurstTexture> masks = texturePool.GetTexture(vectorizedNames, FilterMode.Bilinear);
            //Generate density textures
            NativeArray<JobHandle> TextureCreationJob = new NativeArray<JobHandle>(vectorizedNames.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < vectorizedNames.Length; i++)
            {
                int offset = i*4;
                NativeArray<Color32> rawTexture = masks[i].GetRawData<Color32>();
                TextureCreationJob[i] = MTJVegetationJobFlat.ScheduleParallel(data.map, rawTexture,
                    ogIndices[offset], ogIndices[offset+1], ogIndices[offset+2], ogIndices[offset+3], 
                    data.MapPartLength, texturePool.GetTextureResolution(), data.jobHandle);
            }
            JobHandle textureCreated = JobHandle.CombineDependencies(TextureCreationJob);
            TextureCreationJob.Dispose();
            return new ExportedMultiResult(masks,texturePool, textureCreated);
        }

        public void DestroyTextures(Action<UnityEngine.Object> Destroy) => texturePool.DestroyBurstTextures(Destroy);
        public int GetTextureResolution() => texturePool.GetTextureResolution();
    }
}