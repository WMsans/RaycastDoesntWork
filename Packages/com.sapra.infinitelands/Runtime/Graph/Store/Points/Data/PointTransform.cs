using Unity.Mathematics;

namespace sapra.InfiniteLands{
    public struct PointTransform{
        public float3 Position;
        public float YRotation;
        public float Scale;

        public PointTransform UpdatePosition(float3 position){
            return new PointTransform(){
                Position = position,
                YRotation = YRotation,
                Scale = Scale
            };
        }

        public PointTransform UpdateRotation(float value){
            return new PointTransform(){
                Position = Position,
                YRotation = value,
                Scale = Scale
            };
        }

        public PointTransform UpdateScale(float scale){
            return new PointTransform(){
                Position = Position,
                YRotation = YRotation,
                Scale = scale
            };
        }
    }
}