using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.ComputeShaders;
using Assets.EProps;
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
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Ring2.Db;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.ShaderUtils;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.ShaderBuffers;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration.deos
{
    public class  ETerrainAsyncDEO : MonoBehaviour
    { 
        public GameObject Traveller;
        public FinalVegetationConfiguration VegetationConfiguration;

        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;

        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields = new GameInitializationFields();
        private FEConfiguration _configuration;
        private UpdaterUntilException _updaterUntilException = new UpdaterUntilException();

        private EPropElevationManager _elevationManager;
        private EPropHotAreaSelector _ePropHotAreaSelector;

        public void Start()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Start");
            _configuration = new FEConfiguration(new FilePathsConfiguration()) {Multithreading = false};
            _configuration.TerrainShapeDbConfiguration.UseTextureLoadingFromDisk = true;
            _configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk = true;
            _configuration.TerrainShapeDbConfiguration.MergeTerrainDetail = true;
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            VegetationConfiguration.FeConfiguration = _configuration;

            _gameInitializationFields = new GameInitializationFields();

            _ultraUpdatableContainer =
                ETerrainTestUtils.InitializeFinalElements(_configuration, containerGameObject, _gameInitializationFields,initializeLegacyDesignBodySpotUpdater:false);

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            startConfiguration.CommonConfiguration.YScale = _gameInitializationFields.Retrive<HeightDenormalizer>().DenormalizationMultiplier;
            startConfiguration.CommonConfiguration.InterSegmentMarginSize = 1/6.0f;
            startConfiguration.InitialTravellerPosition = new Vector2(440, 100) + new Vector2(90f*8, 90f*4);
            //startConfiguration.InitialTravellerPosition = new Vector2(0,0);
            startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() { HeightPyramidLevel.Top};//, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom};

            var buffersManager = new ETerrainHeightBuffersManager();
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
            
            var initializingHelper = new FEInitializingHelper(_gameInitializationFields,_ultraUpdatableContainer,_configuration);
            initializingHelper.InitializeGlobalInstancingContainer();

            var dbProxy = _gameInitializationFields.Retrive<TerrainShapeDbProxy>();

            _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>
                {
                    [EGroundTextureType.HeightMap] = GenerateAsyncHeightTextureEntitiesGeneratorFromTerrainShapeDb(
                        startConfiguration, dbProxy, repositioner, _gameInitializationFields.Retrive<UTTextureRendererProxy>()),
                    [EGroundTextureType.SurfaceTexture] = GenerateAsyncSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
                        _configuration,startConfiguration,_gameInitializationFields,_ultraUpdatableContainer,repositioner )
                }
            );

            initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject());
            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            Debug.Log("Init time "+msw.CollectResults());
        }

        private bool _wasFirstUpdateDone = false;

        public void Update()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("FIRST UPDATE");

            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new MockedFromGameObjectCameraForUpdate(Traveller)); });
            var position3D = Traveller.transform.position;
            var travellerFlatPosition = new Vector2(position3D.x, position3D.z);

             _eTerrainHeightPyramidFacade.Update(travellerFlatPosition);
            if (!_wasFirstUpdateDone)
            {
                Debug.Log("FIRST UPDATE RESULT " + msw.CollectResults());
                _wasFirstUpdateDone = true;
            }
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator GenerateAsyncHeightTextureEntitiesGeneratorFromTerrainShapeDb(
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, TerrainShapeDbProxy dbProxy, Repositioner repositioner,
            UTTextureRendererProxy textureRendererProxy, bool modifyCorners = true)
        {
            return new OneGroundTypeLevelTextureEntitiesGenerator
            {
                GeneratorFunc = (level) =>
                {
                    var ceilTexture =
                        EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize,
                            startConfiguration.CommonConfiguration.HeightTextureFormat);
                    var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture
                        , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                    var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                    var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("Height-" + level.ToString(),
                        new LambdaSegmentOrdersFillingExecutor(async (segmentAlignedPosition) =>
                            {
                                var surfaceWorldSpaceRectangle = ETerrainUtils.TerrainShapeSegmentAlignedPositionToWorldSpaceArea(level,
                                    startConfiguration.PerLevelConfigurations[level], segmentAlignedPosition);

                                var terrainDescriptionOutput = await dbProxy.Query(new TerrainDescriptionQuery()
                                {
                                    QueryArea = repositioner.InvMove(surfaceWorldSpaceRectangle),
                                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                                    {
                                        new TerrainDescriptionQueryElementDetail()
                                        {
                                            Resolution = ETerrainUtils.HeightPyramidLevelToTerrainShapeDatabaseResolution(level),
                                            RequiredMergeStatus = RequiredCornersMergeStatus.MERGED,
                                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                                        }
                                    }
                                });

                                var terrainDetailElementOutput = terrainDescriptionOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                                var segmentTexture = terrainDetailElementOutput.TokenizedElement.DetailElement.Texture.Texture;
                                await dbProxy.DisposeTerrainDetailElement(terrainDetailElementOutput.TokenizedElement.Token);
                                await segmentModificationManager.AddSegmentAsync(segmentTexture, segmentAlignedPosition);
                            }
                        ));

                    return new SegmentFillingListenerWithCeilTexture()
                    {
                        CeilTexture = ceilTexture,
                        SegmentFillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor)
                    };
                }
            };
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator GenerateAsyncSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
            FEConfiguration configuration, ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, GameInitializationFields gameInitializationFields
            , UltraUpdatableContainer ultraUpdatableContainer, Repositioner repositioner)
        {
            var surfaceTextureFormat = RenderTextureFormat.ARGB32;

            var feRing2PatchConfiguration = new FeRing2PatchConfiguration(configuration);
            feRing2PatchConfiguration.Ring2PlateStamperConfiguration.PlateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                [0] = 3f,
                [1] = 3 / 8f,
                [2] = 3 / 64f
            };
            feRing2PatchConfiguration.Ring2PatchesOverseerConfiguration_IntensityPatternPixelsPerUnit = new Dictionary<int, float>()
            {
                [0] = 1 / 3f,
                [1] = 1 / (3 * 8f),
                [2] = 1 / (3f * 64f)
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
            var patchStamperOverseerFinalizer = gameInitializationFields.Retrive<Ring2PatchStamplingOverseerFinalizer>();
            var surfacePatchProvider = new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer, mipmapExtractor, mipmapLevelToExtract);

            var commonExecutor = gameInitializationFields.Retrive<CommonExecutorUTProxy>();
            var cachedSurfacePatchProvider =
                new CachedESurfacePatchProvider(surfacePatchProvider
                    , new InMemoryAssetsCache<ESurfaceTexturesPackToken, NullableESurfaceTexturesPack>(
                        FETerrainShapeDbInitialization.CreateLevel2AssetsCache<ESurfaceTexturesPackToken, NullableESurfaceTexturesPack>(
                            cachingConfiguration: new CachingConfiguration()
                            {
                                SaveAssetsToFile = true,
                                UseFileCaching = true,
                            }
                            , new InMemoryCacheConfiguration() /*{ MaxTextureMemoryUsed = 0}*/
                            , new ESurfaceTexturesPackEntityActionsPerformer(commonExecutor)
                            , new ESurfaceTexturesPackFileManager(commonExecutor, configuration.FilePathsConfiguration.SurfacePatchCachePath))));
            cachedSurfacePatchProvider.Initialize().Wait();

            var textureRendererProxy = gameInitializationFields.Retrive<UTTextureRendererProxy>();

            return new OneGroundTypeLevelTextureEntitiesGenerator()
            {
                GeneratorFunc = (level) =>
                {
                    var ceilTexture =
                        EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize, surfaceTextureFormat);
                    var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture
                        , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                    var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                    var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("Height-" + level.ToString(),
                        new LambdaSegmentOrdersFillingExecutor(async (segmentAlignedPosition) =>
                        {
                            var surfaceWorldSpaceRectangle = ETerrainUtils.SurfaceTextureSegmentAlignedPositionToWorldSpaceArea(level,
                                startConfiguration.PerLevelConfigurations[level], segmentAlignedPosition);
                            var lod = ETerrainUtils.HeightPyramidLevelToSurfaceTextureFlatLod(level);
                            var packAndToken = await cachedSurfacePatchProvider.ProvideSurfaceDetail(repositioner.InvMove(surfaceWorldSpaceRectangle), lod);
                            var pack = packAndToken.Pack;
                            if (pack != null)
                            {
                                var mainTexture = pack.MainTexture;
                                await segmentModificationManager.AddSegmentAsync(mainTexture, segmentAlignedPosition);
                                await cachedSurfacePatchProvider.RemoveSurfaceDetailAsync(pack, packAndToken.Token);
                            }
                        }));
                    return new SegmentFillingListenerWithCeilTexture()
                    {
                        CeilTexture = ceilTexture,
                        SegmentFillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor)
                    };
                }
            };
        }
    }

    public class UnityThreadCompoundSegmentFillingListener : ISegmentFillingListener
    {
        private OtherThreadCompoundSegmentFillingOrdersExecutorProxy _executor;

        public UnityThreadCompoundSegmentFillingListener(OtherThreadCompoundSegmentFillingOrdersExecutorProxy executor)
        {
            _executor = executor;
        }

        public void AddSegment(SegmentInformation segmentInfo)
        {
            _executor.FillSegment(segmentInfo.SegmentAlignedPosition);
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
        }
    }

    public class OtherThreadCompoundSegmentFillingOrdersExecutorProxy :  BaseOtherThreadProxy
    {
        private ISegmentOrdersFillingExecutor _executor;

        public OtherThreadCompoundSegmentFillingOrdersExecutorProxy(string namePrefix, ISegmentOrdersFillingExecutor executor)
            : base($"{namePrefix} - OtherThreadCompoundSegmentFillingListenerProxy", false)
        {
            _executor = executor;
        }

        public void FillSegment(IntVector2 alignedPosition)
        {
            PostPureAsyncAction(() => _executor.FillSegmentAsync(alignedPosition));
        }
    }

    public interface ISegmentOrdersFillingExecutor
    {
        Task FillSegmentAsync(IntVector2 alignedPosition);
    } 

    public class LambdaSegmentOrdersFillingExecutor : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task> _segmentFillingFunc;

        public LambdaSegmentOrdersFillingExecutor(Func<IntVector2, Task> segmentFillingFunc)
        {
            _segmentFillingFunc = segmentFillingFunc;
        }

        public Task FillSegmentAsync(IntVector2 alignedPosition)
        {
            return _segmentFillingFunc(alignedPosition);
        }
    } 
}
