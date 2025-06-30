using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class LandmarkPlacer : ChunkProcessor<ChunkData>
    {
        private Transform LandmarkParent;
        private Dictionary<string, LandmarkManager> PrefabManagers = new();
        private Vector2 previousOffset;
        private FloatingOrigin floatingOrigin;

        private List<LandmarkResult> ToCreate = new();
        public int ConsecutiveSpawns = 10;
        protected override void DisableProcessor()
        {
            foreach(var keyValye in PrefabManagers){
                if(keyValye.Value != null)
                    keyValye.Value.Disable();
            }
            
            if(LandmarkParent != null){
                AdaptiveDestroy(LandmarkParent.gameObject);
                LandmarkParent = null;
            }
            PrefabManagers.Clear();
        }

        protected override void InitializeProcessor()
        {
            LandmarkParent = RuntimeTools.FindOrCreateObject("Landmarks", transform).transform;

            floatingOrigin = GetComponent<FloatingOrigin>();
            previousOffset = infiniteLands.localGridOffset;
            ToCreate.Clear();
        }

        public override void OnGraphUpdated()
        {
            Disable();
            Initialize(infiniteLands);
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            if(infiniteLands.localGridOffset != previousOffset){
                Disable();
                Initialize(infiniteLands);
            }

            var landmarks = chunk.GetData<LandmarkData>();
            ToCreate.AddRange(landmarks.LandmarksGenerated);
        }

        public LandmarkManager GetPrefabParent(GameObject prefab, string prefabName, bool AlignWithTerrain){
            if(!PrefabManagers.TryGetValue(prefabName, out var prefabManager)){
                var parent = transform.Find(prefabName);
                if(!parent){
                    var existTransf = RuntimeTools.CreateObjectAndRecord(prefabName);
                    existTransf.transform.SetParent(LandmarkParent);
                    parent = existTransf.transform;
                }
                prefabManager = new LandmarkManager(prefab, parent, AlignWithTerrain, floatingOrigin != null);
                PrefabManagers.Add(prefabName, prefabManager);
            }
            return prefabManager;
        }
        
        protected override void OnProcessRemoved(ChunkData chunk)
        {
        }
        public override void Update()
        {
            int currentCount = 0;
            for (int t = ToCreate.Count - 1; t >= 0; t--)
            {
                var land = ToCreate[t];
                var pnts = land.newPoints;
                var prefab = land.prefab;
                if (prefab == null || pnts == null) continue;

                var finalPoints = land.newPoints;
                LandmarkManager prefabManager = GetPrefabParent(prefab, land.PrefabName, land.AlignWithTerrain);
                for (int i = finalPoints.Count - 1; i >= 0; i--)
                {
                    PointTransform point = finalPoints[i];
                    prefabManager.CreateObject(point, infiniteLands.localToWorldMatrix);
                    finalPoints.RemoveAt(i);
                    currentCount++;
                    if (currentCount >= ConsecutiveSpawns)
                        return;
                }
                ToCreate.RemoveAt(t);
            }
        }
    }
}
