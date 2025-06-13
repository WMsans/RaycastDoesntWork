using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace sapra.InfiniteLands
{
    public class ExporterGenerator 
    {
        IGraph generator;
        public ExporterGenerator(IGraph generator){
            this.generator = generator;
        }
        public List<Texture2D> GenerateAndExportWorld(int EditorResolution, float MeshScale, int Seed, Vector2 WorldOffset, IExportTextures exporter)
        {
            TerrainConfiguration config = new TerrainConfiguration
            {
                Position = WorldOffset,
                TerrainNormal = Vector3.up,
            };
            
            MeshSettings meshSettings = new MeshSettings
            {
                Resolution = EditorResolution,
                MeshScale = MeshScale,
                Seed = Seed,
                meshType = MeshSettings.MeshType.Normal
            };

            //DeepRestart();
            generator.ValidationCheck();
            
            WorldGenerator worldGenerator = new WorldGenerator(generator, false);
            WorldGenerationData chunk = GenericPoolLight<WorldGenerationData>.Get();
            chunk.Reuse(worldGenerator, config, meshSettings);
            chunk.ForceComplete();

            var chunkData = chunk.Result;

            List<Texture2D> texturesToExport = new List<Texture2D>();
            WorldFinalData worldFinalData = chunkData.GetData<WorldFinalData>(true);
            AssetData assetData = chunkData.GetData<AssetData>(true);

            var heightResult = exporter.GenerateHeightTexture(worldFinalData);
            heightResult.job.Complete();
            texturesToExport.AddRange(heightResult.textures.Select(a => a.ApplyTexture()));
            
            var result = exporter.GenerateDensityTextures(assetData.assetWithIndex, assetData);
            result.job.Complete();
            texturesToExport.AddRange(result.textures.Select(a => a.ApplyTexture()));
            GenericPoolLight.Release(chunk);
            
            chunkData.Return();
            worldGenerator.Dispose(default);
            return texturesToExport;
        }
    }
}