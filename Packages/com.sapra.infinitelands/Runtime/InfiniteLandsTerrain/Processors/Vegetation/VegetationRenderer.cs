using System.Collections.Generic;
using UnityEngine;

using System.Linq;

using UnityEngine.Rendering;
using UnityEditor;
using System;

namespace sapra.InfiniteLands
{
    [RequireComponent(typeof(PointStore))]
    [ExecuteAlways]
    public class VegetationRenderer : ChunkProcessor<ChunkData>, IGenerate<VegetationResult>
    {
        public static int textureIsSetID = Shader.PropertyToID("_DisplacementTextureIsSet");
        public static ComputeShader 
            CalculatePositions,
            CalculateTextures,
            ValidateTextureIndices,
            VisibilityCheck,
            AppendingInstances,
            FillArguments;

        public static LocalKeyword
            ShadowKeyword,
            VisibilityShadows,
            CullingKeyword,
            SmallObjectMode;
        
        public static int 
            CalculatePositionsKernel,
            CalculateTexturesKernel,
            OriginShiftKernel,
            VisibilityCheckKernel,
            ValidateTextureIndicesKernel,
            InitialCompactKernel,
            AppenderKernel,
            ResetKernel,
            CountKernel,
            SumKernel,
            CompactKernel,
            FillKernel;

        public static LocalKeyword[] LightKeywords;
        public static LocalKeyword[] ShadowKeywords;
        
        [Layer] public int RenderInLayer;
        public List<RenderingSettings> renderingSettings = new();
        public int DensityPerSize = 800;
        public bool RenderVegetation = true;
        public bool SpawnCollidersAroundCameras = false;

        private bool CullingEnabled = true;
        private bool GlobalRendering = true;

        [Header("Displacement")] 
        public bool DisplaceWithMovement;
        [ShowIf("DisplaceWithMovement")] public Transform PlayerCenter;
        [ShowIf("DisplaceWithMovement")] public LayerMask CullMask;
        [ShowIf("DisplaceWithMovement")] [Min(10)] public float DisplaceDistance = 100;
        [ShowIf("DisplaceWithMovement")] public bool VisualizeDisplacement;

        private RenderTexture[] DepthTextures;
        private MaterialPropertyBlock PropertyBlock;
        private List<MultiCameraManager> VegetationLoaders = new();

        private FloatingOrigin floatingOrigin;
        private ViewSettings renderingManager;
        private bool Initialized;

        private ProfilingSampler OriginShiftSampler;
        private List<VegetationSettings> vegSettings = new();

        private CompactFullExporter exporter;
        private Dictionary<Vector3Int, VegetationResult> ReloadableRequests = new();
        private List<VegetationProcess> VegetationToProcess = new();
        private HashSet<Vector3Int> VegetationToRemove = new();

        public Action<VegetationResult> onProcessDone { get; set; }
        public Action<VegetationResult> onProcessRemoved { get; set; }

        private Transform VegetationObjects;
        private Vector2 LocalGridOffset;

