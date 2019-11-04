using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class EstimatedCostCalculator
    {
        private PathfindingSegment _pathfindingSegment;
        private GratePositionCalculator _positionCalculator;
        private readonly float _distanceToWayFactor;
        private float _heightRateFactor;

        public EstimatedCostCalculator(PathfindingSegment pathfindingSegment,
            GratePositionCalculator positionCalculator, float distanceToWayFactor, float heightRateFactor)
        {
            _pathfindingSegment = pathfindingSegment;
            _distanceToWayFactor = distanceToWayFactor;
            _heightRateFactor = heightRateFactor;
            _positionCalculator = positionCalculator;
        }

        public float CostOf(TerrainPathfindingNode node)
        {
            var thisToTargetCost = CostOfMovement(new NodeWithHeight()
            {
                Height = node.Height,
                Position = node.Position
            }, _pathfindingSegment.TargetNode);

            return thisToTargetCost;
        }

        private float CostOfMovement(NodeWithHeight start, NodeWithHeight finish)
        {
            var distanceToTarget = _positionCalculator.CalculateManhattanDistance(start.Position, finish.Position);
            var heightDifference = Mathf.Abs(start.Height - finish.Height);
            return distanceToTarget * _distanceToWayFactor +
                   (heightDifference * _heightRateFactor) /*/distanceToTarget*/;
        }
    }
}