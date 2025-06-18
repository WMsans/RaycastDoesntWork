using System;

using UnityEngine;

namespace sapra.InfiniteLands
{
    [Serializable]
    public struct MeshSettings
    {
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
        public GenerationMode generationMode;

        public float ratio => Resolution/MeshScale;
        public static MeshSettings Default => new MeshSettings
        {
            Seed = 0,
            Resolution = 255,
            _textureResolution = 255,
            CustomSplatMapResolution = false,
            MeshScale = 1000,
            generationMode = GenerationMode.RelativeToTerrain,
        };
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