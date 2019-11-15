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
using Assets.Utils.Services;
using Assets.Utils.ShaderBuffers;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration.deos
{
    public class ETerrainWithVegetationDEO : MonoBehaviour
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
                    [EGroundTextureType.HeightMap] = ETerrainIntegrationUsingTerrainDatabaseDEO.GenerateHeightTextureEntitiesGeneratorFromTerrainShapeDb(
                        startConfiguration, dbProxy, repositioner, _gameInitializationFields),
                    [EGroundTextureType.SurfaceTexture] = ETerrainIntegrationWithShapeDbAndSurfaceDbDEO.GenerateSurfaceTextureEntitiesGeneratorFromTerrainShapeDb(
                        _configuration,startConfiguration,_gameInitializationFields,_ultraUpdatableContainer,repositioner)
                }
            );

            initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject());
            EPropElevationConfiguration ePropLocationConfiguration = new EPropElevationConfiguration();
            InitializeDesignBodySpotUpdater(startConfiguration, ePropLocationConfiguration, _gameInitializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(), buffersManager,
                perLevelTemplates);

            var reloader = FindObjectOfType<BufferReloaderRootGO>();
            var commonUniforms = new UniformsPack();
            commonUniforms.SetUniform("_ScopeLength", ePropLocationConfiguration.ScopeLength);
            ComputeBuffersPack computeBuffersPack = new ComputeBuffersPack(reloader);
            computeBuffersPack.SetBuffer("_EPropLocaleBuffer", _elevationManager.EPropLocaleBuffer);
            computeBuffersPack.SetBuffer("_EPropIdsBuffer", _elevationManager.EPropIdsBuffer);

            var finalVegetation = new FinalVegetation(_gameInitializationFields, _ultraUpdatableContainer, VegetationConfiguration
                , new UniformsAndComputeBuffersPack(commonUniforms, computeBuffersPack));
            finalVegetation.Start();

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            Debug.Log("Init time "+msw.CollectResults());
        }

        private bool _wasFirstUpdateDone = false;

        public void Update()
        {
            var msw = new MyStopWatch();
            msw.StartSegment("FIRST UPDATE");

            var camera = FindObjectOfType<Camera>();
            camera.transform.position = Traveller.transform.position;
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(camera); });
            var position3D = Traveller.transform.position;
            var travellerFlatPosition = new Vector2(position3D.x, position3D.z);

             _eTerrainHeightPyramidFacade.Update(travellerFlatPosition);

            if (Time.frameCount < 10)
            {
                var selectorWithParameters = EPropHotAreaSelectorWithParameters.Create(_ePropHotAreaSelector,
                    _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel, travellerFlatPosition);
                _elevationManager.Update(travellerFlatPosition, _eTerrainHeightPyramidFacade.PyramidCenterWorldSpacePerLevel, selectorWithParameters);
            }

            if (Time.frameCount % 100 == 0)
            {
                if (false)
                {
                    var propLocaleChanges = _elevationManager.RecalculateSectorsDivision(travellerFlatPosition);
                }
            }
            if (!_wasFirstUpdateDone)
            {
                Debug.Log("FIRST UPDATE RESULT " + msw.CollectResults());
                _wasFirstUpdateDone = true;
            }
        }

        public void InitializeDesignBodySpotUpdater(ETerrainHeightPyramidFacadeStartConfiguration startConfiguration,
            EPropElevationConfiguration ePropLocationConfiguration,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject, ETerrainHeightBuffersManager buffersManager,
            Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplateWithShapeConfiguration> perLevelTemplates)
        {
            var ePropConstantPyramidParameters = new EPropConstantPyramidParameters()
            {
                LevelsCount = startConfiguration.HeightPyramidLevels.Count,
                RingsPerLevelCount = startConfiguration.CommonConfiguration.MaxRingsPerLevelCount, //TODO parametrize
                HeightScale = startConfiguration.CommonConfiguration.YScale
            };
            _elevationManager = new EPropElevationManager( ePropLocationConfiguration, shaderExecutorObject, ePropConstantPyramidParameters);
            _elevationManager.Initialize(buffersManager.PyramidPerFrameParametersBuffer, buffersManager.EPyramidConfigurationBuffer,
                _eTerrainHeightPyramidFacade.CeilTextures.ToDictionary(c => c.Key, c => c.Value.First(r => r.TextureType == EGroundTextureType.HeightMap).Texture as Texture) );

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

        }

    }
}
