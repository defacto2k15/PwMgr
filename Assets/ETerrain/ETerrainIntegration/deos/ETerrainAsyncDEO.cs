using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.ComputeShaders;
using Assets.EProps;
using Assets.ESurface;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.ShaderUtils;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.ShaderBuffers;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain.SectorFilling
{
}

namespace Assets.ETerrain.ETerrainIntegration.deos
{
    public class  ETerrainAsyncDEO : MonoBehaviour
    {
        public GraphicsOverlay Overlay;
        public MultipleLevelsHeightPyramidExplorerGO HeightPyramidExplorer;
        public bool Multithreading = false;
        public GameObject Traveller;
        public FinalVegetationConfiguration VegetationConfiguration;

        private TravellerMovementCustodian _movementCustodian;
        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;

        private UltraUpdatableContainer _ultraUpdatableContainer;
        private GameInitializationFields _gameInitializationFields;
        private FEConfiguration _feConfiguration;
        private UpdaterUntilException _updaterUntilException;

        private HeightmapSegmentFillingListenersContainer _heightmapListenersContainer;

        private bool _initializationWasSuccessfull;

        public void Start()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("Start");
            TaskUtils.ExecuteActionWithOverridenMultithreading(true, () =>
            {
                _feConfiguration = new FEConfiguration(new FilePathsConfiguration()) {Multithreading = Multithreading};
                _feConfiguration.EngraveTerrainFeatures = true;
                _feConfiguration.EngraveRoadsInTerrain = true;

                _feConfiguration.TerrainShapeDbConfiguration.UseTextureLoadingFromDisk = true;
                _feConfiguration.TerrainShapeDbConfiguration.UseTextureSavingToDisk = false;
                _feConfiguration.TerrainShapeDbConfiguration.MergeTerrainDetail = true;

                var containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
                VegetationConfiguration.FeConfiguration = _feConfiguration;

                _heightmapListenersContainer = new HeightmapSegmentFillingListenersContainer();
                _gameInitializationFields = new GameInitializationFields();
                _updaterUntilException = new UpdaterUntilException();
                _movementCustodian = new TravellerMovementCustodian(Traveller);
                _gameInitializationFields.SetField(_movementCustodian);


                _ultraUpdatableContainer = ETerrainTestUtils.InitializeFinalElements(_feConfiguration, containerGameObject, _gameInitializationFields, initializeLegacyDesignBodySpotUpdater: false);

                var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
                var initializingHelper = InitializeETerrain(startConfiguration);
                initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject());
                InitializeUI(startConfiguration);

                Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, Traveller.transform.position.y, startConfiguration.InitialTravellerPosition.y);
            });

            Debug.Log("Init time "+msw.CollectResults());
            _initializationWasSuccessfull = true;
        }

        private FEInitializingHelper InitializeETerrain(ETerrainHeightPyramidFacadeStartConfiguration startConfiguration)
        {
            startConfiguration.CommonConfiguration.YScale = _gameInitializationFields.Retrive<HeightDenormalizer>().DenormalizationMultiplier;
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
                new FETerrainShapeDbInitialization(_ultraUpdatableContainer, _gameInitializationFields, _feConfiguration, new FilePathsConfiguration());
            dbInitialization.Start();

            var initializingHelper = new FEInitializingHelper(_gameInitializationFields, _ultraUpdatableContainer, _feConfiguration);
            initializingHelper.InitializeGlobalInstancingContainer();


            _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>
                {
                    [EGroundTextureType.HeightMap] = ETerrainInitializationHelper.CreateHeightTextureEntitiesGenerator(
                        startConfiguration, _gameInitializationFields, _ultraUpdatableContainer, _heightmapListenersContainer),
                    [EGroundTextureType.SurfaceTexture] = ETerrainInitializationHelper.CreateSurfaceTextureEntitiesGenerator(
                        _feConfiguration, startConfiguration, _gameInitializationFields, _ultraUpdatableContainer)
                }
            );
            return initializingHelper;
        }

        private void InitializeUI(ETerrainHeightPyramidFacadeStartConfiguration startConfiguration)
        {
            if (HeightPyramidExplorer != null)
            {
                _gameInitializationFields.SetField(HeightPyramidExplorer);
            }

            if (_gameInitializationFields.HasField<MultipleLevelsHeightPyramidExplorerGO>())
            {
                _gameInitializationFields.Retrive<MultipleLevelsHeightPyramidExplorerGO>().Initialize(startConfiguration.HeightPyramidLevels,
                    _eTerrainHeightPyramidFacade.CeilTextureArrays.Where(c => c.TextureType == EGroundTextureType.HeightMap).Select(c => c.Texture).First(),
                    startConfiguration.CommonConfiguration.SlotMapSize,
                    startConfiguration.CommonConfiguration.RingsUvRange,
                    startConfiguration.PerLevelConfigurations.ToDictionary(c => c.Key, c => c.Value.BiggestShapeObjectInGroupLength));
            }

            if (Overlay != null)
            {
                Overlay.ServicesProfileInfo = _gameInitializationFields.Retrive<GlobalServicesProfileInfo>();
            }
        }

        private RunOnceBox _movementStartBox;

        public void Update()
        {
            if (!_initializationWasSuccessfull)
            {
                return;
            }

            _movementCustodian.Update();
            if (_movementCustodian.IsMovementPossible())
            {
                RunOnceBox.RunOnce(ref _movementStartBox, () =>
                {
                    Debug.Log("time to moving "+Time.realtimeSinceStartup);
                }, 3);
            }
            Traveller.SetActive(_movementCustodian.IsMovementPossible());
            Overlay.SetMovementPossibilityDetails(_movementCustodian.ThisFrameBlockingProcesses);

            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new MockedFromGameObjectCameraForUpdate(Traveller)); });
            var position3D = Traveller.transform.position;
            var travellerFlatPosition = new Vector2(position3D.x, position3D.z);

             _eTerrainHeightPyramidFacade.Update(travellerFlatPosition);

            if (_gameInitializationFields.HasField<MultipleLevelsHeightPyramidExplorerGO>())
            {
                var explorer = _gameInitializationFields.Retrive<MultipleLevelsHeightPyramidExplorerGO>();
                explorer.UpdateTravellingUniforms(travellerFlatPosition, _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel);
                explorer.UpdateHeightmapSegmentFillingState(_heightmapListenersContainer.ListenersDict.ToDictionary(c=>c.Key, c=>c.Value.TokensDict));
            }
        }
    }
}
