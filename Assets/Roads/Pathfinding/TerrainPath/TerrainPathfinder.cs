using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Roads.Pathfinding.AStar;
using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Pathfinding.TerrainPath
{
    public class TerrainPathfinder
    {
        private TerrainPathfinderConfiguration _configuration;
        private float _oneGrateSideLength;
        private TerrainShapeDbProxy _terrainShapeDbProxy;
        private Action<TerrainPathfindingNodeDetails, TerrainPathfindingNode> _debugActionOnNodeCreation;
        private readonly Action<List<Vector2>> _debugPerSegmentCompleted;

        public TerrainPathfinder(TerrainPathfinderConfiguration configuration, float oneGrateSideLength,
            TerrainShapeDbProxy terrainShapeDbProxy, Action<TerrainPathfindingNodeDetails,
                TerrainPathfindingNode> debugActionOnNodeCreation, Action<List<Vector2>> debugPerSegmentCompleted)
        {
            _configuration = configuration;
            _oneGrateSideLength = oneGrateSideLength;
            _terrainShapeDbProxy = terrainShapeDbProxy;
            _debugActionOnNodeCreation = debugActionOnNodeCreation;
            _debugPerSegmentCompleted = debugPerSegmentCompleted;
        }

        public PathGenerationResult GeneratePath(List<Vector2> globalNodePositions)
        {
            ///////// NOW FINDING PATH
            float finalPointDistanceFactor = _configuration.FloatPointDistanceFactor; //3;
            float finalLineDistanceFactor = _configuration.FinalLineDistanceFactor; //3;
            float minimalGoalValue = _configuration.MinimalGoalValue; // 30;

            float stepDistanceFactor = _configuration.StepDistanceFactor; //1;
            float azimuthFactor = _configuration.AzimuthFactor; //0;
            float heightRateFactor = _configuration.HeightRateFactor; //1;
            float waySeparationDifferenceFactor = _configuration.WaySeparationDifferenceFactor;
            float lineSeparationActivationLength = _configuration.LineSeparationActivationLength;

            float nodeAccomplishedDistance = _configuration.NodeAccomplishedDistance; //4;

            GratePositionCalculator gratePositionCalculator =
                new GratePositionCalculator(new Vector2(0, 0), _oneGrateSideLength);
            TerrainSamplingSource samplingSource =
                new TerrainSamplingSource(_terrainShapeDbProxy, gratePositionCalculator);

            var nodePositions =
                globalNodePositions.Select(c => new IntVector2(Mathf.RoundToInt(c.x / _oneGrateSideLength),
                    Mathf.RoundToInt(c.y / _oneGrateSideLength))).ToList();

            var gratedGeneratedSegments = new List<List<Vector2>>();
            for (int i = 0; i < nodePositions.Count - 1; i++)
            {
                var pathfindingSegment = PathfindingSegment.Calculate(nodePositions[i], nodePositions[i + 1],
                    gratePositionCalculator, samplingSource);

                PathfindingNodesRepositiory nodesRepositiory = new PathfindingNodesRepositiory(
                    new IntVector2((int) (90 * _oneGrateSideLength), (int) (90 * _oneGrateSideLength)));

                var nodePositionMaximas = new IntVector2(
                    nodePositions.Max(c => c.X),
                    nodePositions.Max(c => c.Y)
                );

                var nodePositionMinimas = new IntVector2(
                    nodePositions.Min(c => c.X),
                    nodePositions.Min(c => c.Y)
                );

                var boundariesRectangle = new IntRectangle(
                        nodePositionMinimas.X,
                        nodePositionMinimas.Y,
                        nodePositionMaximas.X - nodePositionMinimas.X,
                        nodePositionMaximas.Y - nodePositionMinimas.Y
                    ).EnlargeByMargins(Mathf.CeilToInt(_configuration.BoundariesMargin))
                    .BoundBy(_configuration.GlobalMapBoundaries.ToIntRectange());

                NodeChildrenFinder childrenFinder = new NodeChildrenFinder(
                    nodesRepositiory,
                    new List<IntVector2>()
                    {
                        new IntVector2(-1, -1),
                        new IntVector2(0, -1),
                        new IntVector2(1, -1),
                        new IntVector2(1, 0),
                        new IntVector2(1, 1),
                        new IntVector2(0, 1),
                        new IntVector2(-1, 1),
                        new IntVector2(-1, 0),
                    },
                    boundariesRectangle);

                MovementCostCalculator movementCostCalculator = new MovementCostCalculator(
                    new MovementCostCalculator.MovementCostCalculatorConfiguration()
                    {
                        StepDistanceFactor = stepDistanceFactor,
                        AzimuthFactor = azimuthFactor,
                        HeightRateFactor = heightRateFactor,
                        WaySeparationDifferenceFactor = waySeparationDifferenceFactor,
                        LineSeparationActivationLength = lineSeparationActivationLength,
                    }, gratePositionCalculator, pathfindingSegment);

                EstimatedCostCalculator estimatedCostCalculator =
                    new EstimatedCostCalculator(pathfindingSegment, gratePositionCalculator, stepDistanceFactor,
                        heightRateFactor);

                IsGoalResolver isGoalResolver = new IsGoalResolver(
                    new IsGoalResolver.IsGoalResoverConfiguration()
                    {
                        FinalPointDistanceFactor = finalPointDistanceFactor,
                        FinalLineDistanceFactor = finalLineDistanceFactor,
                        MinimalGoalValue = minimalGoalValue
                    }, pathfindingSegment, gratePositionCalculator);

                var nodesGenerator = new PathfindingNodesGenerator(
                    movementCostCalculator,
                    estimatedCostCalculator,
                    isGoalResolver,
                    childrenFinder,
                    samplingSource,
                    _debugActionOnNodeCreation);
                childrenFinder.SetNodesGenerator(nodesGenerator);

                var startNode = nodesGenerator.Generate(nodePositions[i], null);
                nodesRepositiory.Add(startNode);
                var solver = new AStarSolver(startNode, null);

                var result = solver.Run();

                if (result != AStarSolverState.GoalFound)
                {
                    return new PathGenerationResult()
                    {
                        GenerationSucceded = false
                    };
                }
                else
                {
                    var solverPath = solver.GetPath().Cast<TerrainPathfindingNode>().Select(c => c.Position).ToList();
                    if (solverPath.Count < 2) //too small, propably start and finish node beside itself..
                    {
                        solverPath = new List<IntVector2>()
                        {
                            nodePositions[i],
                            nodePositions[i + 1],
                        };
                    }
                    var path = solverPath
                        .Select(c => c.ToFloatVec() * _oneGrateSideLength).ToList();
                    gratedGeneratedSegments.Add(path);
                    _debugPerSegmentCompleted?.Invoke(path);
                }
                nodesRepositiory.Clear();
            }

            return new PathGenerationResult()
            {
                GenerationSucceded = true,
                PathSegments = gratedGeneratedSegments
            };
        }

        public class TerrainPathfinderConfiguration
        {
            public float FloatPointDistanceFactor = 3;
            public float FinalLineDistanceFactor = 3;
            public float MinimalGoalValue = 30;
            public float StepDistanceFactor = 0.00004f;
            public float AzimuthFactor = 0.0001f;
            public float HeightRateFactor = 1000000;
            public float WaySeparationDifferenceFactor = 0.0001f;
            public float LineSeparationActivationLength = 6;
            public float NodeAccomplishedDistance = 4;

            public MyRectangle GlobalMapBoundaries = new MyRectangle(0, 0, 3600, 3600);
            public float BoundariesMargin = 20f;
        }
    }
}