using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.FinalExecution;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.PreComputation.Configurations;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.Roads.Osm;
using Assets.Roads.Pathfinding;
using OsmSharp.Math.Geo;

namespace Assets.PreComputation
{
    public class PathPrecomputation
    {
        private GameInitializationFields _gameInitializationFields;
        private PrecomputationConfiguration _rootConfiguration;
        private PathPrecomputationConfiguration _pathConfiguration;
        private FilePathsConfiguration _filePathsConfiguration;

        public PathPrecomputation(GameInitializationFields gameInitializationFields,
            PrecomputationConfiguration rootConfiguration, FilePathsConfiguration filePathsConfiguration)
        {
            _gameInitializationFields = gameInitializationFields;
            _rootConfiguration = rootConfiguration;
            _filePathsConfiguration = filePathsConfiguration;
            _pathConfiguration = new PathPrecomputationConfiguration();
        }

        public void Compute()
        {
            var converter = OsmToWrtRoadConverter.Create(
                new OsmToWrtRoadConverter.OsmToWrtConverterConfiguration(),
                _gameInitializationFields.Retrive<TerrainShapeDbProxy>(),
                _rootConfiguration.GeoCoordsToUnityTranslator
            );

            converter.Convert(
                _filePathsConfiguration.OsmFilePath,
                _filePathsConfiguration.PathsPath,
                _pathConfiguration.PathFilterCoords);
        }

        public void Load()
        {
            var roadDb = new RoadDatabaseProxy(new RoadDatabase(_filePathsConfiguration.PathsPath));
            _gameInitializationFields.SetField(roadDb);
        }
    }

    public class PathPrecomputationConfiguration
    {
        public GeoCoordsRect PathFilterCoords = new GeoCoordsRect(
            new GeoCoordinate(49.601, 19.541412),
            new GeoCoordinate(49.6183, 19.5628));
    }
}