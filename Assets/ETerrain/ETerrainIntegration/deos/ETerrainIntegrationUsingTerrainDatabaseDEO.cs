using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.FinalExecution;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration.deos
{
    public class ETerrainIntegrationUsingTerrainDatabaseDEO : MonoBehaviour
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
            _configuration.TerrainShapeDbConfiguration.UseTextureLoadingFromDisk =true;
            _configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk =true;
            _configuration.TerrainShapeDbConfiguration.MergeTerrainDetail =true;
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            _gameInitializationFields = new GameInitializationFields();
            _ultraUpdatableContainer = ETerrainTestUtils.InitializeFinalElements(_configuration, containerGameObject, _gameInitializationFields);

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            startConfiguration.CommonConfiguration.YScale = _gameInitializationFields.Retrive<HeightDenormalizer>().DenormalizationMultiplier;
            startConfiguration.CommonConfiguration.InterSegmentMarginSize = 1/8.0f;
            startConfiguration.InitialTravellerPosition = new Vector2(490, -21) + new Vector2(90f*8, 90f*4);
            startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() {HeightPyramidLevel.Top, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom};

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
                    {
                        EGroundTextureType.HeightMap, GenerateHeightTextureEntitiesGeneratorFromTerrainShapeDb(startConfiguration, dbProxy, repositioner, _gameInitializationFields,true)
                    }
                }
            );

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Top);
            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Mid);
            //_eTerrainHeightPyramidFacade.SetShapeRootTransform(new MyTransformTriplet(new Vector3(0, -240, 0), Quaternion.identity, new Vector3(1, 20, 1)));
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator GenerateHeightTextureEntitiesGeneratorFromTerrainShapeDb(
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, TerrainShapeDbProxy dbProxy, Repositioner repositioner,
            GameInitializationFields initializationFields, bool modifyCorners=true)
        {
            return new OneGroundTypeLevelTextureEntitiesGenerator
            {
                LambdaSegmentFillingListenerGenerator =
                    (level, segmentModificationManager) =>
                    {
                        return new LambdaSegmentFillingListener(
                            c =>
                            {
                                var surfaceWorldSpaceRectangle = ETerrainUtils.TerrainShapeSegmentAlignedPositionToWorldSpaceArea(level,
                                    startConfiguration.PerLevelConfigurations[level], c.SegmentAlignedPosition);

                                var terrainDetailElementOutput = dbProxy.Query(new TerrainDescriptionQuery()
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
                                }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                                var segmentTexture = terrainDetailElementOutput.TokenizedElement.DetailElement.Texture.Texture;
                                dbProxy.DisposeTerrainDetailElement(terrainDetailElementOutput.TokenizedElement.Token);
                                segmentModificationManager.AddSegment(segmentTexture, c.SegmentAlignedPosition);
                            },
                            c => { },
                            c => { });
                    },
                CeilTextureGenerator = () => EGroundTextureGenerator.GenerateEmptyGroundTexture(
                    startConfiguration.CommonConfiguration.CeilTextureSize, startConfiguration.CommonConfiguration.HeightTextureFormat),
                SegmentPlacerGenerator = ceilTexture =>
                {
                    var modifiedCornerBuffer =
                        EGroundTextureGenerator.GenerateModifiedCornerBuffer(startConfiguration.CommonConfiguration.SegmentTextureResolution,
                            startConfiguration.CommonConfiguration.HeightTextureFormat);

                    return new HeightSegmentPlacer(
                        initializationFields.Retrive<UTTextureRendererProxy>(),
                        ceilTexture
                        , startConfiguration.CommonConfiguration.SlotMapSize
                        , startConfiguration.CommonConfiguration.CeilTextureSize
                        , startConfiguration.CommonConfiguration.InterSegmentMarginSize
                        , modifiedCornerBuffer, modifyCorners);
                }
            };
        }

        public void Update()
        {
            _updaterUntilException.Execute(() => { _ultraUpdatableContainer.Update(new EncapsulatedCameraForUpdate(FindObjectOfType<Camera>())); });
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
