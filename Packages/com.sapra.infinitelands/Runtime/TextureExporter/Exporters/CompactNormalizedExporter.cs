using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class CompactNormalizedExporter : IExportTextures
    {
        public string description => "Export the textures compacted,  normalizing the height map";
        public IBurstTexturePool texturePool;
        public void SetExporterResolution(int resolution)
        {
            texturePool = new BurstTexturePool(resolution);
        }

        private string[] VectorizeNames(string[] originals)
        {
            int nameCount = Mathf.CeilToInt(originals.Length / 4f);
            string[] newNames = new string[nameCount];
            for (int i = 0; i < newNames.Length; i++)
            {
                string a = i * 4 < originals.Length ? originals[i * 4] : "";
                string b = i * 4 + 1 < originals.Length ? originals[i * 4 + 1] : "";
                string c = i * 4 + 2 < originals.Length ? originals[i * 4 + 2] : "";
                string d = i * 4 + 3 < originals.Length ? originals[i * 4 + 3] : "";

                newNames[i] = a + " - " + b + " - " + c + " - " + d;
            }

            return newNames;
        }
        public ExportedMultiResult GenerateHeightTexture(WorldFinalData finalData)
        {
            List<BurstTexture> burstTexture = texturePool.GetTexture(string.Format("Normal and Height Map (Min{0}Max{1})", finalData.MinMaxHeight.x, finalData.MinMaxHeight.y),  FilterMode.Bilinear, TextureFormat.RGBAFloat);
            NativeArray<Vertex4> reinterpreted = finalData.FinalPositions.Reinterpret<Vertex4>(Vertex.size);
            JobHandle finalTextureJob;

            finalTextureJob = MTJHeightNormalizedJob.ScheduleParallel(reinterpreted,
                burstTexture[0].GetRawData<Color>(), finalData.MinMaxHeight, texturePool.GetTextureResolution(), finalData.jobHandle);
            
            return new ExportedMultiResult(burstTexture, texturePool, finalTextureJob);

        }

        public ExportedMultiResult GenerateDensityTextures(IEnumerable<(IAsset, int)> assets, AssetData data)
        {
            string[] ogNames = assets.Select(a => a.Item1.name).ToArray();
            int[] ogIndices = assets.Select(a => a.Item2).Concat(Enumerable.Repeat(-1, 4)).ToArray();

            string[] names = VectorizeNames(ogNames);
            List<BurstTexture> masks = texturePool.GetTexture(names, FilterMode.Bilinear);
            //Generate density textures
            NativeArray<JobHandle> TextureCreationJob = new NativeArray<JobHandle>(names.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int count = ogNames.Length;
            for (int i = 0; i < names.Length; i++)
            {
                int offset = i*4;
                NativeArray<Color32> rawTexture = masks[i].GetRawData<Color32>();
                TextureCreationJob[i] = MTJVegetationJobFlat.ScheduleParallel(data.map, rawTexture,
                    ogIndices[offset], ogIndices[offset+1], ogIndices[offset+2], ogIndices[offset+3], 
                    data.MapPartLength, texturePool.GetTextureResolution(), data.jobHandle);
            }

            JobHandle textureCreated = JobHandle.CombineDependencies(TextureCreationJob);
            TextureCreationJob.Dispose();
            return new ExportedMultiResult(masks, texturePool, textureCreated);
        }

        public void DestroyTextures(Action<UnityEngine.Object> Destroy) => texturePool.DestroyBurstTextures(Destroy);
        public int GetTextureResolution() => texturePool.GetTextureResolution();
    }
}