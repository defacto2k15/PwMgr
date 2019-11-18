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
                _movementCustodian = new TravellerMovementCustodian();
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

                _segmentsGenerationInspector = new InitialSegmentsGenerationInspector(() => Debug.Log("INITIAL CREATED"));
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
                Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            });

            Debug.Log("Init time "+msw.CollectResults());
        }

        private bool _wasFirstUpdateDone = false;

        public void Update()
        {
            Debug.Log("MOVEMENT POSSIBILITY "+_movementCustodian.IsMovementPossible());
            var msw = new MyStopWatch();
            msw.StartSegment("FIRST UPDATE");

            _segmentsGenerationInspector.Update();
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new MockedFromGameObjectCameraForUpdate(Traveller)); });
            var position3D = Traveller.transform.position;
            var travellerFlatPosition = new Vector2(position3D.x, position3D.z);

             _eTerrainHeightPyramidFacade.Update(travellerFlatPosition);
             int after = 22;
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
                    travellerCustodian.AddLimiter(() => fillingListener.AreRequiredSegmentsPresent());
                    initializationFields.Retrive<InitialSegmentsGenerationInspector>().SetConditionToCheck(() => fillingListener.AreRequiredSegmentsPresent());
                    return new SegmentFillingListenerWithCeilTexture()
                    {
                        CeilTexture = ceilTexture,
                        SegmentFillingListener = fillingListener
                    };
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
                GeneratorFunc = (level) =>
                {
                    var ceilTexture =
                        EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize, surfaceTextureFormat);
                    var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture
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
                                var pack = packAndToken.Pack;
                                if (pack != null)
                                {
                                    cachedSurfacePatchDbProxy.RemoveSurfaceDetailAsync(pack, packAndToken.Token);
                                }
                            }
                            ));


                    ultraUpdatableContainer.AddOtherThreadProxy(otherThreadExecutor);
                    var fillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor);
                    var travellerCustodian = gameInitializationFields.Retrive<TravellerMovementCustodian>();
                    travellerCustodian.AddLimiter(() => fillingListener.AreRequiredSegmentsPresent());
                    return new SegmentFillingListenerWithCeilTexture()
                    {
                        CeilTexture = ceilTexture,
                        SegmentFillingListener = fillingListener
                    };
                }
            };
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
                // arleady creation was ordered
            }
        }

        public bool AreRequiredSegmentsPresent()
        {
            if (!_tokensDict.Any(c => c.Value.ShouldBeFilled))
            {
                return false;
            }
            return _tokensDict.Select(c => c.Value).All(c =>
            {
                if (c.ShouldBeFilled)
                {
                    return c.Situation == SegmentGenerationProcessSituation.Filled;
                }
                else
                {
                    return true;
                }
            });
        }
    }

    public enum SegmentGenerationProcessSituation
    {
        BeforeStartOfCreation, DuringCreation, Created, DuringFilling, Filled
    }

    public class SegmentGenerationProcessToken
    {
        private MyConcurrentValue<SegmentGenerationProcessSituation> _situation;
        private MyConcurrentValue<bool> _shouldBeFilled;
        private MyConcurrentValue<bool> _shouldBeRemoved;

        public SegmentGenerationProcessToken(MyConcurrentValue<SegmentGenerationProcessSituation> situation, MyConcurrentValue<bool> shouldBeFilled, MyConcurrentValue<bool> shouldBeRemoved)
        {
            _situation = situation;
            _shouldBeFilled = shouldBeFilled;
            _shouldBeRemoved = shouldBeRemoved;
        }

        public SegmentGenerationProcessSituation Situation
        {
            get => _situation.Value;
            set => _situation.Value = value;
        }

        public bool ShouldBeFilled
        {
            get => _shouldBeFilled.Value;
            set => _shouldBeFilled.Value = value;
        }

        public bool ShouldBeRemoved
        {
            get => _shouldBeRemoved.Value;
            set => _shouldBeRemoved.Value = value;
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
            var token = new SegmentGenerationProcessToken(new MyConcurrentValue<SegmentGenerationProcessSituation>(), new MyConcurrentValue<bool>(),
                new MyConcurrentValue<bool>()) {Situation = SegmentGenerationProcessSituation.BeforeStartOfCreation};
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
                    Preconditions.Fail("Not expected State: " + token.Situation);
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
