using System;

using UnityEngine;

namespace sapra.InfiniteLands
{
    [Serializable]
    public struct MeshSettings
    {
        public enum MeshType
        {
            Normal,
            Decimated
        };

        public enum GenerationMode
        {
            RelativeToWorld,
            RelativeToTerrain
        };

        public int Seed;
        [Range(1, 255)] public int Resolution;
        public int MaxIndex => Resolution-1;
        public bool CustomSplatMapResolution;
        [SerializeField][ShowIf(nameof(CustomSplatMapResolution))] private int _textureResolution;
        public int TextureResolution => CustomSplatMapResolution ? _textureResolution : Resolution;
        public bool SeparatedBranch => CustomSplatMapResolution && TextureResolution != Resolution;
        [Min(100)] public float MeshScale;
        public MeshType meshType;
        public GenerationMode generationMode;
        [ShowIf(nameof(isDecimated))][Min(1)] public int CoreGridSpacing;
        public int coreGridSpacing => Mathf.CeilToInt(Resolution/(float)Mathf.CeilToInt(Resolution/(float)CoreGridSpacing));
        [ShowIf(nameof(isDecimated))][Range(0,1)]public float NormalReduceThreshold;
        public float ratio => Resolution/MeshScale;
        public static MeshSettings Default => new MeshSettings
        {
            Seed = 0,
            Resolution = 255,
            _textureResolution = 255,
            CustomSplatMapResolution = false,
            MeshScale = 1000,
            CoreGridSpacing = 6,
            NormalReduceThreshold = .5f,
            meshType = MeshType.Normal,
            generationMode = GenerationMode.RelativeToTerrain,
        };
        private bool isDecimated => meshType == MeshType.Decimated;
        public MeshSettings ModifyResolution(int resolution){
            float initialRatio = ratio;
            this.Resolution = resolution;
            this.MeshScale = resolution/initialRatio;  
            return this;
        }
        
        public bool SoftEqual(MeshSettings meshSettings){
            return Resolution == meshSettings.Resolution && MeshScale == meshSettings.MeshScale;
        }
        
    }
}