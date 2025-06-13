using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class ExpandedExporter : IExportTextures
    {
        public string description => "Export the textures individually as a greyscale";

        public IBurstTexturePool texturePool;
        public void SetExporterResolution(int resolution)
        {
            texturePool = new BurstTexturePool(resolution);
        }

        public ExportedMultiResult GenerateHeightTexture(WorldFinalData finalData)
        {
            string[] names = new string[]{
                "Normal Map",
                string.Format("Height Map (Min{0}Max{1})",finalData.MinMaxHeight.x, finalData.MinMaxHeight.y),
            };

            List<BurstTexture> maps = texturePool.GetTexture(names, FilterMode.Bilinear, TextureFormat.RGBAFloat);

            NativeArray<Vertex4> reinterpreted = finalData.FinalPositions.Reinterpret<Vertex4>(Vertex.size);
            JobHandle finalTextureJob;

            finalTextureJob = MTJHeightSeparated.ScheduleParallel(reinterpreted,
                maps[0].GetRawData<Color>(), maps[1].GetRawData<Color>(), finalData.MinMaxHeight, texturePool.GetTextureResolution(), finalData.jobHandle);
            
            return new ExportedMultiResult(maps, texturePool, finalTextureJob);
        }

        public ExportedMultiResult GenerateDensityTextures(IEnumerable<(IAsset, int)> assets, AssetData data)
        {
            string[] names = assets.Select(a => a.Item1.name).ToArray();

            //Generate density textures
            List<BurstTexture> masks = texturePool.GetTexture(names, FilterMode.Bilinear);
            NativeArray<JobHandle> TextureCreationJob = new NativeArray<JobHandle>(names.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < names.Length; i++)
            {
                NativeArray<Color32> rawTexture = masks[i].GetRawData<Color32>();
                TextureCreationJob[i] = MTJTextureSingleChannel.ScheduleParallel(data.map, rawTexture,
                    i, data.MapPartLength, texturePool.GetTextureResolution(), data.jobHandle);
            }

            JobHandle textureCreated = JobHandle.CombineDependencies(TextureCreationJob);
            TextureCreationJob.Dispose();
            return new ExportedMultiResult(masks,texturePool, textureCreated);
        }

        public void DestroyTextures(Action<UnityEngine.Object> Destroy) => texturePool.DestroyBurstTextures(Destroy);
        public int GetTextureResolution() => texturePool.GetTextureResolution();
        public void ResetExporter(){}

    }
}