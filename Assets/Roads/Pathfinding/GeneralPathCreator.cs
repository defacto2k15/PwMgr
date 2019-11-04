using System.Collections.Generic;
using System.Linq;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Roads.Pathfinding.TerrainPath;
using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Pathfinding
{
    public class GeneralPathCreator
    {
        private TerrainPathfinder _terrainPathfinder;
        private GratedPathSimplifier _gratedPathSimplifier;
        private PathFitter _pathFitter;
        private PathQuantisizer _pathQuantisizer;

        public GeneralPathCreator(TerrainPathfinder terrainPathfinder, GratedPathSimplifier gratedPathSimplifier,
            PathFitter pathFitter, PathQuantisizer pathQuantisizer)
        {
            _terrainPathfinder = terrainPathfinder;
            _gratedPathSimplifier = gratedPathSimplifier;
            _pathFitter = pathFitter;
            _pathQuantisizer = pathQuantisizer;
        }

        public PathQuantisized GeneratePath(List<Vector2> wayPoints)
        {
            var generatedPath = _terrainPathfinder.GeneratePath(wayPoints);
            Preconditions.Assert(generatedPath.GenerationSucceded, "Path generation failed");

            var simplifiedPathSegments = generatedPath.PathSegments.Select(c => _gratedPathSimplifier.Simplify(c))
                .ToList();

            var unitedPath = UnitePath(simplifiedPathSegments);

            var fittedPath = FitPath(unitedPath);
            if (fittedPath == null)
            {
                return null;
            }
            var quantisizedPath = _pathQuantisizer.GenerateQuantisizedPath(fittedPath);
            return quantisizedPath;
        }

        private PathCurve FitPath(List<Vector2> unitedPath)
        {
            var pointsCount = unitedPath.Count;

            if (pointsCount < 2)
            {
                Debug.LogError($"Points count is less than 2. It is {pointsCount}");
                return null;
            }
            else if (pointsCount == 2)
            {
                return _pathFitter.FitPath(unitedPath, 2, 1);
            }
            else if (pointsCount == 3)
            {
                return _pathFitter.FitPath(unitedPath, 3, 2);
            }
            else if (pointsCount == 4)
            {
                return _pathFitter.FitPath(unitedPath, 4, 2);
            }
            else
            {
                return _pathFitter.FitPath(unitedPath, 5, 3);
            }
        }

        private List<Vector2> UnitePath(List<List<Vector2>> pathSegments)
        {
            var outList = new List<Vector2>();
            outList.AddRange(pathSegments.First());
            for (int i = 1; i < pathSegments.Count; i++)
            {
                outList.AddRange(pathSegments[i].Skip(1));
            }
            return outList;
        }
    }
}