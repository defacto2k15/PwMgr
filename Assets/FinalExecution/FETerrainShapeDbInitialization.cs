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
                    CreateTerrainDetailProvider(terrainDetailGenerator);

                var commonExecutorUtProxy = _gameInitializationFields.Retrive<CommonExecutorUTProxy>();

                var terrainDetailFileManager =
                    new TerrainDetailFileManager(_configuration.TerrainDetailCachePath, commonExecutorUtProxy);
                var terrainShapeDb = CreateTerrainShapeDb(terrainDetailProvider
                    , commonExecutorUtProxy
                    , _gameInitializationFields.Retrive<TerrainDetailAlignmentCalculator>()
                    , _configuration.TerrainShapeDbConfiguration.MergeTerrainDetail
                    , _configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk
                    , _configuration.TerrainShapeDbConfiguration.UseTextureLoadingFromDisk, terrainDetailFileManager);

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

        public static TerrainShapeDb CreateTerrainShapeDb(TerrainDetailProvider terrainDetailProvider, CommonExecutorUTProxy commonExecutorUtProxy,
            TerrainDetailAlignmentCalculator terrainDetailAlignmentCalculator, bool mergingEnabled, bool saveTexturesToFile, bool loadTexturesFromFile,
            TerrainDetailFileManager fileManager)
        {
            var cachingConfiguration = new CachingConfiguration()
            {
                SaveAssetsToFile = saveTexturesToFile,
                UseFileCaching = loadTexturesFromFile
            };

            Func<IAssetsCache<InternalTerrainDetailElementToken, TextureWithSize>> terrainCacheGenerator =
                () => new InMemoryAssetsCache<InternalTerrainDetailElementToken, TextureWithSize>(
                    CreateLevel2AssetsCache<InternalTerrainDetailElementToken, TextureWithSize>(
                        cachingConfiguration,
                        new InMemoryCacheConfiguration(),
                        new TextureWithSizeActionsPerformer(commonExecutorUtProxy),
                        new CachingTerrainDetailFileManager(fileManager)));

            var cachedTerrainDetailProvider = new CachedTerrainDetailProvider(
                terrainDetailProvider,
                terrainCacheGenerator, mergingEnabled);
            cachedTerrainDetailProvider.Initialize().Wait();
            var terrainShapeDb = new TerrainShapeDb( cachedTerrainDetailProvider, terrainDetailAlignmentCalculator);
            return terrainShapeDb;
        }

        public static ILevel2AssetsCache<TQuery, TAsset> CreateLevel2AssetsCache<TQuery, TAsset>(CachingConfiguration cachingConfiguration,
            InMemoryCacheConfiguration inMemoryCacheConfiguration, MemoryCachableAssetsActionsPerformer<TAsset> entityActionsPerformer,
            IAssetCachingFileManager<TQuery, TAsset> fileManager) where TQuery : IFromQueryFilenameProvider where TAsset : class
        {
            var inMemoryAssetsLevel2Cache = new InMemoryAssetsLevel2Cache<TQuery, TAsset>(inMemoryCacheConfiguration, entityActionsPerformer);
            if (!cachingConfiguration.UseFileCaching)
            {
                return inMemoryAssetsLevel2Cache;
            }
            else
            {
                return new TwoStorageOverseeingLevel2Cache<TQuery, TAsset>(new InFilesAssetsCache<TQuery, TAsset>(fileManager), inMemoryAssetsLevel2Cache, cachingConfiguration.SaveAssetsToFile );
            }
        }

        private TerrainDetailGenerator CreateTerrainDetailGenerator(
            Texture mainTexture,
            UTTextureRendererProxy utTextureRendererProxy,
            CommonExecutorUTProxy commonExecutorUtProxy,
            UnityThreadComputeShaderExecutorObject computeShaderExecutorObject,
            ComputeShaderContainerGameObject containerGameObject)
        {

            var featureAppliers =new List<RankedTerrainFeatureApplier>();

            if (_configuration.EngraveTerrainFeatures)
            {
                featureAppliers.AddRange(FinalTerrainFeatureAppliers.CreateFeatureAppliers(
                    utTextureRendererProxy, containerGameObject, commonExecutorUtProxy, computeShaderExecutorObject
                ));
            }
            if (_configuration.EngraveRoadsInTerrain)
            {
                featureAppliers.Add(FinalTerrainFeatureAppliers.CreateRoadEngravingApplier( CreateRoadEngravingFeatureApplier()));
            }

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

        private TerrainDetailProvider CreateTerrainDetailProvider(TerrainDetailGenerator generator)
        {
            var cornerMerger = new TerrainDetailCornerMerger(
                new LateAssignFactory<BaseTerrainDetailProvider>(() => _gameInitializationFields.Retrive<BaseTerrainDetailProvider>()), 
                _gameInitializationFields.Retrive<TerrainDetailAlignmentCalculator>(),
                _gameInitializationFields.Retrive<UTTextureRendererProxy>(),
                _gameInitializationFields.Retrive<TextureConcieverUTProxy>(),
                _configuration.TerrainMergerConfiguration);

            var provider = new TerrainDetailProvider( generator, cornerMerger, _gameInitializationFields.Retrive<TerrainDetailAlignmentCalculator>());

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

    public class CachingConfiguration
    {
        public bool UseFileCaching;
        public bool SaveAssetsToFile;
    }
}