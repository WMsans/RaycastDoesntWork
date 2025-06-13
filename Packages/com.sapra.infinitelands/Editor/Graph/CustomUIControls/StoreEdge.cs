using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
    public class StoreEdge : IRenderSerializableGraph
    {
        public EdgeConnection connection;
        public StoreEdge(EdgeConnection edge)
        {
            connection = edge;
        }
        public object GetDataToSerialize() => connection;
        public string GetGUID()=>connection.GetHashCode().ToString();
        public Vector2 GetPosition() => Vector2.zero;
    }
}