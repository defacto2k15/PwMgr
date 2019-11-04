using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class IsGoalResolver
    {
        private IsGoalResoverConfiguration _configuration;
        private Vector2 _lastPointPosition;
        private MyLine _finalLine;
        private GratePositionCalculator _positionCalculator;

        public IsGoalResolver(IsGoalResoverConfiguration configuration, PathfindingSegment pathfindingSegment,
            GratePositionCalculator positionCalculator)
        {
            _configuration = configuration;
            _positionCalculator = positionCalculator;

            var last = pathfindingSegment.TargetNode.Position.ToFloatVec();
            var preLast = pathfindingSegment.StartNode.Position.ToFloatVec();
            var vec = last - preLast;
            var perpendicular = new Vector2(-vec.y, vec.x);
            _finalLine = MyLine.ComputeFrom(perpendicular, last);
            _lastPointPosition = last;
        }

        public bool IsGoal(TerrainPathfindingNode node)
        {
            var nodePosition = _positionCalculator.ToGlobalPosition(node.Position);
            var goalPoints = _finalLine.DistanceToPoint(nodePosition) * _configuration.FinalLineDistanceFactor +
                             Vector2.Distance(nodePosition, _lastPointPosition) *
                             _configuration.FinalPointDistanceFactor;
            if (goalPoints <= _configuration.MinimalGoalValue)
            {
                _finalLine.DistanceToPoint(nodePosition);
            }
            return goalPoints <= _configuration.MinimalGoalValue;
        }

        public class IsGoalResoverConfiguration
        {
            public float FinalLineDistanceFactor;
            public float FinalPointDistanceFactor;
            public float MinimalGoalValue;
        }
    }
}