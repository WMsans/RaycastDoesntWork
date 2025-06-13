using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    public readonly struct LandmarkResult{
        public readonly GameObject prefab;
        public readonly string PrefabName;
        public readonly List<PointTransform> newPoints;
        public readonly bool AlignWithTerrain;
        public LandmarkResult(GameObject prefab, string _prefabName, List<PointTransform> newPoints, bool alignWithTerrain){
            this.prefab = prefab;
            this.newPoints = newPoints;
            this.AlignWithTerrain = alignWithTerrain;
            this.PrefabName = _prefabName;
        }
    }
    public class LandmarkData : ProcessableData{
        public List<LandmarkResult> LandmarksGenerated = new();
        public void ReuseData(TreeData treeData, WorldGenerator worldGenerator){
            var nodes = worldGenerator.nodes;
            LandmarksGenerated.Clear();
            var settings = treeData.GetTrunk();
            foreach(var node in nodes){
                if(!node.isValid) continue;
                if(node is ILoadPoints pointLoader){
                    var points = pointLoader.GetOutput(settings);
                    var prefab = pointLoader.GetPrefab();
                    var name = pointLoader.GetPrefabName();
                    if (points != null)
                    {
                        points.GetNewPointsInMesh(settings, out var newPoints);
                        if (newPoints != null)
                        {
                            LandmarkResult result = new LandmarkResult(prefab, name, new List<PointTransform>(newPoints), pointLoader.alignWithTerrain);
                            LandmarksGenerated.Add(result);
                        }
                    }
                }
            }            
        }

        protected override void OnFinalisedProcessors()
        {
            GenericPoolLight.Release(this);
        }
    }
}