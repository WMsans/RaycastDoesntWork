using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

namespace sapra.InfiniteLands
{
    [CustomNode("Flatten Around", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/operations/flattenaround")]
    public class FlattenAroundNode : InfiniteLandsNode
    {
        [Input] public PointInstance Points;
        [Input] public HeightData HeightMap;
        [Input, Disabled] public HeightData Texture;
        [Output] public HeightData Output;

        public bool IfMaskAssigned => IsAssigned(nameof(Texture));
        public bool IfMaskNotAssigned => !IsAssigned(nameof(Texture));

        [Min(1)] public float Size = 250;
        public bool AppliesTerrainHeight = true;
        public bool AffectedByScale = false;
        
        [ShowIf(nameof(IfMaskAssigned))]public bool AffectedByRotation = false;
        [ShowIf(nameof(IfMaskNotAssigned))][Min(0.1f)] public float FallofDistance = 1;

        protected override bool ProcessNode(BranchData branch)
        {
            var reuser = new FlattenerReuser(this, branch);
            return AwaitableTools.WaitNode<FlattenAroundData, HeightData, FlattenerReuser>(branch, ref reuser, out Output, this, nameof(Output));
        }

        private struct FlattenerReuser : IReuseObject<FlattenAroundData>
        {
            private FlattenAroundNode node;
            private BranchData branch;
            public FlattenerReuser(FlattenAroundNode node, BranchData branch){
                this.node = node;
                this.branch = branch;
            }
            public void Reuse(FlattenAroundData instance)
            {
                instance.Reuse(node, branch);
            }
        }
        private class FlattenAroundData : AwaitableData<HeightData>
        {
            public HeightData Result{get; private set;}
            FlattenAroundNode node;
            BranchData branch;

            private PointManager pointManager;
            private HeightData HeightMap;
            private PointInstance previousPoints;

            List<PointTransform> ValidPoints = new();
            List<AwaitingHeight> GeneratedHeights = new();

            private int SubState;
            private ICreateGrid createGrid;
            private GridBranch gridBranch;
            private GridData gridData;

            private bool isTextureAssigned;
            BranchData textureSettings; 

            private struct AwaitingHeight{
                public HeightAtPoint awaitingHeight;
                public PointTransform point;
            }
            public void Reuse(FlattenAroundNode node, BranchData branch){
                gridBranch = branch.GetData<GridBranch>();
                createGrid = gridBranch.GridCreator;

                this.SubState = -1;
                this.ValidPoints.Clear();
                GeneratedHeights.Clear();
                this.branch = branch;
                this.node = node;
                pointManager = branch.GetGlobalData<PointManager>();

                isTextureAssigned =  node.IsAssigned(nameof(Texture));

                if(isTextureAssigned){
                    int resolution = Mathf.CeilToInt(branch.meshSettings.ratio*node.Size);
                    float FinalDistance = resolution/branch.meshSettings.ratio;
                    MeshSettings meshSettings = new MeshSettings(){
                        Resolution = resolution,
                        MeshScale = FinalDistance,
                        Seed = branch.meshSettings.Seed,
                    };
                    
                    TerrainConfiguration terrain = new TerrainConfiguration(default, branch.terrain.TerrainNormal, Vector3.zero);

                    textureSettings = BranchData.NewChildBranch(meshSettings, terrain, branch, node.GetNodesInInput(nameof(Texture)));
                }
            }

            public bool ProcessData()
            {
                if(SubState == -1){

                    if(!node.ProcessDependency(branch, nameof(node.Points))) return false;
                    if(!node.ProcessDependency(branch, nameof(node.HeightMap))) return false;
                    if(!gridBranch.ProcessGrid(out gridData)) return false;

                    node.TryGetInputData(branch, out previousPoints,  nameof(node.Points));
                    node.TryGetInputData(branch, out HeightMap,  nameof(node.HeightMap));

                    SubState++;
                }

                if(SubState == 0){
                    if(isTextureAssigned){
                        if(!node.ProcessDependency(textureSettings, nameof(Texture))) return false;
                    }
                    if(!previousPoints.GetAllPoints(branch.meshSettings.MeshScale, branch.terrain.Position, out var foundPoints)) return false;

                    if(node.AppliesTerrainHeight){
                        foreach(var point in foundPoints){
                            MeshSettings meshSettings = new MeshSettings(){
                                Resolution = 3,
                                MeshScale = 50,
                                Seed = branch.meshSettings.Seed,
                            };
                            var height = pointManager.GetDataAtPoint(node, nameof(HeightMap),createGrid, point.Position, meshSettings);
                            GeneratedHeights.Add(new AwaitingHeight(){
                                awaitingHeight = height,
                                point = point,
                            });
                        }
                        SubState++;
                    }
                    else{
                        ValidPoints.AddRange(foundPoints);
                        SubState = 2;
                    }
                }

                if(SubState == 1){
                    var updater = new UpdatePointPosition(ValidPoints);
                    if(AwaitableTools.IterateOverItems(GeneratedHeights, ref updater))
                        SubState++;
                }

                if(SubState == 2){
                    HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
                    var targetSpace = heightBranch.GetAllocationSpace(node, nameof(Output), out var map);
                    NativeArray<PointTransform> flattenedPoints = branch.GetData<ReturnableBranch>().GetData(ValidPoints); 
                    JobHandle combinedJob = JobHandle.CombineDependencies(HeightMap.jobHandle, gridData.jobHandle);

                    JobHandle job;
                    if(isTextureAssigned){    
                        HeightMapBranch textureBranch = textureSettings.GetData<HeightMapBranch>();
                        node.TryGetInputData(textureSettings, out HeightData resultsHeight, nameof(node.Texture));
                        var textureMap = textureBranch.GetMap();
                        JobHandle combined = JobHandle.CombineDependencies(resultsHeight.jobHandle, combinedJob);

                        job = FlattenAtPointsTextureJob.ScheduleParallel(map, textureMap, gridData.grid,flattenedPoints, 
                            targetSpace,HeightMap.indexData, resultsHeight.indexData,
                            ValidPoints.Count, branch.meshSettings.Resolution, textureSettings.meshSettings.MeshScale, 
                            node.AffectedByScale, node.AffectedByRotation,
                            branch.terrain.Position, combined);
                    }
                    else{
                        job = FlattenAtPointsJob.ScheduleParallel(map, gridData.grid,flattenedPoints, HeightMap.indexData, targetSpace,
                            ValidPoints.Count, branch.meshSettings.Resolution, node.FallofDistance, node.Size, node.AffectedByScale, 
                            branch.terrain.Position, combinedJob);
                    }

                    Result = new HeightData(job, targetSpace, HeightMap.minMaxValue);
                    SubState++;
                }

                if(SubState == 3)
                    return true;
                else 
                    return false;
            }

            private struct UpdatePointPosition : ICallMethod<AwaitingHeight>
            {
                List<PointTransform> ValidPoints;
                public UpdatePointPosition(List<PointTransform> pointTransforms){
                    this.ValidPoints = pointTransforms;
                }
                public bool Callback(AwaitingHeight value)
                {
                    if(!value.awaitingHeight.ProcessData()) return false;

                    var result = value.awaitingHeight.Result;
                    var point = value.point;

                    ValidPoints.Add(point.UpdatePosition(new Vector3(point.Position.x, result, point.Position.z)));
                    return true;
                }
            }
        }
    }
}