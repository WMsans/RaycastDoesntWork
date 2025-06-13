using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CreateAssetMenu(fileName = "World", menuName = "Infinite Lands/Graph/World")]
    public class WorldTree : TerrainGenerator
    {
        public List<BiomeTree> PreviouslyConnected = new List<BiomeTree>();
        public override IEnumerable<InfiniteLandsNode> GetAllNodes()
        {
            return base.GetAllNodes().Concat(nodes.OfType<BiomeNode>().SelectMany(a => a.GetNodeCopies()));            
        }
        public override IEnumerable<EdgeConnection> GetAllEdges()
        {
            return base.GetAllEdges().Concat(nodes.OfType<BiomeNode>().SelectMany(a => a.GetEdgeCopies())).Distinct();
        }
    }
}