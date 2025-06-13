using UnityEngine;

namespace sapra.InfiniteLands{
    public interface ILoadPoints : IOutput
    {
        public PointInstance GetOutput(BranchData settings);
        public GameObject GetPrefab();
        public string GetPrefabName();
        public bool alignWithTerrain{get;}
    }
}