using System;
using Assets.Utils;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class PathfindingNodesGenerator
    {
        private MovementCostCalculator _movementCostCalculator;
        private EstimatedCostCalculator _estimatedCostCalculator;
        private IsGoalResolver _isGoalResolver;
        private NodeChildrenFinder _childrenFinder;
        private TerrainSamplingSource _samplingSource;
        private Action<TerrainPathfindingNodeDetails, TerrainPathfindingNode> _debugCreationAction;

        public PathfindingNodesGenerator(
            MovementCostCalculator movementCostCalculator,
            EstimatedCostCalculator estimatedCostCalculator,
            IsGoalResolver isGoalResolver,
            NodeChildrenFinder childrenFinder,
            TerrainSamplingSource samplingSource,
            Action<TerrainPathfindingNodeDetails, TerrainPathfindingNode> debugCreationAction = null
        )
        {
            _movementCostCalculator = movementCostCalculator;
            _estimatedCostCalculator = estimatedCostCalculator;
            _isGoalResolver = isGoalResolver;
            _childrenFinder = childrenFinder;
            _samplingSource = samplingSource;
            _debugCreationAction = debugCreationAction;
        }

        public TerrainPathfindingNode Generate(IntVector2 position, TerrainPathfindingNode parent)
        {
            TerrainPathfindingNodeDetails nodeDetails =
                new TerrainPathfindingNodeDetails()
                {
                    Height = _samplingSource.SamplePosition(position),
                    Position = position,
                };

            _debugCreationAction?.Invoke(nodeDetails, parent);

            return new TerrainPathfindingNode(
                _movementCostCalculator,
                _estimatedCostCalculator,
                _isGoalResolver,
                _childrenFinder,
                nodeDetails);
        }
    }
}