        private void GetShaderData() {
            CalculatePositions = Resources.Load<ComputeShader>("Computes/CalculatePositions");
            CalculateTextures = Resources.Load<ComputeShader>("Computes/CalculateTextures");
            ValidateTextureIndices = Resources.Load<ComputeShader>("Computes/ValidateTextureIndices");
            FillArguments = Resources.Load<ComputeShader>("Computes/CountCompact");
            VisibilityCheck = Resources.Load<ComputeShader>("Computes/VisibilityCheck");
            AppendingInstances = Resources.Load<ComputeShader>("Computes/AppendIndices");

            ValidateTextureIndicesKernel = ValidateTextureIndices.FindKernel("ValidateTextureIndices");

            CalculatePositionsKernel = CalculatePositions.FindKernel("CalculatePositions");
            CalculateTexturesKernel = CalculateTextures.FindKernel("CalculateTextures");

            OriginShiftKernel = CalculatePositions.FindKernel("OriginShift");
            VisibilityCheckKernel = VisibilityCheck.FindKernel("VisibilityCheck");

            InitialCompactKernel = FillArguments.FindKernel("InitialCompact");
            ResetKernel = FillArguments.FindKernel("Reset");
            CountKernel = FillArguments.FindKernel("Count");
            SumKernel = FillArguments.FindKernel("Sum");
            CompactKernel = FillArguments.FindKernel("Compact");
            FillKernel = FillArguments.FindKernel("FillArguments");
            AppenderKernel = AppendingInstances.FindKernel("AppendIndices");

            LightKeywords = new LocalKeyword[3];
            for(int i = 0; i < 3; i++){
                LightKeywords[i] = new LocalKeyword(AppendingInstances, string.Format("LIGHT_{0}", i+1));
            }

            ShadowKeywords = new LocalKeyword[3];
            for(int i = 0; i < 3; i++){
                ShadowKeywords[i] = new LocalKeyword(AppendingInstances, string.Format("SHADOW_{0}", i+1));
            }

            ShadowKeyword = new LocalKeyword(FillArguments, "SHADOWS");
            VisibilityShadows = new LocalKeyword(VisibilityCheck, "SHADOWS_ENABLED");
            SmallObjectMode = new LocalKeyword(VisibilityCheck, "SMALL_OBJECT");
            CullingKeyword = new LocalKeyword(VisibilityCheck, "CULLING");
            OriginShiftSampler = new ProfilingSampler("Shifting Origin");
        }

        private void UpdateRenderingSettings(){
            if(vegSettings.Count > 0 && renderingSettings.Count > 0){
                foreach(VegetationSettings setting in vegSettings){
                    UpdateRenderingSettings(setting);
                }
            }
        }

        private void UpdateRenderingSettings(VegetationSettings setting){
            var targetSetting = renderingSettings.FirstOrDefault(a => setting.DistanceBetweenItems > a.MinDistanceBetweenItems 
                && (a.MaxDistanceBetweenItems < 0 || setting.DistanceBetweenItems < a.MaxDistanceBetweenItems));
            if(targetSetting != null)
                setting.UpdateRenderingDistance(targetSetting.ViewDistance);
            else
                setting.Reset();        
        }
        public override void OnValidate() {
            UpdateRenderingSettings();
        }
        protected override void DisableProcessor()
        {
            UnTrackVegetationAssets();
            
            VegetationLoaders = null;
            Initialized = false;

            if(VegetationToProcess != null){
                foreach(var result in VegetationToProcess){
                    result.job.Complete();
                    
                    result.VegetationSplatMap.Return();
                    result.HeightMap.Return();
                    result.assetData.RemoveProcessor(this);        
                    result.worldFinalData.RemoveProcessor(this);
                }
                VegetationToProcess.Clear();
            }
            if(ReloadableRequests != null){
                foreach(var request in ReloadableRequests){
                    var result = request.Value;
                    result.HeightMap.Return();
                    result.VegetationSplatMap.Return();
                }
                ReloadableRequests.Clear();
            }

            VegetationToRemove?.Clear();
            if(exporter != null)
                exporter.DestroyTextures(AdaptiveDestroy);
            
            if(VegetationObjects != null){
                AdaptiveDestroy(VegetationObjects.gameObject);
                VegetationObjects = null;
            }
            EventsCleanup();
        }

        private void EventsCleanup(){
        
            if(floatingOrigin != null)
                floatingOrigin.OnOriginMove -= OnOriginShift;
            if(renderingManager != null){
                renderingManager.OnCameraAdded -= AddNewCamera;
                renderingManager.OnCameraRemoved -= RemoveCamera;
                renderingManager.OnTransformAdded -= AddNewTransform;
                renderingManager.OnTransformRemoved -= RemoveTransform;
            }
        }

