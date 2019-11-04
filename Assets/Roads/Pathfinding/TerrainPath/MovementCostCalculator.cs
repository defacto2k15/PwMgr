using System;
using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class MovementCostCalculator
    {
        private MovementCostCalculatorConfiguration _configuration;
        private GratePositionCalculator _gratePositionCalculator;
        private PathfindingSegment _pathfindingSegment;

        public MovementCostCalculator(
            MovementCostCalculatorConfiguration configuration,
            GratePositionCalculator gratePositionCalculator, PathfindingSegment pathfindingSegment)
        {
            _configuration = configuration;
            _gratePositionCalculator = gratePositionCalculator;
            _pathfindingSegment = pathfindingSegment;
        }

        public float CostOf(TerrainPathfindingNode source, TerrainPathfindingNode destination)
        {
            var distance = _gratePositionCalculator.CalculateManhattanDistance(source.Position, destination.Position);
            var heightDelta = Math.Abs(source.Height - destination.Height);
            var heightRate = heightDelta /* / distance*/;

            float azimuthDifference = 0f;
            float waySeparationDifference = 0f;
            if (source.Parent != null)
            {
                azimuthDifference = CalculateAzimuthDifference(source, destination);
                waySeparationDifference = CalculateCanonicLineSeparationDifference(source, destination);
            }

            var toReturn = heightRate * _configuration.HeightRateFactor +
                           azimuthDifference * _configuration.AzimuthFactor +
                           distance * _configuration.StepDistanceFactor +
                           waySeparationDifference * _configuration.WaySeparationDifferenceFactor;
            //Debug.Log($"T1P:{destination.Position}, cost {toReturn}");
            //Debug.Log($"T3P:{heightRate}, mult {heightRate*_configuration.HeightRateFactor}");
            //Debug.Log($"T2P: {destination.Position}, h:{distanceToWay}");
            return toReturn;
        }

        private static float CalculateAzimuthDifference(TerrainPathfindingNode source,
            TerrainPathfindingNode destination)
        {
            var sourceToDestinationVector = destination.Position - source.Position;

            var aParent = source.Parent as TerrainPathfindingNode;
            var parentToSourceVector = source.Position - aParent.Position;

            var angleDifference = Mathf.Abs(Mathf.Acos(
                Vector2.Dot(sourceToDestinationVector.ToFloatVec().normalized,
                    parentToSourceVector.ToFloatVec().normalized)));


            return (float) angleDifference; // Math.Abs(sourceToDestinationAzimuth - parentToSourceAzimuth);
        }

        private float CalculateCanonicLineSeparationDifference(TerrainPathfindingNode source,
            TerrainPathfindingNode destination)
        {
            var p1 = _gratePositionCalculator.ToGlobalPosition(source.Position);
            var p2 = _gratePositionCalculator.ToGlobalPosition(destination.Position);

            var line = _pathfindingSegment.IntraNodesLine;
            var d1 = Mathf.Max(0, line.DistanceToPoint(p1) - _configuration.LineSeparationActivationLength);
            var d2 = Mathf.Max(0, line.DistanceToPoint(p2) - _configuration.LineSeparationActivationLength);

            return d2 - d1;
        }

        public class MovementCostCalculatorConfiguration
        {
            public float HeightRateFactor;
            public float AzimuthFactor;
            public float StepDistanceFactor;
            public float WaySeparationDifferenceFactor;
            public float LineSeparationActivationLength;
        }
    }
}