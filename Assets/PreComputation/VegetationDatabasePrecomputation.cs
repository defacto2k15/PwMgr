using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.FinalExecution;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.Trees.Db;
using Assets.Trees.Placement;
using Assets.Trees.Placement.Domain;
using Assets.Trees.Placement.Habitats;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using UnityEngine;

namespace Assets.PreComputation
{
    public class VegetationDatabasePrecomputation
    {
        private GameInitializationFields _gameInitializationFields;
        private PrecomputationConfiguration _rootConfiguration;
        private FilePathsConfiguration _pathsConfiguration;
        private VegetationDatabasePrecomputationConfiguration _vegetationConfiguration;

        public VegetationDatabasePrecomputation(GameInitializationFields gameInitializationFields,
            PrecomputationConfiguration rootConfiguration, FilePathsConfiguration pathsConfiguration)
        {
            _gameInitializationFields = gameInitializationFields;
            _rootConfiguration = rootConfiguration;
            _pathsConfiguration = pathsConfiguration;
            _vegetationConfiguration =
                new VegetationDatabasePrecomputationConfiguration(_rootConfiguration.Repositioner,
                    _rootConfiguration.HeightDenormalizer);
        }

        public void Compute()
        {
            CommonExecutorUTProxy commonExecutor = new CommonExecutorUTProxy();
            HabitatMapDbProxy habitatMapDbProxy = _gameInitializationFields.Retrive<HabitatMapDbProxy>();

            ComputeShaderContainerGameObject computeShaderContainer =
                _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>();
            UnityThreadComputeShaderExecutorObject shaderExecutorObject =
                _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>();

            var templatesDict = _vegetationConfiguration.FloraDomainCreationTemplates;
            var floraConfiguration = _vegetationConfiguration.FloraConfiguration;
            var spatialDbConfiguration = _vegetationConfiguration.FloraDomainSpatialDbConfiguration;

            var dbsDict = new Dictionary<HabitatAndZoneType, ISpatialDb<FloraDomainIntensityArea>>();
            foreach (var pair in templatesDict)
            {
                IStoredPartsGenerator<FloraDomainIntensityArea> partsGenerator =
                    new FloraDomainIntensityGenerator(pair.Value, computeShaderContainer, shaderExecutorObject,
                        commonExecutor, 1f, floraConfiguration);
                var spatialDb =
                    new CacheingSpatialDb<FloraDomainIntensityArea>(
                        new SpatialDb<FloraDomainIntensityArea>(partsGenerator, spatialDbConfiguration),
                        spatialDbConfiguration);
                dbsDict[pair.Key] = spatialDb;
            }
            var floraDomainDb = new FloraDomainDbProxy(dbsDict);


            var biomeProvidersGenerators = _vegetationConfiguration.BiomeConfigurationsDict(floraDomainDb)
                .ToDictionary(c => c.Key, c => new BiomeProvidersFromHabitatGenerator(habitatMapDbProxy, c.Value));

            var genralPlacer = new GeneralMultiDistrictPlacer(
                _vegetationConfiguration.GeneralMultiDistrictPlacerConfiguration,
                _gameInitializationFields.Retrive<TerrainShapeDbProxy>(),
                commonExecutor,
                biomeProvidersGenerators);

            var database = genralPlacer.Generate(
                _vegetationConfiguration.GenerationArea,
                _vegetationConfiguration.RankDependentSpeciesCharacteristics(),
                _vegetationConfiguration.GenerationCenter);

            var vegetationOnRoadRemover = new VegetationOnRoadRemover(
                _gameInitializationFields.Retrive<RoadDatabaseProxy>(),
                _vegetationConfiguration.VegetationOnRoadRemoverConfiguration
            );

            var newDb = vegetationOnRoadRemover.RemoveCollidingTrees(database, _vegetationConfiguration.GenerationArea);
            //CreateDebugObjects(newDb.Subjects.ToDictionary(c => c.Key, c => c.Value.QueryAll()));


            VegetationDatabaseFileUtils.WriteToFileNonOverwrite(
                _pathsConfiguration.LoadingVegetationDatabaseDictionaryPath, newDb);
        }


        private void CreateDebugObjects(Dictionary<VegetationLevelRank, IList<VegetationSubject>> dict)
        {
            //var rootObjectsDict = new Dictionary<VegetationLevelRank, GameObject>();
            foreach (var pair in dict)
            {
                var gameObject = new GameObject(pair.Key.ToString());
                //rootObjectsDict[pair.Key] = gameObject;
                CreateDebugObjects(gameObject, pair.Value);
            }
        }

        private void CreateDebugObjects(GameObject rootObject, IList<VegetationSubject> subjects)
        {
            var groupedSubjects = subjects.GroupBy(c => c.CreateCharacteristics.CurrentVegetationType)
                .ToDictionary(c => c.Key, c => c.ToList());

            foreach (var pair in groupedSubjects)
            {
                var type = pair.Key;
                var subrootGo = new GameObject(type.ToString());
                subrootGo.transform.SetParent(rootObject.transform);

                foreach (var subject in pair.Value)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localPosition = new Vector3(subject.XzPosition.x, 0, subject.XzPosition.y);
                    go.transform.SetParent(subrootGo.transform);
                }
            }
        }
    }
}