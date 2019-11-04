using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.FinalExecution;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation.Configurations;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.TerrainMat;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;

namespace Assets.PreComputation
{
    public class Ring1Precomputation
    {
        private GameInitializationFields _gameInitializationFields;
        private PrecomputationConfiguration _configuration;
        private FilePathsConfiguration _pathsConfiguration;
        private StainTerrainResource _stainTerrainResource;

        public Ring1Precomputation(GameInitializationFields gameInitializationFields,
            PrecomputationConfiguration configuration, FilePathsConfiguration pathsConfiguration)
        {
            _gameInitializationFields = gameInitializationFields;
            _configuration = configuration;
            _pathsConfiguration = pathsConfiguration;
        }

        public void Compute()
        {
            TaskUtils.SetGlobalMultithreading(_configuration.Multithreading);

            StainTerrainResourceCreatorUTProxy stainTerrainResourceCreator =
                new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator());

            RoadToBiomesConventer roadToBiomesConventer =
                new RoadToBiomesConventer(
                    _gameInitializationFields.Retrive<RoadDatabaseProxy>(),
                    _configuration.RoadToBiomesConventerConfiguration
                );

            HabitatToStainBiomeConventer habitatToBiomesConventer = new HabitatToStainBiomeConventer(
                _gameInitializationFields.Retrive<HabitatMapDbProxy>(),
                _configuration.HabitatToStainBiomeConversionConfiguration);

            var terrainProvider = new StainTerrainProvider(
                stainTerrainResourceCreator,
                roadToBiomesConventer,
                habitatToBiomesConventer,
                _configuration.StainTerrainProviderConfiguration);

            var generator = terrainProvider.ProvideGeneratorAsync().Result;
            _stainTerrainResource = generator.GenerateTerrainTextureDataAsync().Result;

            var fromManualTextureResourceLoader = new FromManualTextureStainResourceLoader(
                new BiomeInstanceDetailGenerator(_configuration.BiomeInstanceDetailTemplates),
                _configuration.FromManualTextureStainResourceLoaderConfiguration);
            //fromManualTextureResourceLoader.ProvideData();

            var resourceGenerator =
                new ComputationStainTerrainResourceGenerator(
                    new StainTerrainResourceComposer(stainTerrainResourceCreator), new StainTerrainArrayMelder(),
                    fromManualTextureResourceLoader);

            _stainTerrainResource = resourceGenerator.GenerateTerrainTextureDataAsync().Result;

            var fileManager = new StainTerrainResourceFileManager(_pathsConfiguration.StainTerrainServicePath, new CommonExecutorUTProxy());
            fileManager.SaveResources(_stainTerrainResource);
        }

        public void Load()
        {
            if (_stainTerrainResource == null)
            {
                var fileManager = new StainTerrainResourceFileManager(_pathsConfiguration.StainTerrainServicePath, new CommonExecutorUTProxy());
                _stainTerrainResource = fileManager.LoadResources().Result;
            }
        }
    }
}