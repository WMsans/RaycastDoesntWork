using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    [EditorForClass(typeof(HeightData))]
    public class HeightDataPreview : OutputPreview
    {
        private BurstTexture texture;
        private readonly PointInstanceVisualizer previousVisualizer;
        private bool LocalMode;

        public HeightDataPreview(PortData targetPort, InfiniteLandsNode node, NodeView nodeView) : base(targetPort, node, nodeView)
        {
            previousVisualizer = new PointInstanceVisualizer(Color.red, true);
        }

        public override VisualElement GetPreview(BranchData settings, GraphSettings graphSettings) => GetPreview(settings, graphSettings, true);

        public VisualElement GetPreview(BranchData settings, GraphSettings graphSettings, bool button)
        {
            if (!Node.isValid)
                return null;

            // Fetch data and resources
            var foundData = Node.TryGetOutputData(settings, out HeightData data, PortData.fieldName,PortData.listIndex);
            if (!foundData)
                return null;
            HeightMapBranch heightBranch = settings.GetData<HeightMapBranch>();
            IBurstTexturePool texturePool = settings.GetGlobalData<IBurstTexturePool>();
            texture = texturePool.GetUnpooledTexture(PortData.fieldName, FilterMode.Point);
            NativeArray<Color32> raw = texture.GetRawData<Color32>();

            // Global min/max from HeightData
            var globalMinMax = new[] { data.minMaxValue.y, data.minMaxValue.x };

            // Determine normalization range based on mode
            NativeArray<float> normalizationRange;
            JobHandle jobHandle = data.jobHandle;
            if(LocalMode){
                    normalizationRange = new NativeArray<float>(globalMinMax, Allocator.TempJob);
                    jobHandle = GetMapBoundaries.ScheduleParallel(
                        normalizationRange, heightBranch.GetMap(),
                        data.indexData, raw.Length, texturePool.GetTextureResolution(), data.jobHandle
                    );
            }
            else{
                normalizationRange = new NativeArray<float>(new[] { data.minMaxValue.x, data.minMaxValue.y }, Allocator.TempJob);
            }

            JobHandle textureJob = MTJGeneral.ScheduleParallel(
                raw, normalizationRange, heightBranch.GetMap(),
                data.indexData, texturePool.GetTextureResolution(), jobHandle
            );
            textureJob.Complete();

            var container = new VisualElement
            {
                style = { width = Length.Percent(100), height = Length.Percent(100) }
            };

            // Dropdown for mode selection
            var modeToggle = new Toggle()
            {
                value = LocalMode // Initial state
            };
            modeToggle.AddToClassList("map-toggle");
            modeToggle.style.width = Length.Percent(100);
            modeToggle.RegisterValueChangedCallback(evt =>
            {
                LocalMode = evt.newValue;
                NodeView.ChangePreview(PortData);

                UpdateToggleState(modeToggle, evt.newValue);
            });
            UpdateToggleState(modeToggle, LocalMode);

            // Image display
            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                image = texture.ApplyTexture(),
                style = { width = Length.Percent(100), height = Length.Percent(100) }
            };

            var minmaxContainer = new VisualElement();
            minmaxContainer.AddToClassList("minMaxContainer");
            var minLabel = new Label("Min: "+normalizationRange[0].ToString("F2"));
            var maxLabel = new Label("Max: "+normalizationRange[1].ToString("F2"));
            minmaxContainer.Add(maxLabel);
            minmaxContainer.Add(minLabel);

            texture.ApplyTexture();
            normalizationRange.Dispose(textureJob);

            StackVisuals(container, image);

            // Handle point data visualization (unchanged from your original)
            var inputPointData = Node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => new { Field = f, Attribute = f.GetCustomAttribute<InputAttribute>() })
                .Where(x => x.Attribute != null && x.Field.GetCustomAttribute<DisabledAttribute>() == null && x.Field.FieldType == typeof(PointInstance))
                .Select(x => x.Field)
                .FirstOrDefault();
          
            if (inputPointData != null)
            {
                container.style.position = Position.Relative;
                if (Node.TryGetInputData<PointInstance>(settings, out var pointData, inputPointData.Name))
                {
                    VisualElement pointVisuals = previousVisualizer.CreateVisual(pointData, settings, true, graphSettings.MeshScale);
                    StackVisuals(container, pointVisuals);
                }
            }

            if(button){
                StackVisuals(container, minmaxContainer);
                StackVisualRight(container, modeToggle);
            }

            return container;
        }
        private void UpdateToggleState(Toggle toggle, bool value){
            string add = value.ToString();
            string remove = (!value).ToString();
            toggle.RemoveFromClassList(remove);
            toggle.AddToClassList(add);
            toggle.AddToClassList(value.ToString());
        }
        public override bool ValidPreview()
        {
            return Node.isValid;
        }

        
    }
}