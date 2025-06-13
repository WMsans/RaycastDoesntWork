using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    [RequireComponent(typeof(PointStore))]
    public class TerrainPainter : ChunkProcessor<ChunkData>, IGenerate<TextureResult>
    {
        public bool UseMaximumTextureResolution = true;
        [HideIf(nameof(UseMaximumTextureResolution))][Min(8)] public int Width = 256;
        [HideIf(nameof(UseMaximumTextureResolution))][Min(8)] public int Height = 256;
        [HideIf(nameof(UseMaximumTextureResolution))][Min(8)] public int MipCount = 10;
        public Material TerrainMaterial;
        private TextureArrayPool textureArrayPool;
        private TerrainTextures textureArrays;
        private UnityEngine.Pool.ObjectPool<Material> _materialPool;
        private CompactFullExporter exporter;

        public Action<TextureResult> onProcessDone { get; set; }
        public Action<TextureResult> onProcessRemoved { get; set; }

        private HashSet<Vector3Int> TextureToRemove = new();
        private Dictionary<Vector3Int, TextureResult> ReloadableRequests = new();
        private List<TextureProcess> TexturesToProcess = new();
        private List<Material> Orphans = new();
        private List<Texture2D> TexturesToArray = new();
        private IHoldTextures[] PreviousTextures;

        public override void OnValidate()
        {
            ReassignMaterials();
        }

        protected override void InitializeProcessor()
        {
#if UNITY_EDITOR
            TextureReloader.OnSaveAnyAsset -= ReassignMaterials;
            TextureReloader.OnSaveAnyAsset += ReassignMaterials;
#endif

            ReloadableRequests.Clear();
            Orphans.Clear();

            if (TerrainMaterial == null)
            {
                Debug.LogWarningFormat("No material has been set in {0}. Creating a temporal one", nameof(TerrainMaterial));
                TerrainMaterial = new Material(Resources.Load<Material>("Materials/InfiniteLandsDefault"));
            }

            if (_materialPool == null)
                _materialPool = new UnityEngine.Pool.ObjectPool<Material>(CreateMaterial, actionOnDestroy: AdaptiveDestroy);

            UpdateTextureArray();
            UpdateMaterialsOfAssets();
        }

        private void UpdateMaterialsOfAssets()
        {
            var graph = infiniteLands.graph;
            if (graph == null)
                return;
            var materialsRequired = graph.GetAssets().OfType<IHoldMaterials>().SelectMany(a => a.GetMaterials()).Distinct();
            foreach (var material in materialsRequired)
            {
                AssignTexturesToMaterials(material);
            }
        }

        void DisableTextureArrays()
        {
            if (textureArrays != null)
            {
                textureArrays.Release();
                textureArrays.OnTextureAssetModified -= OnGraphUpdated;
                textureArrays = null;
            }

            foreach (var request in ReloadableRequests)
            {
                var result = request.Value;
                textureArrayPool?.Release(result.TextureMasksArray);
            }

            if (textureArrayPool != null)
            {
                textureArrayPool.Dispose();
                textureArrayPool = null;
            }
            PreviousTextures = null;
        }
        protected override void DisableProcessor()
        {
            DisableTextureArrays();

            foreach (var result in TexturesToProcess)
            {
                result.job.Complete();
                result.SplatMaps.Return();
                result.finalData.RemoveProcessor(this);
                result.assetData.RemoveProcessor(this);
            }
            TexturesToProcess.Clear();

            foreach (var request in ReloadableRequests)
            {
                var result = request.Value;
                result.SplatMaps.Return();
                result.HeightMap.Return();
                _materialPool.Release(result.groundMaterial);
            }

            TextureToRemove.Clear();
            ReloadableRequests.Clear();

            if (exporter != null)
                exporter.DestroyTextures(AdaptiveDestroy);

            if (_materialPool != null)
            {
                if (_materialPool.CountActive > 0)
                    Debug.LogErrorFormat("Not all materials have been released {0}", _materialPool.CountActive);
                _materialPool.Dispose();
                _materialPool = null;
            }

            #if UNITY_EDITOR
            TextureReloader.OnSaveAnyAsset -= ReassignMaterials;
            #endif
        }

        public override void OnGraphUpdated()
        {
            if (infiniteLands == null || infiniteLands.graph == null)
                return;

            if (textureArrays != null && textureArrays.AssetsAreDirty)
            {
                textureArrays.Release();
                textureArrays.OnTextureAssetModified -= OnGraphUpdated;
                textureArrays = null;
            }

            UpdateTextureArray();
            UpdateMaterialsOfAssets();
            ReassignMaterials();
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            if (!TerrainMaterial)
                return;

            AssetData assetData = chunk.GetData<AssetData>();
            WorldFinalData finalData = chunk.GetData<WorldFinalData>();

            if (assetData == null || finalData == null)
                return;

            assetData.AddProcessor(this);
            finalData.AddProcessor(this);

            if (!exporter.namesInitialized)
            {
                var tempList = ListPoolLight<(IAsset, int)>.Get();
                for (int i = 0; i < assetData.assetWithIndex.Length; i++)
                {
                    var assetWithIndex = assetData.assetWithIndex[i];
                    if (typeof(IHoldTextures).IsAssignableFrom(assetWithIndex.Item1.GetType()))
                        tempList.Add(assetWithIndex);
                }
                exporter.InitializeNames(tempList);
                ListPoolLight<(IAsset, int)>.Release(tempList);
            }

            ExportedMultiResult SplatMaps = exporter.GenerateDensityTextures(default, assetData);

            ExportedMultiResult HeightMap = default;
            if (!exporter.HasItems)
            {
                HeightMap = exporter.GenerateHeightTexture(finalData);
            }

            JobHandle job = JobHandle.CombineDependencies(SplatMaps.job, HeightMap.job);
            TextureProcess textureProcess = new TextureProcess(assetData, finalData, chunk.meshSettings, chunk.terrainConfig, SplatMaps, HeightMap, job);
            TexturesToProcess.Add(textureProcess);
            if (infiniteLands.InstantProcessors)
                UpdateRequests(true);
        }

        protected override void OnProcessRemoved(ChunkData chunk)
        {
            foreach (var texture in TexturesToProcess)
            {
                if (texture.terrainConfig.ID.Equals(chunk.ID))
                {
                    TextureToRemove.Add(chunk.ID);
                    break;
                }
            }

            if (ReloadableRequests.TryGetValue(chunk.ID, out TextureResult result))
            {
                result.SplatMaps.Return();
                result.HeightMap.Return();

                _materialPool.Release(result.groundMaterial);
                textureArrayPool?.Release(result.TextureMasksArray);
                onProcessRemoved?.Invoke(result);
            }
            ReloadableRequests.Remove(chunk.ID);
        }

        public override void Update()
        {
            UpdateRequests(false);
        }

        void UpdateRequests(bool instantApply)
        {
            if (TexturesToProcess.Count <= 0)
                return;

            for (int i = TexturesToProcess.Count - 1; i >= 0; i--)
            {
                TextureProcess process = TexturesToProcess[i];
                if (process.job.IsCompleted || instantApply)
                {
                    process.job.Complete();
                    Vector3Int ID = process.terrainConfig.ID;
                    if (TextureToRemove.Contains(ID))
                    {
                        process.SplatMaps.Return();
                        TextureToRemove.Remove(ID);
                    }
                    else
                    {
                        Material mat = _materialPool?.Get();
                        AssignTexturesToMaterials(mat);
                        Texture2DArray texture2DArray = null;
                        if (textureArrayPool != null)
                        {
                            TexturesToArray.Clear();
                            for (int x = 0; x < process.SplatMaps.textures.Count; x++)
                            {
                                TexturesToArray.Add(process.SplatMaps.textures[x].ApplyTexture());
                            }
                            texture2DArray = textureArrayPool?.GetConfiguredArray("Masks", TexturesToArray);
                        }
                        TextureResult result = new TextureResult(process.meshSettings, process.terrainConfig,
                            process.SplatMaps, process.HeightMap, mat, texture2DArray);

                        ReloadableRequests.TryAdd(ID, result);
                        onProcessDone?.Invoke(result);
                    }
                    process.finalData.RemoveProcessor(this);
                    process.assetData.RemoveProcessor(this);
                    TexturesToProcess.RemoveAt(i);
                }
            }
        }


        private void ReassignMaterials()
        {
            foreach (KeyValuePair<Vector3Int, TextureResult> reassignable in ReloadableRequests)
            {
                TextureResult reques = reassignable.Value;
                reques.ReloadMaterial();
            }

            foreach (Material material in Orphans)
            {
                if (textureArrays != null)
                {
                    textureArrays.ApplyTextureArrays(material);
                }
            }

        }

        private Material CreateMaterial()
        {
            return new(TerrainMaterial);
        }

        public void AssignTexturesToMaterials(Material material)
        {
            if (material == null)
                return;

            if (textureArrays != null)
            {
                textureArrays.ApplyTextureArrays(material);
            }

            Orphans.Add(material);
        }

        public uint[] ExtractTexturesMask(List<TextureAsset> textures)
        {
            var previousTextures = infiniteLands.graph.GetAssets()
                .OfType<IHoldTextures>();
            int length = previousTextures.Count();
            if (length == 0)
                return new uint[1] { 0 };

            uint[] mask = new uint[(length + 31) / 32];
            if (textures == null || textures.Count == 0)
                return mask;

            foreach (TextureAsset texture in textures)
            {
                if (texture == null)
                    continue;

                uint index = GetTextureIndex(previousTextures, texture, out bool matched);
                if (matched)
                    mask[index / 32] |= 1u << (int)(index % 32);
            }
            return mask;
        }

        private uint GetTextureIndex(IEnumerable<IHoldTextures> previousTextures, TextureAsset texture, out bool match)
        {
            var found = previousTextures
                .Select((t, i) => new { Texture = t, Index = (uint)i })
                .FirstOrDefault(t => t.Texture != null && texture.Equals(t.Texture));
            match = found != null;
            return found?.Index ?? 0;
        }

        public void AssignTexturesToMaterials(CommandBuffer bf, ComputeShader compute, int kernelIndex, IHoldVegetation.ColorSamplingMode colorSamplingMode)
        {
            if (textureArrays != null)
            {
                textureArrays.ApplyTextureArrays(bf, compute, kernelIndex, colorSamplingMode);
            }
        }

        public bool TryGetDataAt(Vector2 position, out TextureResult data)
        {
            return infiniteLands.TryGetChunkDataAtGridPosition(position, ReloadableRequests, out data);
        }

        private void UpdateTextureArray()
        {
            var MeshScale = infiniteLands.meshSettings.MeshScale;
            var graph = infiniteLands.graph;
            if (graph == null)
                return;

            IEnumerable<IHoldTextures> textures = graph.GetAssets().OfType<IHoldTextures>();
            var settings = infiniteLands.meshSettings;
            if (PreviousTextures == null || (PreviousTextures != null && !PreviousTextures.SequenceEqual(textures)) || textureArrays == null)
            {
                DisableTextureArrays();
                ResetExporter(settings);

                if (textures.Count() > 0)
                {
                    if (textureArrays == null)
                    {
                        TextureResolution TextureResolution = new TextureResolution()
                        {
                            MipCount = MipCount,
                            UseMaximumResolution = UseMaximumTextureResolution,
                            Width = Width,
                            Height = Height,
                        };

                        textureArrays = new TerrainTextures(textures, AdaptiveDestroy, TerrainMaterial?.shader, TextureResolution, MeshScale);
                        textureArrays.OnTextureAssetModified += OnGraphUpdated;
                    }
                    if (textureArrayPool == null)
                    {
                        int size = exporter.GetTextureResolution() + 1;
                        int count = (textures.Count() + 3) / 4; // Equivalent to Mathf.CeilToInt(originals.Length / 4f)
                        textureArrayPool = new TextureArrayPool(size, size, 1, count, false, AdaptiveDestroy, true);
                    }
                }

            }
            else if (exporter != null && exporter.GetTextureResolution() != settings.TextureResolution)
            {
                ResetExporter(settings);
            }

            PreviousTextures = textures.ToArray();
        }
        private void ResetExporter(MeshSettings settings)
        {
            exporter?.DestroyTextures(AdaptiveDestroy);
            exporter = new CompactFullExporter(settings.TextureResolution, settings.Resolution);
            exporter.Reset();
        }
    }
}