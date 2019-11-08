using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ESurface;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.TestUtils;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
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
            _configuration = new FEConfiguration(new FilePathsConfiguration()) {Multithreading = false};
            _configuration.TerrainDetailProviderConfiguration.UseTextureLoadingFromDisk = true;
            _configuration.TerrainDetailProviderConfiguration.UseTextureSavingToDisk = true;
            _configuration.TerrainDetailProviderConfiguration.MergeTerrainDetail = true;
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            _gameInitializationFields = new GameInitializationFields();
            _ultraUpdatableContainer = ETerrainTestUtils.InitializeFinalElements(_configuration, containerGameObject, _gameInitializationFields);

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            startConfiguration.CommonConfiguration.YScale = _gameInitializationFields.Retrive<HeightDenormalizer>().DenormalizationMultiplier;
            startConfiguration.CommonConfiguration.InterSegmentMarginSize = 1/6.0f;
            startConfiguration.InitialTravellerPosition = new Vector2(490, -21) + new Vector2(90f*8, 90f*4);
            //startConfiguration.InitialTravellerPosition = new Vector2(0,0);
            startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() { HeightPyramidLevel.Top};//, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom};

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

            var surfaceTextureFormat = RenderTextureFormat.ARGB32;
            var intensityPatternPixelsPerUnit = new Dictionary<int, float>()
            {
                {1, 1}
            };
            int mipmapLevelToExtract = 2;
            var plateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                {1, 3}
            };
            //var surfacePatchProvider = ESurfaceProviderInitializationHelper.ConstructProvider(
            //    _ultraUpdatableContainer, intensityPatternPixelsPerUnit, containerGameObject, mipmapLevelToExtract, plateStampPixelsPerUnit);

            var patchInitializer = new Ring2PatchInitialization(_gameInitializationFields, _ultraUpdatableContainer, new FeRing2PatchConfiguration(_configuration));
            patchInitializer.Start();

            MipmapExtractor mipmapExtractor = new MipmapExtractor(_gameInitializationFields.Retrive<UTTextureRendererProxy>());
            var patchesCreatorProxy = _gameInitializationFields.Retrive<GRing2PatchesCreatorProxy>();
            var  patchStamperOverseerFinalizer= _gameInitializationFields.Retrive<Ring2PatchStamplingOverseerFinalizer>();
            var surfacePatchProvider = new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer, mipmapExtractor, mipmapLevelToExtract);

            UTTextureRendererProxy textureRendererProxy = _gameInitializationFields.Retrive<UTTextureRendererProxy>();
            _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>
                {
                    [EGroundTextureType.HeightMap] = ETerrainIntegrationUsingTerrainDatabaseDEO.GenerateHeightTextureEntitiesGeneratorFromTerrainShapeDb(
                        startConfiguration, dbProxy, repositioner, _gameInitializationFields),
                    [EGroundTextureType.SurfaceTexture] = new OneGroundTypeLevelTextureEntitiesGenerator()
                    {
                        LambdaSegmentFillingListenerGenerator =
                            (level, segmentModificationManager) => new LambdaSegmentFillingListener(
                                (c) =>
                                {

                                    var surfaceWorldSpaceRectangle = ETerrainUtils.SegmentAlignedPositionToWorldSpaceArea(level,
                                        startConfiguration.PerLevelConfigurations[level], c.SegmentAlignedPosition);
                                    var pack = surfacePatchProvider.ProvideSurfaceDetail(repositioner.InvMove(surfaceWorldSpaceRectangle),
                                        new FlatLod(1, 1));

                                    if (pack != null)
                                    {
                                        var mainTexture = pack.MainTexture;
                                        segmentModificationManager.AddSegment(mainTexture, c.SegmentAlignedPosition);
                                        GameObject.Destroy(mainTexture);
                                    }
                                },
                                (c) => { },
                                (c) => { }),
                        CeilTextureGenerator = () =>
                            EGroundTextureGenerator.GenerateEmptyGroundTexture(startConfiguration.CommonConfiguration.CeilTextureSize,
                                surfaceTextureFormat),
                        SegmentPlacerGenerator = (ceilTexture) => new ESurfaceSegmentPlacer(textureRendererProxy, ceilTexture,
                            startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize)
                    }
                }
            );

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Top);
            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Mid);
            //_eTerrainHeightPyramidFacade.SetShapeRootTransform(new MyTransformTriplet(new Vector3(0, -240, 0), Quaternion.identity, new Vector3(1, 20, 1)));
        }

        public void Update()
        {
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(FindObjectOfType<Camera>()); });
            var position3D = Traveller.transform.position;
            var flatPosition = new Vector2(position3D.x, position3D.z);

            _eTerrainHeightPyramidFacade.Update(flatPosition);

            if (Time.frameCount > 10)
            {
                //EditorApplication.isPaused = true;
            }
        }

    }
}
