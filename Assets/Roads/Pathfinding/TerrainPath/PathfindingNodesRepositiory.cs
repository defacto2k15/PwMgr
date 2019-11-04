using System.Collections.Generic;
using Assets.Utils;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class PathfindingNodesRepositiory
    {
        private readonly Dictionary<IntVector2, TerrainPathfindingNode> _nodesDict =
            new Dictionary<IntVector2, TerrainPathfindingNode>();

        public PathfindingNodesRepositiory(IntVector2 subFieldSize)
        {
        }

        public TerrainPathfindingNode Retrive(IntVector2 gratePosition)
        {
            if (!_nodesDict.ContainsKey(gratePosition))
            {
                return null;
            }
            return _nodesDict[gratePosition];
        }

        public void Add(TerrainPathfindingNode node)
        {
            IntVector2 gratePosition = node.Position;
            Preconditions.Assert(!_nodesDict.ContainsKey(gratePosition),
                $"In position {gratePosition} there arleady is node");
            _nodesDict[gratePosition] = node;
        }

        public void Clear()
        {
            _nodesDict.Clear();
        }
    }
}