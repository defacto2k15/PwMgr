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
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.SectorFilling;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
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
        public GraphicsOverlay Overlay;
        public bool Multithreading = false;
        public GameObject Traveller;
        public FinalVegetationConfiguration VegetationConfiguration;

        private TravellerMovementCustodian _movementCustodian;
        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;

        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields = new GameInitializationFields();
        private FEConfiguration _configuration;
        private UpdaterUntilException _updaterUntilException = new UpdaterUntilException();

        private EPropElevationManager _elevationManager;
        private EPropHotAreaSelector _ePropHotAreaSelector;
        private InitialSegmentsGenerationInspector _segmentsGenerationInspector;

        private bool _initializationWasSuccessfull;

        public void Start()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Start");
            TaskUtils.ExecuteActionWithOverridenMultithreading(true, () =>
            {
                _configuration = new FEConfiguration(new FilePathsConfiguration()) {Multithreading = Multithreading};
                _configuration.EngraveTerrainFeatures = true;

                _configuration.TerrainShapeDbConfiguration.UseTextureLoadingFromDisk = true;
                _configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk = true;
                _configuration.TerrainShapeDbConfiguration.MergeTerrainDetail = true;
                var containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
                VegetationConfiguration.FeConfiguration = _configuration;

                _gameInitializationFields = new GameInitializationFields();
                _movementCustodian = new TravellerMovementCustodian(Traveller);
                _gameInitializationFields.SetField(_movementCustodian);

                _ultraUpdatableContainer = ETerrainTestUtils.InitializeFinalElements(_configuration, containerGameObject, _gameInitializationFields, initializeLegacyDesignBodySpotUpdater: false);
                if (Overlay != null)
                {
                    Overlay.ServicesProfileInfo = _gameInitializationFields.Retrive<GlobalServicesProfileInfo>();
                }

                var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
                startConfiguration.GenerateInitialSegmentsDuringStart = false;
                startConfiguration.CommonConfiguration.YScale = _gameInitializationFields.Retrive<HeightDenormalizer>().DenormalizationMultiplier;
                startConfiguration.CommonConfiguration.InterSegmentMarginSize = 1 / 6.0f;
                startConfiguration.InitialTravellerPosition = new Vector2(440, 100) + new Vector2(90f * 8, 90f * 4);
                //startConfiguration.InitialTravellerPosition = new Vector2(0,0);
                startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() {HeightPyramidLevel.Top, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom};

                var buffersManager = new ETerrainHeightBuffersManager();
                _eTerrainHeightPyramidFacade = new ETerrainHeightPyramidFacade(buffersManager,
                    _gameInitializationFields.Retrive<MeshGeneratorUTProxy>(),
                    _gameInitializationFields.Retrive<UTTextureRendererProxy>(), startConfiguration);

                var perLevelTemplates = _eTerrainHeightPyramidFacade.GenerateLevelTemplates();
                var levels = startConfiguration.PerLevelConfigurations.Keys.Where(c => startConfiguration.HeightPyramidLevels.Contains(c));
                buffersManager.InitializeBuffers(levels.ToDictionary(c => c, c => new EPyramidShaderBuffersGeneratorPerRingInput()
                {
                    CeilTextureResolution = startConfiguration.CommonConfiguration.CeilTextureSize.X, //TODO i use only X, - works only for squares
                    HeightMergeRanges = perLevelTemplates[c].PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                    CeilSliceWorldSize =
                        startConfiguration.PerLevelConfigurations[c].CeilTextureWorldSize.x, // TODO works only for square pyramids - i use width
                    RingUvRanges = startConfiguration.CommonConfiguration.RingsUvRange
                }), startConfiguration.CommonConfiguration.MaxLevelsCount, startConfiguration.CommonConfiguration.MaxRingsPerLevelCount);

                var dbInitialization =
                    new FETerrainShapeDbInitialization(_ultraUpdatableContainer, _gameInitializationFields, _configuration, new FilePathsConfiguration());
                dbInitialization.Start();

                var initializingHelper = new FEInitializingHelper(_gameInitializationFields, _ultraUpdatableContainer, _configuration);
                initializingHelper.InitializeGlobalInstancingContainer();

                _segmentsGenerationInspector = new InitialSegmentsGenerationInspector(() => _initialTerrainCreatedSemaphore?.Set());
                _gameInitializationFields.SetField(_segmentsGenerationInspector);

                _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                    new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>
                    {
                        [EGroundTextureType.HeightMap] = GenerateAsyncHeightTextureEntitiesGeneratorFromTerrainShapeDb(
                            startConfiguration, _gameInitializationFields, _ultraUpdatableContainer),
                        [EGroundTextureType.SurfaceTexture] = GenerateAsyncSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
                            _configuration, startConfiguration, _gameInitializationFields, _ultraUpdatableContainer)
                    }
                );
                initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject());

                if (VegetationConfiguration.GenerateBigBushes || VegetationConfiguration.GenerateGrass || VegetationConfiguration.GenerateTrees)
                {
                    EPropElevationConfiguration ePropLocationConfiguration = new EPropElevationConfiguration();
                    var elevationBuffers = InitializeDesignBodySpotUpdater(startConfiguration, ePropLocationConfiguration
                        , _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(), buffersManager, perLevelTemplates);

                    _initialTerrainCreatedSemaphore = new TcsSemaphore();
                    _elevationManagerUpdateInputData = new MyAwaitableValue<EPropElevationManagerUpdateInputData>();
                    var spotUpdaterProxy = _gameInitializationFields.Retrive<DesignBodySpotUpdaterProxy>();

                    spotUpdaterProxy.PostAction(async () =>
                    {
                        await _initialTerrainCreatedSemaphore.Await();
                        var propsMsw = new MyStopWatch();
                        while (true)
                        {
                            //Debug.Log("RRT ErectingBarrier");
                            //await spotUpdaterProxy.ErectNewActionsBarrierAndWaitForSoleRemainingTask().Await();
                            //propsMsw.StartSegment("UpdateAsync");
                            //Debug.Log("RRT 0");
                            var updateInput = await _elevationManagerUpdateInputData.RetriveValue();
                            await _elevationManager.UpdateAsync(updateInput);
                            await Task.Delay(3000);

                            //propsMsw.StartSegment("RecalculateSectorsDivision");
                            //Debug.Log("RRT 1");
                            //await _elevationManager.RecalculateSectorsDivisionAsync(updateInput.TravellerFlatPosition);
                            //Debug.Log($"Update of localeRecalculation "+propsMsw.CollectResults());
                            //propsMsw = new MyStopWatch();

                            //spotUpdaterProxy.DismantleNewActionsBarrierAndWaitForQueuedActionsToStart();
                        }
                    });
                    _elevationManager.RecalculateSectorsDivisionAsync(startConfiguration.InitialTravellerPosition).Wait();

                    var commonUniforms = new UniformsPack();
                    commonUniforms.SetUniform("_ScopeLength", ePropLocationConfiguration.ScopeLength);

                    var reloader = FindObjectOfType<BufferReloaderRootGO>();
                    ComputeBuffersPack computeBuffersPack = new ComputeBuffersPack(reloader);
                    computeBuffersPack.SetBuffer("_EPropLocaleBuffer", elevationBuffers.EPropLocaleBuffer);
                    computeBuffersPack.SetBuffer("_EPropIdsBuffer", elevationBuffers.EPropIdsBuffer);

                    var finalVegetation = new FinalVegetation(_gameInitializationFields, _ultraUpdatableContainer, VegetationConfiguration
                        , new UniformsAndComputeBuffersPack(commonUniforms, computeBuffersPack));
                    finalVegetation.Start();
                }

                Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            });

            Debug.Log("Init time "+msw.CollectResults());
            _initializationWasSuccessfull = true;
        }

        private bool _wasFirstUpdateDone = false;
        private TcsSemaphore _initialTerrainCreatedSemaphore;
        private MyAwaitableValue<EPropElevationManagerUpdateInputData> _elevationManagerUpdateInputData;

        public void Update()
        {
            if (!_initializationWasSuccessfull)
            {
                return;
            }
            //Debug.Log("MOVEMENT POSSIBILITY "+_movementCustodian.IsMovementPossible());
            _movementCustodian.Update();
            Traveller.SetActive(_movementCustodian.IsMovementPossible());
            Overlay.SetMovementPossibilityDetails(_movementCustodian.ThisFrameBlockingProcesses);

            var msw = new MyStopWatch();
            msw.StartSegment("FIRST UPDATE");

            _segmentsGenerationInspector.Update();
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new MockedFromGameObjectCameraForUpdate(Traveller)); });
            var position3D = Traveller.transform.position;
            var travellerFlatPosition = new Vector2(position3D.x, position3D.z);

             _eTerrainHeightPyramidFacade.Update(travellerFlatPosition);

            if (Time.frameCount % 50 == 0)
            {
                if (VegetationConfiguration.GenerateBigBushes || VegetationConfiguration.GenerateGrass || VegetationConfiguration.GenerateTrees)
                {
                    var selectorWithParameters = EPropHotAreaSelectorWithParameters.Create(_ePropHotAreaSelector,
                        _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel, travellerFlatPosition);
                    _elevationManagerUpdateInputData.SetValue(new EPropElevationManagerUpdateInputData()
                    {
                        TravellerFlatPosition = travellerFlatPosition,
                        SelectorWithParameters = selectorWithParameters,
                        LevelCentersWorldSpace = _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel
                    });
                }
            }

            //if (!_wasFirstUpdateDone)
            //{
            //    Debug.Log("FIRST UPDATE RESULT " + msw.CollectResults());
            //    _wasFirstUpdateDone = true;
            //}
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator GenerateAsyncHeightTextureEntitiesGeneratorFromTerrainShapeDb(
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, GameInitializationFields initializationFields, UltraUpdatableContainer updatableContainer, bool modifyCorners = true)
        {
            //startConfiguration.CommonConfiguration.UseNormalTextures = false;
            var textureRendererProxy = initializationFields.Retrive<UTTextureRendererProxy>();
            var dbProxy = initializationFields.Retrive<TerrainShapeDbProxy>();
            var repositioner = initializationFields.Retrive<Repositioner>();

            return new OneGroundTypeLevelTextureEntitiesGenerator
            {
                CeilTextureArrayGenerator = () =>
                    {
                        var outList = new List<EGroundTexture>()
                        {
                            new EGroundTexture(
                                texture: EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize
                                    , startConfiguration.HeightPyramidLevels.Count, startConfiguration.CommonConfiguration.HeightTextureFormat),
                                textureType:EGroundTextureType.HeightMap
                                ),
                        };
                        if (startConfiguration.CommonConfiguration.UseNormalTextures)
                        {
                            outList.Add(
                                new EGroundTexture(
                                    texture: EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize
                                        , startConfiguration.HeightPyramidLevels.Count, startConfiguration.CommonConfiguration.NormalTextureFormat),
                                    textureType: EGroundTextureType.NormalTexture
                                )
                            );
                        }
                        return outList;
                    },
                SegmentFillingListenerGeneratorFunc = (level, ceilTextureArrays) =>
                {
                    var usedGroundTypes = new List<EGroundTextureType>() {EGroundTextureType.HeightMap};
                    if (startConfiguration.CommonConfiguration.UseNormalTextures)
                    {
                        usedGroundTypes.Add(EGroundTextureType.NormalTexture);
                    }
                    var segmentModificationManagers = usedGroundTypes.ToDictionary(groundType => groundType,
                        groundType =>
                        {
                            var groundTexture = ceilTextureArrays.First(c => c.TextureType == groundType);

                            var segmentsPlacer= new HeightSegmentPlacer(
                                textureRendererProxy, initializationFields.Retrive<CommonExecutorUTProxy>(), groundTexture.Texture
                                , level.GetIndex(), startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize
                                , startConfiguration.CommonConfiguration.InterSegmentMarginSize, startConfiguration.CommonConfiguration.SegmentTextureResolution
                                , false
                            );
                            var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                            return new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);
                        });

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("Height-" + level.ToString(), 
                        new  CompoundSegmentOrdersFillingExecutor<TerrainDescriptionOutput>(
                            async (sap) =>
                            {
                                var surfaceWorldSpaceRectangle = ETerrainUtils.TerrainShapeSegmentAlignedPositionToWorldSpaceArea(level,
                                    startConfiguration.PerLevelConfigurations[level],sap);

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
                                        },
                                        new TerrainDescriptionQueryElementDetail()
                                        {
                                            Resolution = ETerrainUtils.HeightPyramidLevelToTerrainShapeDatabaseResolution(level),
                                            RequiredMergeStatus = RequiredCornersMergeStatus.NOT_MERGED,
                                            Type = TerrainDescriptionElementTypeEnum.NORMAL_ARRAY
                                        },
                                    }
                                });

                                return terrainDescriptionOutput;
                            },
                            async (sap, terrainDescriptionOutput) =>
                            {
                                var heightSegmentTexture = terrainDescriptionOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
                                    .TokenizedElement.DetailElement.Texture.Texture;
                                await segmentModificationManagers[EGroundTextureType.HeightMap].AddSegmentAsync(heightSegmentTexture, sap);

                                if (startConfiguration.CommonConfiguration.UseNormalTextures)
                                {
                                    var normalSegmentTexture = terrainDescriptionOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
                                        .TokenizedElement.DetailElement.Texture.Texture;
                                    await segmentModificationManagers[EGroundTextureType.NormalTexture].AddSegmentAsync(normalSegmentTexture, sap);
                                }
                            },
                            async (terrainDescriptionOutput) =>
                            {
                                await dbProxy.DisposeTerrainDetailElement(terrainDescriptionOutput
                                    .GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.Token);
                                if (startConfiguration.CommonConfiguration.UseNormalTextures)
                                {
                                    await dbProxy.DisposeTerrainDetailElement(terrainDescriptionOutput
                                        .GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY).TokenizedElement.Token);
                                }
                            }
                        ));
                    updatableContainer.AddOtherThreadProxy(otherThreadExecutor);

                    var fillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor);
                    var travellerCustodian = initializationFields.Retrive<TravellerMovementCustodian>();
                    travellerCustodian.AddLimiter(() => new MovementBlockingProcess(){ProcessName = "HeightSegmentsGenerationProcess "+level, BlockCount = fillingListener.BlockingProcessesCount()});
                    initializationFields.Retrive<InitialSegmentsGenerationInspector>().SetConditionToCheck(() => fillingListener.BlockingProcessesCount() == 0);
                    return fillingListener;
                }
            };
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator GenerateAsyncSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
            FEConfiguration configuration, ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, GameInitializationFields gameInitializationFields
            , UltraUpdatableContainer ultraUpdatableContainer)
        {
            var repositioner = gameInitializationFields.Retrive<Repositioner>();
            var surfaceTextureFormat = RenderTextureFormat.ARGB32;
            var commonExecutor = gameInitializationFields.Retrive<CommonExecutorUTProxy>();

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
            var surfacePatchProvider = new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer, commonExecutor,
                mipmapExtractor, mipmapLevelToExtract);

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

            var cachedSurfacePatchDbProxy = new ESurfacePatchDbProxy(cachedSurfacePatchProvider);
            ultraUpdatableContainer.AddOtherThreadProxy(cachedSurfacePatchDbProxy);

            var textureRendererProxy = gameInitializationFields.Retrive<UTTextureRendererProxy>();

            return new OneGroundTypeLevelTextureEntitiesGenerator()
            {
                CeilTextureArrayGenerator =  () =>
                {
                    return new List<EGroundTexture>()
                    {
                        new EGroundTexture( EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize,
                            startConfiguration.HeightPyramidLevels.Count, surfaceTextureFormat),
                        EGroundTextureType.SurfaceTexture )
                    };
                },
                SegmentFillingListenerGeneratorFunc = (level, ceilTextureArrays) =>
                {
                    var ceilTextureArray = ceilTextureArrays.First(c => c.TextureType == EGroundTextureType.SurfaceTexture);
                    var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTextureArray.Texture, level.GetIndex()
                        , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                    var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                    var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("ESurface-" + level.ToString(),
                        new CompoundSegmentOrdersFillingExecutor<TokenizedESurfaceTexturesPackToken>(
                            async (sap) =>
                            {
                                var surfaceWorldSpaceRectangle = ETerrainUtils.SurfaceTextureSegmentAlignedPositionToWorldSpaceArea(level,
                                    startConfiguration.PerLevelConfigurations[level], sap);
                                var lod = ETerrainUtils.HeightPyramidLevelToSurfaceTextureFlatLod(level);
                                return await cachedSurfacePatchDbProxy.ProvideSurfaceDetail(repositioner.InvMove(surfaceWorldSpaceRectangle), lod);
                            },
                            async (sap, packAndToken) =>
                            {
                                var pack = packAndToken.Pack;
                                if (pack != null)
                                {
                                    var mainTexture = pack.MainTexture;
                                    await segmentModificationManager.AddSegmentAsync(mainTexture, sap);
                                }
                            },
                            segmentRemovalFunc: async (packAndToken) =>
                            {
                                if (packAndToken != null)
                                {
                                    var pack = packAndToken.Pack;
                                    if (pack != null)
                                    {
                                        Preconditions.Assert(packAndToken.Token != null, "Token is null. Unexpected");
                                        cachedSurfacePatchDbProxy.RemoveSurfaceDetailAsync(pack, packAndToken.Token);
                                    }
                                }
                            }
                            ));

                    ultraUpdatableContainer.AddOtherThreadProxy(otherThreadExecutor);
                    var fillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor);
                    var travellerCustodian = gameInitializationFields.Retrive<TravellerMovementCustodian>();
                    travellerCustodian.AddLimiter(() => new MovementBlockingProcess() { BlockCount = fillingListener.BlockingProcessesCount(), ProcessName = "SurfaceSegmentsGeneration " + level });
                    return fillingListener;
                }
            };
        }

        public EPropLocaleBufferManagerInitializedBuffers InitializeDesignBodySpotUpdater(ETerrainHeightPyramidFacadeStartConfiguration startConfiguration,
            EPropElevationConfiguration ePropLocationConfiguration,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject, ETerrainHeightBuffersManager buffersManager,
            Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplate> perLevelTemplates)
        {
            var ePropConstantPyramidParameters = new EPropConstantPyramidParameters()
            {
                LevelsCount = startConfiguration.HeightPyramidLevels.Count,
                RingsPerLevelCount = startConfiguration.CommonConfiguration.MaxRingsPerLevelCount, 
                HeightScale = startConfiguration.CommonConfiguration.YScale
            };
            _elevationManager = new EPropElevationManager(_gameInitializationFields.Retrive<CommonExecutorUTProxy>()  , shaderExecutorObject,ePropLocationConfiguration,ePropConstantPyramidParameters);
            var heightCeilTextureArray = _eTerrainHeightPyramidFacade.CeilTextureArrays.Where(c => c.TextureType == EGroundTextureType.HeightMap).Select(c => c.Texture).First();
            var elevationBuffers = _elevationManager.Initialize(buffersManager.PyramidPerFrameParametersBuffer, buffersManager.EPyramidConfigurationBuffer,
                heightCeilTextureArray);

            var ceilTextureWorldSizes = startConfiguration.PerLevelConfigurations.ToDictionary(c=>c.Key, c=> c.Value.CeilTextureWorldSize);
            var ringMergeRanges = perLevelTemplates.ToDictionary(c => c.Key,
                c => c.Value.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange));
            _ePropHotAreaSelector = new EPropHotAreaSelector(ceilTextureWorldSizes, ringMergeRanges);

            var spotUpdater = new EPropsDesignBodyChangesListener(_elevationManager, VegetationConfiguration.VegetationRepositioner ); // todo get repositioner from other place
            var designBodySpotUpdaterProxy = new DesignBodySpotUpdaterProxy(spotUpdater);
            _gameInitializationFields.SetField(designBodySpotUpdaterProxy);
            _ultraUpdatableContainer.AddOtherThreadProxy(designBodySpotUpdaterProxy);

            var rootMediator = new RootMediatorSpotPositionsUpdater();
            spotUpdater.SetChangesListener(rootMediator);
            _gameInitializationFields.SetField(rootMediator);

            return elevationBuffers;
        }
    }

    public class UnityThreadDummySegmentFillingListener : ISegmentFillingListener
    {
        private OtherThreadCompoundSegmentFillingOrdersExecutorProxy _executor;

        public UnityThreadDummySegmentFillingListener(OtherThreadCompoundSegmentFillingOrdersExecutorProxy executor)
        {
            _executor = executor;
        }

        public void AddSegment(SegmentInformation segmentInfo)
        {
            var token = new SegmentGenerationProcessToken(SegmentGenerationProcessSituation.BeforeStartOfCreation, RequiredSegmentSituation.Filled);
            _executor.ExecuteSegmentAction(token,segmentInfo.SegmentAlignedPosition);
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
        }
    }

    public class UnityThreadCompoundSegmentFillingListener : ISegmentFillingListener
    {
        private OtherThreadCompoundSegmentFillingOrdersExecutorProxy _executor;
        private Dictionary<IntVector2, SegmentGenerationProcessToken> _tokensDict;

        public UnityThreadCompoundSegmentFillingListener(OtherThreadCompoundSegmentFillingOrdersExecutorProxy executor)
        {
            _executor = executor;
            _tokensDict = new Dictionary<IntVector2, SegmentGenerationProcessToken>();
        }

        public void AddSegment(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(!_tokensDict.ContainsKey(sap), $"There arleady is segment of sap {sap}");

            RequiredSegmentSituation requiredSituation;
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                requiredSituation = RequiredSegmentSituation.Filled;
            }
            else
            {
                requiredSituation = RequiredSegmentSituation.Created;
            }
            var newToken = new SegmentGenerationProcessToken(SegmentGenerationProcessSituation.BeforeStartOfCreation,requiredSituation);
            _tokensDict[sap] = newToken;
            _executor.ExecuteSegmentAction(newToken, sap);
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(_tokensDict.ContainsKey(sap),"Cannot remove segment, as it was never present in dict "+segmentInfo.SegmentAlignedPosition);
            var token = _tokensDict[sap];
            token.RequiredSituation = RequiredSegmentSituation.Removed;
            _executor.ExecuteSegmentAction(token, sap);
            _tokensDict.Remove(sap);
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(_tokensDict.ContainsKey(sap), "During segmentStateChange to Active there is no tokens in dict");
            var token = _tokensDict[sap];
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                token.RequiredSituation = RequiredSegmentSituation.Filled;
                _executor.ExecuteSegmentAction(token, sap);
            }
            else
            {
                token.RequiredSituation = RequiredSegmentSituation.Created;
            }
        }

        public int BlockingProcessesCount()
        {
            if (!_tokensDict.Any())
            {
                return 0;
            }

            return _tokensDict.Select(c => c.Value).Sum(c =>
            {
                if (c.RequiredSituation == RequiredSegmentSituation.Filled)
                {
                    if (c.CurrentSituation == SegmentGenerationProcessSituation.Filled)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }

                return 0;
            });
        }
    }

    public enum SegmentGenerationProcessSituation
    {
        BeforeStartOfCreation, DuringCreation, Created, DuringFilling, Filled
    }

    public enum RequiredSegmentSituation
    {
         Created, Filled, Removed
    }

    public class SegmentGenerationProcessToken
    {
        private volatile  SegmentGenerationProcessSituation _currentSituation;
        private volatile  RequiredSegmentSituation _requiredSituation;

        public SegmentGenerationProcessToken(SegmentGenerationProcessSituation currentSituation, RequiredSegmentSituation requiredSituation)
        {
            _currentSituation = currentSituation;
            _requiredSituation = requiredSituation;
        }

        public SegmentGenerationProcessSituation CurrentSituation
        {
            get => _currentSituation;
            set => _currentSituation = value;
        }

        public RequiredSegmentSituation RequiredSituation
        {
            get => _requiredSituation;
            set => _requiredSituation = value;
        }

        public bool ProcessIsOngoing =>
            (_currentSituation == SegmentGenerationProcessSituation.DuringCreation ||
             _currentSituation == SegmentGenerationProcessSituation.DuringCreation ||
             _currentSituation == SegmentGenerationProcessSituation.DuringFilling);
    }


    public class OtherThreadCompoundSegmentFillingOrdersExecutorProxy :  BaseOtherThreadProxy
    {
        private ISegmentOrdersFillingExecutor _executor;

        public OtherThreadCompoundSegmentFillingOrdersExecutorProxy(string namePrefix, ISegmentOrdersFillingExecutor executor)
            : base($"{namePrefix} - OtherThreadCompoundSegmentFillingListenerProxy", false)
        {
            _executor = executor;
        }

        public void ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            PostPureAsyncAction(() => _executor.ExecuteSegmentAction(token, sap));
        }
    }

    public interface ISegmentOrdersFillingExecutor
    {
        Task ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap);
    } 

    public class LambdaSegmentOrdersFillingExecutor : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task> _segmentFillingFunc;

        public Task ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            if (token.RequiredSituation == RequiredSegmentSituation.Filled || token.RequiredSituation == RequiredSegmentSituation.Created)
            {
                return _segmentFillingFunc(sap);
            }
            else
            {
                return TaskUtils.EmptyCompleted();
            }
        }
    } 

    public class CompoundSegmentOrdersFillingExecutor<T> : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task<T>> _segmentGeneratingFunc;
        private Func<IntVector2,T, Task> _segmentFillingFunc;
        private Func<T, Task> _segmentRemovalFunc;
        private Dictionary<IntVector2,T > _currentlyCreatedSegments;

        public CompoundSegmentOrdersFillingExecutor(Func<IntVector2, Task<T>> segmentGeneratingFunc, Func<IntVector2, T, Task> segmentFillingFunc, Func<T, Task> segmentRemovalFunc)
        {
            _segmentGeneratingFunc = segmentGeneratingFunc;
            _segmentFillingFunc = segmentFillingFunc;
            _segmentRemovalFunc = segmentRemovalFunc;
            _currentlyCreatedSegments = new Dictionary<IntVector2, T>();
        }

        private async Task<bool> SegmentProcessOneLoop(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            switch (token.CurrentSituation)
            {
                case SegmentGenerationProcessSituation.BeforeStartOfCreation:
                    switch (token.RequiredSituation)
                    {
                        case RequiredSegmentSituation.Created:
                            await GenerateSegment(token, sap);
                            return true;
                        case RequiredSegmentSituation.Filled:
                            if (!_currentlyCreatedSegments.ContainsKey(sap))
                            {
                                await GenerateSegment(token, sap);
                            }
                            else
                            {
                                await FillSegment(token, sap);
                            }
                            return true;
                        case RequiredSegmentSituation.Removed:
                            await RemoveSegment(token, sap);
                            return false;
                        default:
                            Preconditions.Fail("Unexpected required situation " + token.RequiredSituation);
                            return false;
                    }
                case SegmentGenerationProcessSituation.Created:
                    switch (token.RequiredSituation)
                    {
                        case RequiredSegmentSituation.Created:
                            return false;
                        case RequiredSegmentSituation.Filled:
                            await FillSegment(token, sap);
                            return true;
                        case RequiredSegmentSituation.Removed:
                            await RemoveSegment(token, sap);
                            return false;
                        default:
                            Preconditions.Fail("Unexpected required situation " + token.RequiredSituation);
                            return false;
                    }
                case SegmentGenerationProcessSituation.Filled:
                    switch (token.RequiredSituation)
                    {
                        case RequiredSegmentSituation.Created:
                            return false;
                        case RequiredSegmentSituation.Filled:
                            return false;
                        case RequiredSegmentSituation.Removed:
                            await RemoveSegment(token, sap);
                            return false;
                        default:
                            Preconditions.Fail("Unexpected required situation " + token.RequiredSituation);
                            return false;
                    }
                default:
                    Preconditions.Fail("Unexpected situation " + token.CurrentSituation);
                    return false;
            }
        }

        public async Task ExecuteSegmentAction(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            if (!token.ProcessIsOngoing) { 
                bool shouldContinue = true;
                while (shouldContinue)
                {
                    shouldContinue = await SegmentProcessOneLoop(token, sap);
                }
            }
        }

        private async Task RemoveSegment(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            Preconditions.Assert(!token.ProcessIsOngoing, "Cannot remove while process in ongoing");
            Preconditions.Assert(token.RequiredSituation == RequiredSegmentSituation.Removed, "Required situation in not removed but "+token.RequiredSituation);
            if (_currentlyCreatedSegments.ContainsKey(sap))
            {
                var segment = _currentlyCreatedSegments[sap];
                _currentlyCreatedSegments.Remove(sap);
                await _segmentRemovalFunc(segment);
            }
        }

        private async Task FillSegment(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            Preconditions.Assert(token.CurrentSituation == SegmentGenerationProcessSituation.Created, "Unexpected situaton "+token.CurrentSituation);
            token.CurrentSituation = SegmentGenerationProcessSituation.DuringFilling;
            await _segmentFillingFunc(sap, _currentlyCreatedSegments[sap]);
            token.CurrentSituation = SegmentGenerationProcessSituation.Filled;
        }

        private async Task GenerateSegment(SegmentGenerationProcessToken token, IntVector2 sap)
        {
            Preconditions.Assert(token.CurrentSituation == SegmentGenerationProcessSituation.BeforeStartOfCreation, "Unexpected situaton "+token.CurrentSituation);
            if (!_currentlyCreatedSegments.ContainsKey(sap))
            {
                token.CurrentSituation = SegmentGenerationProcessSituation.DuringCreation;
                var newSegment = await _segmentGeneratingFunc(sap);
                _currentlyCreatedSegments[sap] = newSegment;
            }
            token.CurrentSituation = SegmentGenerationProcessSituation.Created;
        }
    }

    public class SegmentWithToken<T>
    {
        public T Segment;
        public SegmentGenerationProcessToken Token;
    }
}
