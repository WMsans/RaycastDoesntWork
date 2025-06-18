using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    public class EditorGenerator 
    {
        HeightOutputNode output;
        IGraph graph;

        public EditorGenerator(IGraph generator){
            output = generator.GetOutputNode();
            graph = generator;
        }
        public void GenerateEditorVisuals(GraphSettings graphSettings, IBurstTexturePool pool, IEnumerable<OutputPreview> outputPreviews)
        {
            var store = new StringObjectStore<object>();
            var returnableArrays = new ReturnableManager(store);
            var indexManager = new HeightMapManager(graph);
            var gridManager = new GridManager();
            var pointManager = new PointManager(store);
            var completitionToken = new CompletitionToken();
            completitionToken.Complete();

            store.AddData(returnableArrays);
            store.AddData(indexManager);
            store.AddData(pool);
            store.AddData(gridManager);
            store.AddData(pointManager);
            store.AddData(completitionToken);

            TerrainConfiguration terrain = new TerrainConfiguration
            {
                Position = new Vector3(graphSettings.WorldOffset.x, 0, graphSettings.WorldOffset.y)
            };

            MeshSettings meshSettings = new MeshSettings
            {
                Resolution = graphSettings.Resolution,
                MeshScale = graphSettings.MeshScale,
                Seed = graphSettings.Seed,
            };
            var activeNodes = outputPreviews.Select(a => a.Node);
            if (activeNodes.Any())
            {
                //graph.ValidationCheck();
                foreach (var preview in outputPreviews)
                {
                    TreeData nodeSettings = TreeData.NewTree(store, meshSettings, terrain, output, new InfiniteLandsNode[] { preview.Node });
                    //nodeSettings.Complete();
                    nodeSettings.ProcessTree();
                    preview.NodeView.GeneratePreview(nodeSettings.GetTrunk(), graphSettings);
                    nodeSettings.CloseTree();
                }
            }

            store.Dispose(default);
        }
    }
}