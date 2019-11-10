using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Habitat;
using Assets.Heightmaps;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.MT;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Welding;
using Assets.Ring2.Db;
using Assets.Ring2.IntensityProvider;
using Assets.Roads;
using Assets.Roads.Engraving;
using Assets.Roads.Files;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.SpotUpdating;
using Assets.Trees.SpotUpdating.RTAlignment;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class FEInitializingHelper
    {
        private GameInitializationFields _gameInitializationFields;
        private UltraUpdatableContainer _updatableContainer;
        private FEConfiguration _configuration;

        public FEInitializingHelper(GameInitializationFields gameInitializationFields,
            UltraUpdatableContainer updatableContainer, FEConfiguration configuration)
        {
            _gameInitializationFields = gameInitializationFields;
            _updatableContainer = updatableContainer;
            _configuration = configuration;
        }

        public void InitializeUTService<T>(T updatable) where T : BaseUTService
        {
            _gameInitializationFields.SetField(updatable);
            _updatableContainer.Add(updatable);
        }

        public void InitializeMonoliticRing2RegionsDatabase()
        {
            var habitatMap = new HabitatMapDbProxy(new HabitatMapDb(_configuration.HabitatDbInitializationInfo));
            _updatableContainer.AddOtherThreadProxy(habitatMap);

            _gameInitializationFields.SetField(habitatMap);

            var regionsDbGenerator = new Ring2RegionsDbGenerator(habitatMap,
                _configuration.Ring2RegionsDbGeneratorConfiguration(new Ring2AreaDistanceDatabase()),
                _gameInitializationFields.Retrive<RoadDatabaseProxy>());
            var regionsDatabase = regionsDbGenerator.GenerateDatabaseAsync(_configuration.Ring2GenerationArea).Result;
            _gameInitializationFields.SetField(regionsDatabase);
        }

        public void InitializeComplexRing2RegionsDatabase(Dictionary<int,  Ring2RegionsDbGeneratorConfiguration> configurations)
        {
            var habitatMap = new HabitatMapDbProxy(new HabitatMapDb(_configuration.HabitatDbInitializationInfo));
            _updatableContainer.AddOtherThreadProxy(habitatMap);

            _gameInitializationFields.SetField(habitatMap);

            var monoliticDbs = configurations.ToDictionary(pair => pair.Key, pair =>
            {
                var regionsDbGenerator = new Ring2RegionsDbGenerator(habitatMap,
                    pair.Value,
                    _gameInitializationFields.Retrive<RoadDatabaseProxy>());
                return regionsDbGenerator.GenerateDatabaseAsync(_configuration.Ring2GenerationArea).Result;
            });
            var regionsDatabase = new ComplexRing2RegionsDatabase(monoliticDbs);
            _gameInitializationFields.SetField(regionsDatabase);
        }


        public void InitializeUTRendererProxy()
        {
            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(_gameInitializationFields.Retrive<ComputeShaderContainerGameObject>()),
                _configuration.TextureRendererServiceConfiguration));
            _updatableContainer.Add(textureRendererProxy);
            _gameInitializationFields.SetField(textureRendererProxy);
        }

        public void InitializeDesignBodySpotUpdater()
        {
            //var spotUpdater =
            //    new DesignBodySpotUpdater(new DesignBodySpotChangeCalculator(
            //        _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>(),
            //        _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(),
            //        _gameInitializationFields.Retrive<CommonExecutorUTProxy>(),
            //        _gameInitializationFields.Retrive<HeightDenormalizer>()));
            //_gameInitializationFields.SetField(spotUpdater);
            var spotUpdater = new RTAlignedDesignBodySpotUpdater(new DesignBodySpotChangeCalculator(
                    _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>(),
                    _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(),
                    _gameInitializationFields.Retrive<CommonExecutorUTProxy>(),
                    _gameInitializationFields.Retrive<HeightDenormalizer>()),
                new RTAlignedDesignBodySpotUpdaterConfiguration()
                {
                    _topLod = 3,
                    WholeAreaLength = 92160
                }
            );

            var designBodySpotUpdaterProxy = new DesignBodySpotUpdaterProxy(spotUpdater);
            _gameInitializationFields.SetField(designBodySpotUpdaterProxy);
            _updatableContainer.AddOtherThreadProxy(designBodySpotUpdaterProxy);

            var rootMediator = new RootMediatorSpotPositionsUpdater();
            spotUpdater.SetChangesListener(rootMediator);

            _gameInitializationFields.SetField(rootMediator);

            var gRingSpotUpdater = new GRingSpotUpdater(designBodySpotUpdaterProxy);
            _gameInitializationFields.SetField(gRingSpotUpdater);
        }

        public void InitializeGlobalInstancingContainer()
        {
            var globalInstancingContainer = new GlobalGpuInstancingContainer();
            _gameInitializationFields.SetField(globalInstancingContainer);

            var updatableElement = new FieldBasedUltraUpdatable()
            {
                StartCameraField = (currentCamera) => { globalInstancingContainer.StartThread(); },
                UpdateCameraField = (currentCamera) =>
                {
                    globalInstancingContainer.DrawFrame();
                    globalInstancingContainer.FinishUpdateBatch();
                }
            };
            _updatableContainer.AddUpdatableElement(updatableElement);
        }

        public void CreatePathProximityTextureDb()
        {
            var roadDatabaseProxy = new RoadDatabaseProxy(new RoadDatabase(_configuration.RoadDatabasePath));
            _updatableContainer.AddOtherThreadProxy(roadDatabaseProxy);
            _gameInitializationFields.SetField(roadDatabaseProxy);

            PathProximityTextureGenerator proximityTextureGenerator = new PathProximityTextureGenerator(
                _gameInitializationFields.Retrive<TextureConcieverUTProxy>(),
                _configuration.PathProximityTextureGeneratorConfiguration);

            PathProximityArrayGenerator proximityArrayGenerator = new PathProximityArrayGenerator(
                _configuration.PathProximityArrayGeneratorConfiguration);

            var pathProximityTextureDbProxy = new PathProximityTextureDbProxy(new SpatialDb<TextureWithSize>(
                new PathProximityTexturesProvider(roadDatabaseProxy, proximityTextureGenerator,
                    proximityArrayGenerator, _configuration.PathProximityTextureProviderConfiguration),
                _configuration.PathProximityTextureDatabaseConfiguration
            ));

            _gameInitializationFields.SetField(pathProximityTextureDbProxy);
            _updatableContainer.AddOtherThreadProxy(pathProximityTextureDbProxy);
        }

    }
}