        protected override void InitializeProcessor()
        {   
            var settings = infiniteLands.meshSettings;
            var graph = infiniteLands.graph;
            LocalGridOffset = infiniteLands.localGridOffset;
            if(graph == null)
                return;
            EventsCleanup();

            renderingManager = GetComponent<ViewSettings>();
            renderingManager.OnCameraAdded += AddNewCamera;
            renderingManager.OnCameraRemoved += RemoveCamera;
            renderingManager.OnTransformAdded += AddNewTransform;
            renderingManager.OnTransformRemoved += RemoveTransform;

            if(exporter == null || exporter.GetTextureResolution() != settings.TextureResolution){
                exporter?.DestroyTextures(AdaptiveDestroy);
                exporter = new CompactFullExporter(settings.TextureResolution,settings.Resolution);
            }
            exporter.Reset();

            GetShaderData();
        
            floatingOrigin = GetComponent<FloatingOrigin>();
            if(floatingOrigin != null){
                floatingOrigin.OnOriginMove += OnOriginShift;
            }
            
            PrepareCamerasAndDepthTextures();
            PrepareAssets(graph, settings);
            PrepareDisplacementTexture();

            PropertyBlock = new MaterialPropertyBlock();
            Initialized = true;
        }

        #region Dynamic Modifications
        private void AddNewTransform(Transform body){
            foreach(var loader in VegetationLoaders){
                loader.AddTransform(body);
            }
        }

        private void RemoveTransform(Transform body){
            foreach(var loader in VegetationLoaders){
                loader.RemoveTransform(body);
            }
        }
        
        private void AddNewCamera(Camera cam){
            foreach(var loader in VegetationLoaders){
                loader.AddCamera(cam);
                loader.AddTransform(cam.transform);
            }
        }

        private void RemoveCamera(Camera cam){
            foreach(var loader in VegetationLoaders){
                loader.RemoveCamera(cam);
                loader.RemoveTransform(cam.transform);
            }
        }

        public void AddNewRenderSettings(RenderingSettings settings){
            renderingSettings.Add(settings);
            UpdateRenderingSettings();
        }

        public void RemoveRenderSettings(RenderingSettings settings){
            renderingSettings.Remove(settings);
            UpdateRenderingSettings();
        }      

        public void SetRendering(bool value){
            RenderVegetation = value;
        }
        #endregion

        public void OnOriginShift(Vector3Double newOrigin, Vector3Double previousOrigin){
            if(VegetationLoaders == null || VegetationLoaders.Count <= 0)
                return;
            CommandBuffer bf = CommandBufferPool.Get("Vegetation Renderer");
            ComputeShader compute = CalculatePositions;
            int kernel = OriginShiftKernel;
            Vector3 offset = previousOrigin - newOrigin;

            bf.SetComputeVectorParam(compute, "_OriginOffset", offset);
            using(new ProfilingScope(bf, OriginShiftSampler))
            {
                foreach(MultiCameraManager renderer in VegetationLoaders){
                    renderer.OnOriginShift(bf,compute, kernel, offset);
                    
                }
            }
            Graphics.ExecuteCommandBuffer(bf);
            CommandBufferPool.Release(bf);
        }
        public override void OnGraphUpdated()
        {
            Disable();
            Initialize(infiniteLands);
        }

        private void Reload()
        {
            InitializeProcessor();
            Debug.Log("called");
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            AssetData assetData = chunk.GetData<AssetData>();
            WorldFinalData worldFinalData = chunk.GetData<WorldFinalData>();
            if(assetData == null || worldFinalData == null)
                return;
            assetData.AddProcessor(this);
            worldFinalData.AddProcessor(this);

            if (!exporter.namesInitialized)
            {
                var tempList = ListPoolLight<(IAsset, int)>.Get();
                for (int i = 0; i < assetData.assetWithIndex.Length; i++)
                {
                    var assetWithIndex = assetData.assetWithIndex[i];
                    if (typeof(IHoldVegetation).IsAssignableFrom(assetWithIndex.Item1.GetType()))
                        tempList.Add(assetWithIndex);
                }
                exporter.InitializeNames(tempList);
                ListPoolLight<(IAsset, int)>.Release(tempList);
            }

            ExportedMultiResult resultVegetation = exporter.GenerateDensityTextures(default, assetData);
            ExportedMultiResult NormalAndHeightTexture = exporter.GenerateHeightTexture(worldFinalData);
            VegetationToProcess.Add(new VegetationProcess(assetData, worldFinalData, resultVegetation, NormalAndHeightTexture, chunk.terrainConfig,chunk.meshSettings));
            if(infiniteLands.InstantProcessors)
                UpdateRequests(true);
        }

