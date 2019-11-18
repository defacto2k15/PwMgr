using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Assets.Caching;
using Assets.ESurface;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Ring2.Db;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Ring2.Stamping;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.UTUpdating;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Assets.ETerrain.ETerrainIntegration.deos
{
    public class ETerrainIntegrationWithShapeDbAndSurfaceDbDEO: MonoBehaviour
    {
        public GameObject Traveller;
        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;

        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields = new GameInitializationFields();
        private FEConfiguration _configuration;
        private UpdaterUntilException _updaterUntilException = new UpdaterUntilException();

        public void Start()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Start");
            _configuration = new FEConfiguration(new FilePathsConfiguration()) {Multithreading = false};
            _configuration.TerrainShapeDbConfiguration.UseTextureLoadingFromDisk = true;
            _configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk = true;
            _configuration.TerrainShapeDbConfiguration.MergeTerrainDetail = true;
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            _gameInitializationFields = new GameInitializationFields();
            Dictionary<int, Ring2RegionsDbGeneratorConfiguration> ring2RegionsDatabasesConfiguration = Enumerable.Range(0, 3)
                .ToDictionary(i => i, i => _configuration.Ring2RegionsDbGeneratorConfiguration(new Ring2AreaDistanceDatabase()));

            ring2RegionsDatabasesConfiguration[1].GenerateRoadHabitats = false;
            ring2RegionsDatabasesConfiguration[1].MinimalRegionArea = 2000;
            ring2RegionsDatabasesConfiguration[2].GenerateRoadHabitats = false;
            ring2RegionsDatabasesConfiguration[2].MinimalRegionArea = 2000;

            _ultraUpdatableContainer =
                ETerrainTestUtils.InitializeFinalElements(_configuration, containerGameObject, _gameInitializationFields, ring2RegionsDatabasesConfiguration);

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            startConfiguration.CommonConfiguration.YScale = _gameInitializationFields.Retrive<HeightDenormalizer>().DenormalizationMultiplier;
            startConfiguration.CommonConfiguration.InterSegmentMarginSize = 1/6.0f;
            startConfiguration.InitialTravellerPosition = new Vector2(490, -21) + new Vector2(90f*8, 90f*4);
            //startConfiguration.InitialTravellerPosition = new Vector2(0,0);
            startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() { HeightPyramidLevel.Top, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom};

            ETerrainHeightBuffersManager buffersManager = new ETerrainHeightBuffersManager();
            _eTerrainHeightPyramidFacade = new ETerrainHeightPyramidFacade(buffersManager,
                _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                _gameInitializationFields.Retrive<UTTextureRendererProxy>(), startConfiguration);

            var perLevelTemplates = _eTerrainHeightPyramidFacade.GenerateLevelTemplates();
            var levels = startConfiguration.PerLevelConfigurations.Keys.Where(c=> startConfiguration.HeightPyramidLevels.Contains(c));
            buffersManager.InitializeBuffers(levels.ToDictionary(c => c, c => new EPyramidShaderBuffersGeneratorPerRingInput()
            {
                CeilTextureResolution = startConfiguration.CommonConfiguration.CeilTextureSize.X,  //TODO i use only X, - works only for squares
                HeightMergeRanges = perLevelTemplates[c].LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                PyramidLevelWorldSize = startConfiguration.PerLevelConfigurations[c].PyramidLevelWorldSize.Width,  // TODO works only for square pyramids - i use width
                RingUvRanges = startConfiguration.CommonConfiguration.RingsUvRange
            }),startConfiguration.CommonConfiguration.MaxLevelsCount, startConfiguration.CommonConfiguration.MaxRingsPerLevelCount);

            var repositioner = Repositioner.Default;
            var dbInitialization =
                new FETerrainShapeDbInitialization(_ultraUpdatableContainer, _gameInitializationFields, _configuration, new FilePathsConfiguration());
            dbInitialization.Start();
            var dbProxy = _gameInitializationFields.Retrive<TerrainShapeDbProxy>();

            _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>
                {
                    //[EGroundTextureType.HeightMap] = ETerrainIntegrationUsingTerrainDatabaseDEO.GenerateHeightTextureEntitiesGeneratorFromTerrainShapeDb(
                    //    startConfiguration, dbProxy, repositioner, _gameInitializationFields.Retrive< UTTextureRendererProxy>()  ),
                    [EGroundTextureType.SurfaceTexture] = GenerateSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
                        _configuration,startConfiguration,_gameInitializationFields,_ultraUpdatableContainer,repositioner)
                }
            );

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            Debug.Log("Init time "+msw.CollectResults());
        }

        public void Update()
        {
            var camera = FindObjectOfType<Camera>();
            camera.transform.position = Traveller.transform.position;
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new EncapsulatedCameraForUpdate(FindObjectOfType<Camera>())); });
            var position3D = Traveller.transform.position;
            var flatPosition = new Vector2(position3D.x, position3D.z);

            _eTerrainHeightPyramidFacade.Update(flatPosition);

            if (Time.frameCount > 10)
            {
                //EditorApplication.isPaused = true;
            }
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator GenerateSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
            FEConfiguration configuration, ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, GameInitializationFields gameInitializationFields
            , UltraUpdatableContainer ultraUpdatableContainer, Repositioner repositioner)
        {
            var surfaceTextureFormat = RenderTextureFormat.ARGB32;

            var feRing2PatchConfiguration = new FeRing2PatchConfiguration(configuration);
            feRing2PatchConfiguration.Ring2PlateStamperConfiguration.PlateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                [0] = 3f,
                [1] = 3 /8f,
                [2] = 3 / 64f
            };
            feRing2PatchConfiguration.Ring2PatchesOverseerConfiguration_IntensityPatternPixelsPerUnit= new Dictionary<int, float>()
            {
                [0] = 1/3f,
                [1] = 1 /(3*8f),
                [2] = 1 /(3f*32f)
            }; 

            int mipmapLevelToExtract = 1;
            feRing2PatchConfiguration.Ring2PlateStamperConfiguration.PlateStampPixelsPerUnit =
                feRing2PatchConfiguration.Ring2PlateStamperConfiguration.PlateStampPixelsPerUnit.ToDictionary(
                    c => c.Key,
                    c => c.Value * Mathf.Pow(2, mipmapLevelToExtract)
                );

            var patchInitializer = new Ring2PatchInitialization(gameInitializationFields, ultraUpdatableContainer, feRing2PatchConfiguration);
            patchInitializer.Start();

            var mipmapExtractor = new MipmapExtractor(gameInitializationFields.Retrive<UTTextureRendererProxy>());
            var patchesCreatorProxy = gameInitializationFields.Retrive<GRing2PatchesCreatorProxy>();
            var  patchStamperOverseerFinalizer= gameInitializationFields.Retrive<Ring2PatchStamplingOverseerFinalizer>();
            var surfacePatchProvider = new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer,gameInitializationFields.Retrive<CommonExecutorUTProxy>(),
                mipmapExtractor, mipmapLevelToExtract);

            var commonExecutor = gameInitializationFields.Retrive<CommonExecutorUTProxy>();
            var cachedSurfacePatchProvider =
                new CachedESurfacePatchProvider(surfacePatchProvider
                    , new InMemoryAssetsCache<ESurfaceTexturesPackToken, NullableESurfaceTexturesPack>(
                        FETerrainShapeDbInitialization.CreateLevel2AssetsCache<ESurfaceTexturesPackToken, NullableESurfaceTexturesPack>(
                            cachingConfiguration: new CachingConfiguration()
                            {
                                SaveAssetsToFile =true,
                                UseFileCaching =true,
                            }
                            , new InMemoryCacheConfiguration() /*{ MaxTextureMemoryUsed = 0}*/
                            , new ESurfaceTexturesPackEntityActionsPerformer(commonExecutor)
                            , new ESurfaceTexturesPackFileManager(commonExecutor, configuration.FilePathsConfiguration.SurfacePatchCachePath))));
            cachedSurfacePatchProvider.Initialize().Wait();

            var textureRendererProxy = gameInitializationFields.Retrive<UTTextureRendererProxy>();

            return new OneGroundTypeLevelTextureEntitiesGenerator()
            {
                GeneratorFunc =
                    (level) =>
                    {
                        var ceilTexture =
                            EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize, surfaceTextureFormat);
                        var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture
                            , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                        var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                        var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                        return new SegmentFillingListenerWithCeilTexture()
                        {
                            CeilTexture = ceilTexture,
                            SegmentFillingListener =
                                new LambdaSegmentFillingListener(
                                    (c) =>
                                    {
                                        {
                                                var surfaceWorldSpaceRectangle = ETerrainUtils.SurfaceTextureSegmentAlignedPositionToWorldSpaceArea(level,
                                                    startConfiguration.PerLevelConfigurations[level], c.SegmentAlignedPosition);
                                                var lod = ETerrainUtils.HeightPyramidLevelToSurfaceTextureFlatLod(level);
                                                var packAndToken = cachedSurfacePatchProvider.ProvideSurfaceDetail(
                                                    repositioner.InvMove(surfaceWorldSpaceRectangle), lod).Result;
                                                var pack = packAndToken.Pack;
                                                if (pack != null)
                                                {
                                                    var mainTexture = pack.MainTexture;
                                                    segmentModificationManager.AddSegmentAsync(mainTexture, c.SegmentAlignedPosition);
                                                    cachedSurfacePatchProvider.RemoveSurfaceDetailAsync(pack, packAndToken.Token).Wait();
                                                }
                                        }
                                    },
                                    (c) => { },
                                    (c) => { })
                        };
                    },
            };
        }
    }
}
