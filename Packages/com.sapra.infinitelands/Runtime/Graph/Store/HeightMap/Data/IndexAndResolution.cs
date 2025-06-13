
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct IndexAndResolution{
        public int Resolution;
        public int Length;

        public int IndexOffset;

        public int Index;
        private int FullLength;

        public void LogIt()
        {
            Debug.Log(Resolution);
            Debug.Log(Length);
            Debug.Log(IndexOffset);
            Debug.Log(FullLength);
            Debug.Log(Index);
        }
        public IndexAndResolution(int index, int resolution)
        {
            Index = index;
            Resolution = resolution;
            Length = MapTools.LengthFromResolution(Resolution);
            IndexOffset = -1;
            FullLength = -1;
        }
        public IndexAndResolution(int index) : this(index, -1){}

        public static IndexAndResolution OffsetResolution(IndexAndResolution og, int targetResolution){
            var newData = new IndexAndResolution(og.Index, targetResolution);
            newData.SetIndexOffset(og.FullLength);
            return newData;
        }
        public static IndexAndResolution CopyAndOffset(IndexAndResolution og, int indexOffset, int modifyResolution = 0){
            var newData = new IndexAndResolution(og.Index+indexOffset, MapTools.IncreaseResolution(og.Resolution,modifyResolution));
            newData.SetIndexOffset(og.FullLength);
            return newData;
        }

        public IndexAndResolution SetIndexOffset(int finalMapLength){
            FullLength = finalMapLength;
            IndexOffset = Index*finalMapLength;
            return this;
        }
        
        public bool UpdateLength(int resolution){
            if(resolution > Resolution){
                Resolution = resolution;
                Length = MapTools.LengthFromResolution(Resolution);
                return true;
            }
            return false;
        }
    }
}