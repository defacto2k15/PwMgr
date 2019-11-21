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
                    HeightMergeRanges = perLevelTemplates[c].LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                    PyramidLevelWorldSize =
                        startConfiguration.PerLevelConfigurations[c].PyramidLevelWorldSize.Width, // TODO works only for square pyramids - i use width
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
            var textureRendererProxy = initializationFields.Retrive<UTTextureRendererProxy>();
            var dbProxy = initializationFields.Retrive<TerrainShapeDbProxy>();
            var repositioner = initializationFields.Retrive<Repositioner>();

            return new OneGroundTypeLevelTextureEntitiesGenerator
            {
                CeilTextureArrayGenerator = () =>
                    {
                        return EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize,
                            startConfiguration.HeightPyramidLevels.Count, startConfiguration.CommonConfiguration.HeightTextureFormat);
                    },
                SegmentFillingListenerGeneratorFunc = (level, ceilTextureArray) =>
                {
                    var segmentsPlacer = new HeightSegmentPlacer(
                        textureRendererProxy, initializationFields.Retrive<CommonExecutorUTProxy>(), ceilTextureArray
                        , level.GetIndex(), startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize
                        , startConfiguration.CommonConfiguration.InterSegmentMarginSize, startConfiguration.CommonConfiguration.SegmentTextureResolution
                        , startConfiguration.CommonConfiguration.ModifyCornersInHeightSegmentPlacer 
                        );
                    var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                    var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("Height-" + level.ToString(), 
                        new  CompoundSegmentOrdersFillingExecutor<TerrainDetailElementOutput>(
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
                                        }
                                    }
                                });

                                var terrainDetailElementOutput = terrainDescriptionOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                                return terrainDetailElementOutput;
                            },
                            (sap, terrainDetailElementOutput) =>
                            {
                                var segmentTexture = terrainDetailElementOutput.TokenizedElement.DetailElement.Texture.Texture;
                                return segmentModificationManager.AddSegmentAsync(segmentTexture, sap);
                            },
                            (terrainDetailElementOutput) =>
                            {
                                return dbProxy.DisposeTerrainDetailElement(terrainDetailElementOutput.TokenizedElement.Token);
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
                        return EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize,
                            startConfiguration.HeightPyramidLevels.Count, surfaceTextureFormat);
                    },
                SegmentFillingListenerGeneratorFunc = (level, ceilTextureArray) =>
                {
                    var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTextureArray, level.GetIndex()
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
            Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplateWithShapeConfiguration> perLevelTemplates)
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

            var levelWorldSizes = startConfiguration.PerLevelConfigurations.ToDictionary(c=>c.Key, c=>c.Value.PyramidLevelWorldSize.Size);
            var ringMergeRanges = perLevelTemplates.ToDictionary(c => c.Key,
                c => c.Value.LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange));
            _ePropHotAreaSelector = new EPropHotAreaSelector(levelWorldSizes, ringMergeRanges);

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
            var token = _executor.CreateSegmentAsync(segmentInfo.SegmentAlignedPosition);
            _executor.FillSegmentWhenReady(token,segmentInfo.SegmentAlignedPosition);
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
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                _tokensDict[sap] = _executor.CreateSegmentAsync(sap);
                _executor.FillSegmentWhenReady(_tokensDict[sap], sap);
            }
            else
            {
                _tokensDict[sap] = _executor.CreateSegmentAsync(sap);
            }
        }

        public void RemoveSegment(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            Preconditions.Assert(_tokensDict.ContainsKey(sap),"Cannot remove segment, as it was never present in dict "+segmentInfo.SegmentAlignedPosition);
            _executor.RemoveSegment(_tokensDict[sap], sap);
            _tokensDict.Remove(sap);
        }

        public void SegmentStateChange(SegmentInformation segmentInfo)
        {
            var sap = segmentInfo.SegmentAlignedPosition;
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                Preconditions.Assert(_tokensDict.ContainsKey(sap),
                    "During segmentStateChange to Active there is no tokens in dict");
                _executor.FillSegmentWhenReady(_tokensDict[sap], sap);
            }
            else
            {
                _executor.CancelFillingRequirement(_tokensDict[sap]);
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
                if (c.ShouldBeFilled)
                {
                    if (c.Situation == SegmentGenerationProcessSituation.Filled)
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

    public class SegmentGenerationProcessToken
    {
        private volatile  SegmentGenerationProcessSituation _situation;
        private volatile bool _shouldBeFilled;
        private volatile bool _shouldBeRemoved;

        public SegmentGenerationProcessToken(SegmentGenerationProcessSituation situation, bool shouldBeFilled, bool shouldBeRemoved)
        {
            _situation = situation;
            _shouldBeFilled = shouldBeFilled;
            _shouldBeRemoved = shouldBeRemoved;
        }

        public SegmentGenerationProcessSituation Situation
        {
            get => _situation;
            set => _situation = value;
        }

        public bool ShouldBeFilled
        {
            get => _shouldBeFilled;
            set => _shouldBeFilled= value;
        }

        public bool ShouldBeRemoved
        {
            get => _shouldBeRemoved;
            set => _shouldBeRemoved= value;
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

        public SegmentGenerationProcessToken CreateSegmentAsync( IntVector2 alignedPosition)
        {
            var token = new SegmentGenerationProcessToken(SegmentGenerationProcessSituation.BeforeStartOfCreation, false, false);
            PostPureAsyncAction(() => _executor.CreateSegmentAsync(token, alignedPosition));
            return token;
        }

        public void FillSegmentWhenReady(SegmentGenerationProcessToken token, IntVector2 alignedPosition)
        {
            token.ShouldBeFilled = true;
            PostPureAsyncAction(() => _executor.FillSegmentWhenReady(alignedPosition));
        }

        public void RemoveSegment(SegmentGenerationProcessToken token, IntVector2 alignedPosition)
        {
            token.ShouldBeRemoved = true;
            PostPureAsyncAction(() => _executor.RemoveSegmentAsync(alignedPosition));
        }

        public void CancelFillingRequirement(SegmentGenerationProcessToken token)
        {
            Preconditions.Assert(token.ShouldBeFilled, "There is not filling requirement");
            token.ShouldBeFilled = false;
            if (token.Situation == SegmentGenerationProcessSituation.Filled)
            {
                token.Situation = SegmentGenerationProcessSituation.Created;
            }
        }
    }

    public interface ISegmentOrdersFillingExecutor
    {
        Task CreateSegmentAsync(SegmentGenerationProcessToken  token, IntVector2 alignedPosition);
        Task FillSegmentWhenReady(IntVector2 alignedPosition);
        Task RemoveSegmentAsync(IntVector2 alignedPosition);
    } 

    public class LambdaSegmentOrdersFillingExecutor : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task> _segmentFillingFunc;

        public LambdaSegmentOrdersFillingExecutor(Func<IntVector2, Task> segmentFillingFunc)
        {
            _segmentFillingFunc = segmentFillingFunc;
        }

        public Task CreateSegmentAsync( SegmentGenerationProcessToken  token, IntVector2 alignedPosition)
        {
            return _segmentFillingFunc(alignedPosition); //automatic filling, this is dummy 
        }

        public Task FillSegmentWhenReady(IntVector2 alignedPosition)
        {
            throw new NotImplementedException();
        }

        public Task RemoveSegmentAsync(IntVector2 alignedPosition)
        {
            throw new NotImplementedException();
        }
    } 

    public class CompoundSegmentOrdersFillingExecutor<T> : ISegmentOrdersFillingExecutor
    {
        private Func<IntVector2, Task<T>> _segmentGeneratingFunc;
        private Func<IntVector2,T, Task> _segmentFillingFunc;
        private Func<T, Task> _segmentRemovalFunc;
        private Dictionary<IntVector2, SegmentWithToken<T>> _segmentsDict;

        public CompoundSegmentOrdersFillingExecutor(Func<IntVector2, Task<T>> segmentGeneratingFunc, Func<IntVector2, T, Task> segmentFillingFunc, Func<T, Task> segmentRemovalFunc)
        {
            _segmentGeneratingFunc = segmentGeneratingFunc;
            _segmentFillingFunc = segmentFillingFunc;
            _segmentRemovalFunc = segmentRemovalFunc;
            _segmentsDict = new Dictionary<IntVector2, SegmentWithToken<T>>();
        }

        public async Task CreateSegmentAsync( SegmentGenerationProcessToken token, IntVector2 alignedPosition)
        {
            Preconditions.Assert(!_segmentsDict.ContainsKey(alignedPosition), "There arleady is segment of position "+alignedPosition);
            _segmentsDict[alignedPosition] = new SegmentWithToken<T>()
            {
                Token = token
            };
            token.Situation = SegmentGenerationProcessSituation.DuringCreation;
            var segment = await _segmentGeneratingFunc(alignedPosition);

            if (token.ShouldBeRemoved)
            {
                await RemoveInternal(alignedPosition);
                return;
            }

            switch (token.Situation)
            {
                case SegmentGenerationProcessSituation.BeforeStartOfCreation:
                    Preconditions.Fail("Not expected State: "+token.Situation);
                    return;
                case SegmentGenerationProcessSituation.DuringCreation:
                    break; //normal situation
                case SegmentGenerationProcessSituation.Created:
                    Preconditions.Fail("Not expected State: " + token.Situation);
                    return;
                case SegmentGenerationProcessSituation.DuringFilling:
                    Preconditions.Fail("Not expected State: " + token.Situation);
                    return;
                case SegmentGenerationProcessSituation.Filled:
                    Preconditions.Fail("Not expected State: " + token.Situation);
                    return;
            }

            _segmentsDict[alignedPosition].Segment = segment;
            token.Situation = SegmentGenerationProcessSituation.Created;

            if (token.ShouldBeFilled)
            {
                await Fill(token, alignedPosition, segment);
            }
        }

        private async Task Fill(SegmentGenerationProcessToken token, IntVector2 alignedPosition, T segment)
        {
            token.Situation = SegmentGenerationProcessSituation.DuringFilling;
            await _segmentFillingFunc(alignedPosition, segment);
            token.Situation = SegmentGenerationProcessSituation.Filled;
        }

        public async Task FillSegmentWhenReady(IntVector2 alignedPosition)
        {
            Preconditions.Assert( _segmentsDict.ContainsKey(alignedPosition) ,"Segment of position "+alignedPosition+" is not present nor it is created");
            var token = _segmentsDict[alignedPosition].Token;
            if (token.ShouldBeRemoved)
            {
                await RemoveInternal(alignedPosition);
                return;
            }

            if (!token.ShouldBeFilled)
            {
                return;
            }

            switch (token.Situation)
            {
                case SegmentGenerationProcessSituation.BeforeStartOfCreation:
                    Preconditions.Fail("Not expected State: "+token.Situation);
                    return;
                case SegmentGenerationProcessSituation.DuringCreation:
                    token.ShouldBeFilled = true;
                    return;
                case SegmentGenerationProcessSituation.Created:
                    await Fill(token, alignedPosition, _segmentsDict[alignedPosition].Segment);
                    return;
                case SegmentGenerationProcessSituation.DuringFilling:
                    //Preconditions.Fail("Not expected State: " + token.Situation);
                    Debug.Log("Not expected State: " + token.Situation);
                    return;
                case SegmentGenerationProcessSituation.Filled:
                    Preconditions.Fail("Not expected State: " + token.Situation);
                    return;
            }
        }

        public async Task RemoveSegmentAsync(IntVector2 alignedPosition)
        {
            Preconditions.Assert( _segmentsDict.ContainsKey(alignedPosition) ,"Segment of position "+alignedPosition+" is not present nor it is created");
            var token = _segmentsDict[alignedPosition].Token;
            Preconditions.Assert(token.ShouldBeRemoved, "Token is not marked as should-be-removed");
            switch (token.Situation)
            {
                case SegmentGenerationProcessSituation.BeforeStartOfCreation:
                    Preconditions.Fail("Not expected State: "+token.Situation);
                    return;
                case SegmentGenerationProcessSituation.DuringCreation:
                    return;
                case SegmentGenerationProcessSituation.Created:
                    _segmentsDict.Remove(alignedPosition);
                    return;
                case SegmentGenerationProcessSituation.DuringFilling:
                    return;
                case SegmentGenerationProcessSituation.Filled:
                    _segmentsDict.Remove(alignedPosition);
                    return;
            }

            await RemoveInternal(alignedPosition);
        }

        private async Task RemoveInternal(IntVector2 alignedPosition)
        {
            var segment = _segmentsDict[alignedPosition].Segment;
            _segmentsDict.Remove(alignedPosition);
            await _segmentRemovalFunc(segment);
        }
    }

    public class SegmentWithToken<T>
    {
        public T Segment;
        public SegmentGenerationProcessToken Token;
    }
}