        protected override void OnProcessRemoved(ChunkData chunk){
            foreach(var vegetation in VegetationToProcess){
                if(vegetation.TerrainConfiguration.ID.Equals(chunk.ID)){
                    VegetationToRemove.Add(chunk.ID);
                    break;
                }
            }

            if(ReloadableRequests.TryGetValue(chunk.ID, out VegetationResult result)){
                result.VegetationSplatMap.Return();
                result.HeightMap.Return();
                onProcessRemoved?.Invoke(result);
            }
            ReloadableRequests.Remove(chunk.ID);
        }

        public bool TryGetDataAt(Vector2 position, out VegetationResult result){
            return infiniteLands.TryGetChunkDataAtGridPosition(position, ReloadableRequests, out result);
        }

        public override void Update(){
            UpdateRequests(false);
        }

        void UpdateRequests(bool instantApply){
            if(VegetationToProcess == null || VegetationToProcess.Count <= 0)
                return;

            for(int i = VegetationToProcess.Count-1; i >= 0; i--){
                VegetationProcess process = VegetationToProcess[i];
                if(process.job.IsCompleted || instantApply){
                    process.job.Complete();
                    Vector3Int ID = process.TerrainConfiguration.ID;
                    if(VegetationToRemove.Contains(ID)){
                        process.VegetationSplatMap.Return();
                        process.HeightMap.Return();
                        VegetationToRemove.Remove(ID);
                    }   
                    else{
                        VegetationResult result = new VegetationResult(process.TerrainConfiguration, process.MeshSettings, 
                            process.VegetationSplatMap, process.HeightMap);
                        process.assetData.RemoveProcessor(this);
                        process.worldFinalData.RemoveProcessor(this);
                        ReloadableRequests.TryAdd(ID, result);
                        onProcessDone?.Invoke(result);
                    }

                    VegetationToProcess.RemoveAt(i);
                }
            }
        }

        void PrepareAssets(IGraph generator, MeshSettings settings){
            if(generator == null)
                return;

            UnTrackVegetationAssets();
            var cameras = FilterCameras(renderingManager.GetCurrentCameras());

            var transforms = new List<Transform>(renderingManager.GetCurrentTransforms());
            if(SpawnCollidersAroundCameras)
                transforms.AddRange(transforms.Concat(cameras.Select(a => a.transform)));

            VegetationLoaders = new List<MultiCameraManager>();
            Vector2 localGridOffset = MapTools.GetOffsetInGrid(infiniteLands.localGridOffset, settings.MeshScale);
            vegSettings.Clear();    

            int index = 0;
            var Sets = generator.GetAssets().OfType<IHoldVegetation>();
            foreach(IHoldVegetation set in Sets){
                var positionData = set.GetPositionData();
                if(positionData.distanceBetweenItems > 5 && set is UpdateableSO updateable){
                    updateable.OnValuesUpdated += Reload;
                }
                
                int textureIndex = index/4;
                int subIndex = index - Mathf.FloorToInt(index / 4) * 4;

                VegetationSettings vegetationSettings = new VegetationSettings(settings.MeshScale, index, 
                    DensityPerSize, localGridOffset, positionData.distanceBetweenItems, positionData.viewDistance,
                    GlobalRendering, CullingEnabled, textureIndex, subIndex, RenderInLayer);
                UpdateRenderingSettings(vegetationSettings);

                vegSettings.Add(vegetationSettings);
                MultiCameraManager loader = new MultiCameraManager(set, vegetationSettings, cameras, transforms, infiniteLands);
                VegetationLoaders.Add(loader);
                index++;            
            }
        }

