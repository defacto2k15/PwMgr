using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Roads.Osm;
using Assets.Roads.Pathfinding;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Roads.Pathfinding.TerrainPath;
using Assets.Utils;
using UnityEngine;

namespace Assets.Roads.Files
{
    public class OsmToWrtRoadConverter
    {
        private readonly GeoCoordsToUnityTranslator _coordsTranslator;
        private readonly OsmWaysExtractor _extractor;
        private readonly GeneralPathCreator _generalPathCreator;
        private readonly PathFileManager _pathFileManager;

        public OsmToWrtRoadConverter(GeoCoordsToUnityTranslator coordsTranslator, OsmWaysExtractor extractor,
            GeneralPathCreator generalPathCreator, PathFileManager pathFileManager)
        {
            _coordsTranslator = coordsTranslator;
            _extractor = extractor;
            _generalPathCreator = generalPathCreator;
            _pathFileManager = pathFileManager;
        }

        public void Convert(string osmPath, string wrtRootPath, GeoCoordsRect osmCoordsFilter)
        {
            var osmWays = _extractor.ExtractWays(osmPath, osmCoordsFilter).ToList();

            var generatedPaths = new List<PathQuantisized>();
            int i = 0;
            foreach (var aOsmWay in osmWays.Where(c => c.Nodes.Count > 1))
            {
                var translatedNodePositions = aOsmWay.Nodes.Select(n => _coordsTranslator.TranslateToUnity(n.Position))
                    .ToList();
                var quantisizedPath = _generalPathCreator.GeneratePath(translatedNodePositions);
                if (quantisizedPath.Line.IsEmpty)
                {
                    continue;
                }
                generatedPaths.Add(quantisizedPath);
                i++;
            }
            _pathFileManager.SavePaths(wrtRootPath, generatedPaths);
        }

        public static OsmToWrtRoadConverter Create(
            OsmToWrtConverterConfiguration configuration,
            TerrainShapeDbProxy terrainShapeDbProxy,
            GeoCoordsToUnityTranslator coordsTranslator)
        {
            var extractor = new OsmWaysExtractor();

            TerrainPathfinder terrainPathfinder =
                new TerrainPathfinder(new TerrainPathfinder.TerrainPathfinderConfiguration(),
                    configuration.OneGrateSideLength,
                    terrainShapeDbProxy, null, null);

            GratedPathSimplifier gratedPathSimplifier = new GratedPathSimplifier();

            PathFitter pathFitter = new PathFitter();

            PathQuantisizer pathQuantisizer = new PathQuantisizer(configuration.SamplesPerUnit);

            var generalPathCreator = new GeneralPathCreator(terrainPathfinder, gratedPathSimplifier, pathFitter,
                pathQuantisizer);

            var pathFileManager = new PathFileManager();

            return new OsmToWrtRoadConverter(coordsTranslator, extractor, generalPathCreator, pathFileManager);
        }

        public class OsmToWrtConverterConfiguration
        {
            public float OneGrateSideLength = 1f;
            public float SamplesPerUnit = 4f;
        }
    }
}