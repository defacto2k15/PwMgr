using System.Collections.Generic;
using System.Linq;
using Assets.Roads.Pathfinding.AStar;
using Assets.Utils;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class TerrainPathfindingNode : BasePathfindingNode
    {
        private readonly MovementCostCalculator _movementCostCalculator;
        private readonly EstimatedCostCalculator _estimatedCostCalculator;
        private readonly IsGoalResolver _isGoalResolver;
        private readonly NodeChildrenFinder _childrenFinder;
        private readonly TerrainPathfindingNodeDetails _nodeDetails;

        public TerrainPathfindingNode(
            MovementCostCalculator movementCostCalculator,
            EstimatedCostCalculator estimatedCostCalculator,
            IsGoalResolver isGoalResolver,
            NodeChildrenFinder childrenFinder,
            TerrainPathfindingNodeDetails nodeDetails)
        {
            _movementCostCalculator = movementCostCalculator;
            _estimatedCostCalculator = estimatedCostCalculator;
            _isGoalResolver = isGoalResolver;
            _childrenFinder = childrenFinder;
            _nodeDetails = nodeDetails;
        }

        public override void SetMovementCost(IRoadSearchNode parent)
        {
            MovementCost = parent.MovementCost + _movementCostCalculator.CostOf((TerrainPathfindingNode) parent, this);
        }

        public override void SetEstimatedCost(IRoadSearchNode goal)
        {
            EstimatedCost = _estimatedCostCalculator.CostOf(this);
        }

        public override IEnumerable<IRoadSearchNode> Children => _childrenFinder.FindFor(this).Cast<IRoadSearchNode>();

        public override bool IsGoal(IRoadSearchNode goal)
        {
            return _isGoalResolver.IsGoal(this);
        }

        public IntVector2 Position => _nodeDetails.Position;

        public float Height => _nodeDetails.Height;
    }
}