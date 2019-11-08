using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Caching;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.Ring1.VisibilityTexture;
using Assets.PreComputation;
using Assets.PreComputation.Configurations;
using Assets.Ring2;
using Assets.Roads;
using Assets.Roads.Engraving;
using Assets.Roads.TerrainFeature;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class FETerrainShapeDbInitialization
    {
        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields;
        private FEConfiguration _configuration;
        private FilePathsConfiguration _filePathsConfiguration;

        public FETerrainShapeDbInitialization(UltraUpdatableContainer ultraUpdatableContainer,
            GameInitializationFields gameInitializationFields, FEConfiguration configuration,
            FilePathsConfiguration filePathsConfiguration)
        {
            _ultraUpdatableContainer = ultraUpdatableContainer;
            _gameInitializationFields = gameInitializationFields;
            _configuration = configuration;
            _filePathsConfiguration = filePathsConfiguration;
        }

        public void Start()
        {
            var heightmapTextureSize = new IntVector2(3600, 3600);
            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile(_filePathsConfiguration.HeightmapFilePath,
                heightmapTextureSize.X, heightmapTextureSize.Y, TextureFormat.ARGB32, true, false);

            TerrainTextureFormatTransformator transformator =
                new TerrainTextureFormatTransformator(_gameInitializationFields.Retrive<CommonExecutorUTProxy>());
            var globalHeightTexture = transformator.EncodedHeightTextureToPlain(new TextureWithSize()
            {
                Size = heightmapTextureSize,
                Texture = rgbaMainTexture
            });

            if (true)
            {
                _gameInitializationFields.SetField(new TerrainDetailAlignmentCalculator(240));

                TerrainDetailGenerator terrainDetailGenerator =
                    CreateTerrainDetailGenerator(
                        globalHeightTexture,
                        _gameInitializationFields.Retrive<UTTextureRendererProxy>(),
                        _gameInitializationFields.Retrive<CommonExecutorUTProxy>(),
                        _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(),
                        _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>());

                TerrainDetailProvider terrainDetailProvider =
                    CreateTerrainDetailProvider(terrainDetailGenerator,
                        _gameInitializationFields.Retrive<CommonExecutorUTProxy>());

                var commonExecutorUtProxy = _gameInitializationFields.Retrive<CommonExecutorUTProxy>();
                var terrainShapeDb = CreateTerrainShapeDb(terrainDetailProvider, commonExecutorUtProxy, _gameInitializationFields.Retrive<TerrainDetailAlignmentCalculator>());

                TerrainShapeDbProxy terrainShapeDbProxy = new TerrainShapeDbProxy(terrainShapeDb);
                var baseTerrainDetailProvider = BaseTerrainDetailProvider.CreateFrom(terrainShapeDb);
                _gameInitializationFields.SetField(baseTerrainDetailProvider);
                terrainDetailGenerator.SetBaseTerrainDetailProvider( baseTerrainDetailProvider);

                _ultraUpdatableContainer.AddOtherThreadProxy(terrainShapeDbProxy);

                _gameInitializationFields.SetField(terrainShapeDbProxy);
            }
            else
            {
                ITerrainShapeDb terrainShapeDbProxy = new DebugSlopedTerrainShapeDb(
                    _gameInitializationFields.Retrive<UTTextureRendererProxy>()
                );

                terrainShapeDbProxy = new RecordingTerrainShapeDb(terrainShapeDbProxy);
                _gameInitializationFields.SetField((RecordingTerrainShapeDb) terrainShapeDbProxy);
            }
        }

        public static TerrainShapeDb CreateTerrainShapeDb(TerrainDetailProvider terrainDetailProvider, CommonExecutorUTProxy commonExecutorUtProxy, TerrainDetailAlignmentCalculator terrainDetailAlignmentCalculator)
        {
            var terrainShapeDb = new TerrainShapeDb(
                new CachedTerrainDetailProvider(
                    terrainDetailProvider,
                    () => new InMemoryAssetsCache<IntRectangle, TextureWithSize>(new InMemoryAssetsLevel2Cache<IntRectangle, TextureWithSize>(
                        new InMemoryCacheConfiguration(), new TextureWithSizeActionsPerformer(commonExecutorUtProxy))
                    )),
                terrainDetailAlignmentCalculator);
            return terrainShapeDb;
        }

        private TerrainDetailGenerator CreateTerrainDetailGenerator(
            Texture mainTexture,
            UTTextureRendererProxy utTextureRendererProxy,
            CommonExecutorUTProxy commonExecutorUtProxy,
            UnityThreadComputeShaderExecutorObject computeShaderExecutorObject,
            ComputeShaderContainerGameObject containerGameObject)
        {
            RoadEngravingTerrainFeatureApplier engravingTerrainFeatureApplier = null;
            if (_configuration.EngraveRoadsInTerrain)
            {
                engravingTerrainFeatureApplier = CreateRoadEngravingFeatureApplier();
            }

            var featureAppliers =
                FinalTerrainFeatureAppliers.CreateFeatureAppliers(
                    utTextureRendererProxy, containerGameObject, commonExecutorUtProxy, computeShaderExecutorObject,
                    engravingTerrainFeatureApplier);

            TerrainDetailGeneratorConfiguration generatorConfiguration =
                _configuration.TerrainDetailGeneratorConfiguration;

            TextureWithCoords fullFundationData = new TextureWithCoords(new TextureWithSize()
            {
                Texture = mainTexture,
                Size = new IntVector2(mainTexture.width, mainTexture.height)
            }, new MyRectangle(0, 0, 3601 * 24, 3601 * 24));

            TerrainDetailGenerator generator = new TerrainDetailGenerator(generatorConfiguration,
                utTextureRendererProxy, fullFundationData, featureAppliers, commonExecutorUtProxy);

            return generator;
        }

        private TerrainDetailProvider CreateTerrainDetailProvider(
            TerrainDetailGenerator generator,
            CommonExecutorUTProxy commonExecutorUtProxy)
        {
            var terrainDetailProviderConfiguration = _configuration.TerrainDetailProviderConfiguration;

            var terrainDetailFileManager =
                new TerrainDetailFileManager(_configuration.TerrainDetailCachePath, commonExecutorUtProxy);

            var cornerMerger = new TerrainDetailCornerMerger(
                new LateAssignFactory<BaseTerrainDetailProvider>(() => _gameInitializationFields.Retrive<BaseTerrainDetailProvider>()), 
                _gameInitializationFields.Retrive<TerrainDetailAlignmentCalculator>(),
                _gameInitializationFields.Retrive<UTTextureRendererProxy>(),
                _gameInitializationFields.Retrive<TextureConcieverUTProxy>());

            var provider = new TerrainDetailProvider(
                terrainDetailProviderConfiguration, terrainDetailFileManager, generator, cornerMerger, _gameInitializationFields.Retrive<TerrainDetailAlignmentCalculator>());

            return provider;
        }

        private RoadEngravingTerrainFeatureApplier CreateRoadEngravingFeatureApplier()
        {
            PathProximityTextureDbProxy pathProximityDb =
                _gameInitializationFields.Retrive<PathProximityTextureDbProxy>();

            RoadEngraver.RoadEngraverConfiguration roadEngraverConfiguration = _configuration.RoadEngraverConfiguration;

            RoadEngraver roadEngraver =
                new RoadEngraver(_gameInitializationFields.Retrive<ComputeShaderContainerGameObject>(),
                    _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(),
                    roadEngraverConfiguration);

            RoadEngravingTerrainFeatureApplierConfiguration roadEngravingTerrainFetureApplierConfiguration =
                _configuration.RoadEngravingTerrainFetureApplierConfiguration;
            return new RoadEngravingTerrainFeatureApplier(pathProximityDb, roadEngraver,
                roadEngravingTerrainFetureApplierConfiguration);
        }
    }
}