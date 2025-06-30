using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class CoordinateDataHolder : ProcessableData
    {
        public MeshSettings meshSettings{get; private set;}
        public TerrainConfiguration terrainConfiguration{get; private set;}
        public NativeArray<CoordinateData> points{get; private set;}
        public Vector2 MinMaxHeight{get; private set;}
        public JobHandle finalJob{get; private set;}

        private ReturnablePack returnablePack;
        public void ReuseData(ReturnableManager returnableManager, WorldFinalData worldFinalData, JobHandle combinedJob, MeshSettings meshSettings, TerrainConfiguration terrainConfiguration){
            this.meshSettings = meshSettings;
            this.terrainConfiguration = terrainConfiguration;
            this.MinMaxHeight = worldFinalData.MinMaxHeight;

            returnablePack = GenericPoolLight<ReturnablePack>.Get().Reuse();
            points = returnableManager.GetData<CoordinateData>(returnablePack, worldFinalData.FinalPositions.Length);
            finalJob = CoordinateDataJob.ScheduleParallel(worldFinalData.FinalPositions,
                points, terrainConfiguration.Position, tr(terrainConfiguration.ID), meshSettings.Resolution, combinedJob);
        }

        public CoordinateDataHolder(){}
        public CoordinateDataHolder(NativeArray<CoordinateData> data, MeshSettings meshSettings, TerrainConfiguration terrainConfiguration)
        {
            this.meshSettings = meshSettings;
            this.terrainConfiguration = terrainConfiguration;

            returnablePack = GenericPoolLight<ReturnablePack>.Get().Reuse();
            points = data;
            finalJob = default;
        }

        protected override void OnFinalisedProcessors()
        {
            returnablePack.Release();
            GenericPoolLight.Release(this);
        }

        private int3 tr(Vector3Int id){
            return new int3(id.x, id.y, id.z);
        }

        public CoordinateData GetCoordinateDataAtGrid(Vector2 position, bool interpolated)
        {
            if(interpolated)
                return CoordinateDataResultInterpolated(points, meshSettings.Resolution, meshSettings.MeshScale, position, terrainConfiguration.Position);
            else
                return CoordinateDataResult(points, meshSettings.Resolution, meshSettings.MeshScale, position, terrainConfiguration.Position);
        }

        private CoordinateData CoordinateDataResult(NativeArray<CoordinateData> points, int resolution, float correctMeshScale, Vector2 simplePos, Vector3 position){
            Vector2 leftCorner = new Vector2(position.x, position.z)-new Vector2(correctMeshScale,correctMeshScale)/2f;
            Vector2 flatUV = (simplePos-leftCorner) / correctMeshScale;
            Vector2Int index = Vector2Int.RoundToInt(flatUV * resolution);
            return SampleData(points, resolution, index);
        }
        
        private CoordinateData CoordinateDataResultInterpolated(NativeArray<CoordinateData> points, int resolution, float correctMeshScale, Vector2 simplePos, Vector3 position){
            Vector2 leftCorner = new Vector2(position.x, position.z)-new Vector2(correctMeshScale,correctMeshScale)/2f;
            Vector2 flatUV = (simplePos-leftCorner)*resolution / correctMeshScale;
            
            Vector2Int indexA = Vector2Int.FloorToInt(flatUV);
            Vector2Int indexB = indexA+new Vector2Int(0, 1);
            Vector2Int indexC = indexA+new Vector2Int(1, 1);
            Vector2Int indexD = indexA+new Vector2Int(1, 0);

            Vector2 t = flatUV-indexA;

            var dataA = SampleData(points, resolution, indexA);
            var dataB = SampleData(points, resolution, indexB);
            var dataC = SampleData(points, resolution, indexC);
            var dataD = SampleData(points, resolution, indexD);

            CoordinateData XBottom = CoordinateData.Lerp(dataA, dataD, t.x);
            CoordinateData XTop = CoordinateData.Lerp(dataB, dataC, t.x);

            return CoordinateData.Lerp(XBottom, XTop, t.y);
        }

        private CoordinateData SampleData(NativeArray<CoordinateData> points, int resolution, Vector2Int index){
            index.x = Mathf.Clamp(index.x, 0, resolution);
            index.y = Mathf.Clamp(index.y, 0, resolution);

            if(index.x + index.y * (resolution + 1) < points.Length)
                return points[index.x + index.y * (resolution + 1)];
            else 
                return CoordinateData.Default;
        }
    }
}