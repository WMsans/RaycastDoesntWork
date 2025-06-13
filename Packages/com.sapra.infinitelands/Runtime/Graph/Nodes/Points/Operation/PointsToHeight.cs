using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [CustomNode("Points to Height", docs = "https://ensapra.com/packages/infinite_lands/nodes/points/operations/pointstoheight", synonims = new string[]{"Stamp"})]
    public class PointsToHeight : InfiniteLandsNode
    {
        [Input] public PointInstance Points;
        [Input, Disabled] public HeightData Texture;

        [Output] public HeightData Output;
        [Min(0.1f)] public float Size = 200;
        public bool IfMaskAssigned => !IsAssigned(nameof(Texture));
        public bool AffectedByScale = false;
        public bool AffectedByRotation = false;
        [ShowIf(nameof(ifAssigned))] public bool TextureHeightAffectedByScale;
        private bool ifAssigned => IsAssigned(nameof(Texture));

        [ShowIf(nameof(IfMaskAssigned))][Min(0.1f)] public float FallofDistance = 1;

        protected override bool ProcessNode(BranchData branch)
        {
            var reuser = new GenericNodeReuser<PointsToHeight, PointsToHeightData>(branch, this);
            return AwaitableTools.WaitNode<PointsToHeightData, HeightData, GenericNodeReuser<PointsToHeight, PointsToHeightData>>(branch, ref reuser, out Output, this,nameof(Output));
        }

        private class PointsToHeightData : AwaitableData<HeightData>, INodeReusable<PointsToHeight>
        {
            public HeightData Result{get; private set;}
            BranchData branch;
            int SubState;
            PointsToHeight node;

            private bool isTextureAssigned;
            private BranchData textureSettings;
            private GridBranch gridBranch;
            private PointInstance pointInstance;

            public void Reuse(PointsToHeight node, BranchData branch){
                this.branch = branch;
                this.node = node;
                SubState = -1;
                gridBranch = branch.GetData<GridBranch>();
                isTextureAssigned = node.IsAssigned(nameof(Texture));
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
                    if(!node.ProcessDependency(branch, nameof(Points))) return false;
                    if(!gridBranch.ProcessGrid(out _)) return false;

                    node.TryGetInputData(branch, out pointInstance, nameof(Points));
                    SubState++;
                }

                if(SubState == 0){
                    if(isTextureAssigned){
                        if(!node.ProcessDependency(textureSettings, nameof(Texture))) return false;
                    }

                    if(!pointInstance.GetAllPoints(branch.meshSettings.MeshScale+node.Size, branch.terrain.Position, out var foundPoints)) return false;

                    GridData gridData = gridBranch.GetGridData();
                    HeightMapBranch heightBranch = branch.GetData<HeightMapBranch>();
                    var targetSpace = heightBranch.GetAllocationSpace(node, nameof(Output), out var map);
                    JobHandle job;

                    Vector2 minMaxHeight = new Vector2(0,1);
                    NativeArray<PointTransform> flattenedPoints = branch.GetData<ReturnableBranch>().GetData(foundPoints); 
                    if (isTextureAssigned)
                    {
                        node.TryGetInputData(textureSettings, out HeightData resultsHeight, nameof(node.Texture));
                        HeightMapBranch textureBranch = textureSettings.GetData<HeightMapBranch>();
                        var textureMap = textureBranch.GetMap();
                        JobHandle combined = JobHandle.CombineDependencies(resultsHeight.jobHandle, gridData.jobHandle);
                        minMaxHeight = resultsHeight.minMaxValue;
                        job = PointsToHeightTextureJob.ScheduleParallel(map, textureMap, gridData.grid, flattenedPoints,
                            targetSpace, resultsHeight.indexData,
                            foundPoints.Count, branch.meshSettings.Resolution, textureSettings.meshSettings.MeshScale,
                            node.AffectedByScale, node.AffectedByRotation, node.TextureHeightAffectedByScale, resultsHeight.minMaxValue,
                            branch.terrain.Position, combined);
                    }
                    else
                    {
                        job = PointsToHeightJob.ScheduleParallel(map, gridData.grid, flattenedPoints, targetSpace,
                            flattenedPoints.Length, branch.meshSettings.Resolution, node.FallofDistance, node.Size,
                            node.AffectedByScale, branch.terrain.Position, gridData.jobHandle);
                    }
                    

                    Result = new HeightData(job, targetSpace, minMaxHeight);
                    SubState++;
                }

                return SubState == 1;
            }
        }
    }
}