        private void UnTrackVegetationAssets()
        {
            if (VegetationLoaders != null && VegetationLoaders.Count > 0)
            {
                foreach (MultiCameraManager manager in VegetationLoaders)
                {
                    IHoldVegetation set = manager.VegetationAsset;
                    if (set is UpdateableSO updateable)
                    {
                        updateable.OnValuesUpdated -= Reload;
                    }
                    manager.Dispose();
                }
            }
        }

        #if UNITY_EDITOR
        private IEnumerable<Camera> FilterCameras(IEnumerable<Camera> originalCameras)
        {
            IEnumerable<Camera> target = originalCameras.Where(a => (a.cullingMask & (1 << RenderInLayer)) != 0);
            GlobalRendering = (RuntimeTools.IsSceneViewOpenAndFocused() && RuntimeTools.IsGameViewOpenAndFocused()) || !Application.isPlaying;
            if (RuntimeTools.IsGameViewOpenAndFocused())
            {
                CullingEnabled = true;
            }
            else
            {
                CullingEnabled = false;
            }
            return target;
        }
        #else
        private IEnumerable<Camera> FilterCameras(IEnumerable<Camera> originalCameras){
            CullingEnabled = true;
            GlobalRendering = false;           
            return originalCameras.Where(a => (a.cullingMask & (1 << RenderInLayer)) != 0);
        }
        #endif

        void PrepareCamerasAndDepthTextures(){
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(a => a.type == LightType.Directional);
            if(lights.Count() > 1)
                Debug.LogError("There are too many directional lights on scene, this can return in problems when applying Frustrum Culling to the sahows");
            foreach(Light light in lights){
                var current = light.GetComponent<SetGlobalLightDirection>();
                if(!current)
                    current = light.gameObject.AddComponent<SetGlobalLightDirection>();
            }

/*             DepthTextures = new RenderTexture[Cameras.Count];
            for (int i = 0; i < Cameras.Count; i++)
            {
                Camera cam = Cameras[i];
                if (!cam.TryGetComponent(out CreateDepthTexture depthCreator))
                {
                    depthCreator = cam.gameObject.AddComponent<CreateDepthTexture>();
                }

                DepthTextures[i] = depthCreator.DepthTexture;
            } */
        }
        
        void PrepareDisplacementTexture(){
            DisplaceWithMovement = DisplaceWithMovement && GraphicsSettings.defaultRenderPipeline == null;
            if (DisplaceWithMovement)
            {
                Shader.SetGlobalInt(textureIsSetID, 1);
                if(PlayerCenter == null)    
                    PlayerCenter = this.transform;
                CreateDisplacementTexture Displacer = gameObject.GetComponentInChildren<CreateDisplacementTexture>();
                if (Displacer == null)
                {
                    GameObject DisplacerGO = RuntimeTools.FindOrCreateObject("Vegetation Displacer", transform);
                    Displacer = DisplacerGO.AddComponent<CreateDisplacementTexture>();
                }
                
                Displacer.Initialize(infiniteLands, PlayerCenter, CullMask, DisplaceDistance, VisualizeDisplacement);
            }
            else
                Shader.SetGlobalInt(textureIsSetID, 0);

        }

        public override void LateUpdate()
        {
            if(!Initialized || !RenderVegetation)
                return;

            if(VegetationLoaders == null)
                return;

            PropertyBlock.Clear();
            bool AnyoneCreatedData = false;
            foreach (MultiCameraManager vegetation in VegetationLoaders)
            {   
                bool HasCreatedData = vegetation.Update(PropertyBlock, AnyoneCreatedData);
                AnyoneCreatedData |= HasCreatedData;
            }
        }


        public override void OnDrawGizmos()
        {
            if(VegetationLoaders == null || !Initialized)
                return;

            foreach (MultiCameraManager vegetation in VegetationLoaders)
            {
                vegetation.OnDrawGizmos();
            }
        }

        internal Transform GetVegetationParent()
        {
            if(VegetationObjects == null){
                VegetationObjects = RuntimeTools.FindOrCreateObject("Vegetation Objects", transform).transform;
            }
            return VegetationObjects;
        }
    }
}