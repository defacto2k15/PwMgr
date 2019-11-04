using System.Collections.Generic;
using Assets.Ring2;
using Assets.Utils;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class NodeChildrenFinder
    {
        private readonly PathfindingNodesRepositiory _nodesRepositiory;
        private readonly List<IntVector2> _neighbourDeltas;
        private readonly IntRectangle _boundaries;
        private PathfindingNodesGenerator _nodesGenerator;

        public NodeChildrenFinder(PathfindingNodesRepositiory nodesRepositiory,
            List<IntVector2> neighbourDeltas,
            IntRectangle boundaries)
        {
            _nodesRepositiory = nodesRepositiory;
            _neighbourDeltas = neighbourDeltas;
            _boundaries = boundaries;
        }

        public void SetNodesGenerator(PathfindingNodesGenerator nodesGenerator)
        {
            _nodesGenerator = nodesGenerator;
        }

        public List<TerrainPathfindingNode> FindFor(TerrainPathfindingNode node)
        {
            List<TerrainPathfindingNode> neighbours = new List<TerrainPathfindingNode>();
            foreach (var delta in _neighbourDeltas)
            {
                var neighbourPosition = node.Position + delta;
                if (_boundaries.Contains(neighbourPosition))
                {
                    var neighbour = _nodesRepositiory.Retrive(neighbourPosition);
                    if (neighbour == null)
                    {
                        neighbour = _nodesGenerator.Generate(neighbourPosition, node);
                        _nodesRepositiory.Add(neighbour);
                    }
                    neighbours.Add(neighbour);
                }
            }
            return neighbours;
        }
    }
}