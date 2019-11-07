﻿using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.SectorFilling;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Utils;
using Assets.Utils.ShaderBuffers;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class ETerrainHeightPyramidFacade
    {
        private ETerrainHeightBuffersManager _buffersManager;
        private readonly MeshGeneratorUTProxy _meshGeneratorUtProxy;
        private readonly UTTextureRendererProxy _textureRendererProxy;
        private readonly ETerrainHeightPyramidFacadeStartConfiguration _startConfiguration;

        private Dictionary<HeightPyramidLevel, HeightPyramidLevelEntities> _perLevelEntites;
        private HeightPyramidCommonEntites _pyramidCommonEntites;
        private Dictionary<HeightPyramidLevel, Vector2> _pyramidCenterWorldSpacePerLevel;
        private GameObject _pyramidRootGo;

        public ETerrainHeightPyramidFacade(ETerrainHeightBuffersManager buffersManager,
            MeshGeneratorUTProxy meshGeneratorUtProxy, UTTextureRendererProxy textureRendererProxy,
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration )
        {
            _buffersManager = buffersManager;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
            _textureRendererProxy = textureRendererProxy;
            _startConfiguration = startConfiguration;
        }

        public Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplateWithShapeConfiguration> GenerateLevelTemplates()
        {
            var heightPyramidMapConfiguration = _startConfiguration.CommonConfiguration;
            var perLevelConfigurations = _startConfiguration.PerLevelConfigurations;

            return _startConfiguration.HeightPyramidLevels.ToDictionary(c => c, c =>
            {
                var perLevelConfiguration = perLevelConfigurations[c];

                var heightPyramidLevelShapeGenerationConfiguration = new HeightPyramidLevelShapeGenerationConfiguration()
                {
                    YScale = heightPyramidMapConfiguration.YScale,
                    PyramidLevelWorldSize = perLevelConfiguration.PyramidLevelWorldSize,
                    CenterObjectMeshVertexLength = heightPyramidMapConfiguration.SegmentTextureResolution,
                    CenterObjectLength = perLevelConfiguration.BiggestShapeObjectInGroupLength,
                    TransitionSingleStepPercent = perLevelConfiguration.TransitionSingleStepPercent,
                    PerRingMergeWidths = perLevelConfiguration.PerRingMergeWidths
                };

                var templateGenerator = new HeightPyramidLevelTemplateGenerator(heightPyramidLevelShapeGenerationConfiguration);
                var perLevelTemplate =
                    templateGenerator.CreateGroup(Vector2.zero, perLevelConfiguration.CreateCenterObject, heightPyramidMapConfiguration.RingsUvRange);
                return new HeightPyramidLevelTemplateWithShapeConfiguration()
                {
                    LevelTemplate = perLevelTemplate,
                    ShapeConfiguration = heightPyramidLevelShapeGenerationConfiguration
                };
            });
        }

        public void Start(
            Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplateWithShapeConfiguration> templates,
            Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator> groundEntitiesGenerators
        )
        {
            var heightLevels = _startConfiguration.HeightPyramidLevels.OrderBy(c=> c.GetIndex()).ToList();
            var heightPyramidMapConfiguration = _startConfiguration.CommonConfiguration;
            var perLevelConfigurations = _startConfiguration.PerLevelConfigurations;

            _pyramidRootGo = new GameObject("HeightPyramidRoot");
            _perLevelEntites = new Dictionary<HeightPyramidLevel, HeightPyramidLevelEntities>();
            foreach (var level in heightLevels)
            {
                var perLevelConfiguration = perLevelConfigurations[level];

                var perGroundTypesEntities = groundEntitiesGenerators.Select(c =>
                {
                    var groundType = c.Key;
                    var generators = c.Value;
                    var ceilTexture = new EGroundTexture(
                        generators.CeilTextureGenerator(),
                        groundType
                    );

                    var segmentsPlacer = generators.SegmentPlacerGenerator( ceilTexture.Texture);

                    var pyramidLevelManager = new GroundLevelTexturesManager(heightPyramidMapConfiguration.SlotMapSize);

                    var segmentAddingManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);
                    var segmentFiller = new SegmentFiller(heightPyramidMapConfiguration.SlotMapSize, perLevelConfiguration.SegmentFillerStandByMarginsSize,
                        perLevelConfiguration.BiggestShapeObjectInGroupLength,
                        generators.LambdaSegmentFillingListenerGenerator(level, segmentAddingManager));

                    return new PerGroundTypeEntities()
                    {
                        CeilTexture = ceilTexture,
                        Filler = segmentFiller
                    };
                }).ToList();

                var heightPyramidLevelShapeGenerationConfiguration = templates[level].ShapeConfiguration;
                var perLevelTemplate = templates[level].LevelTemplate;
                var instancer = new HeightPyramidShapeInstancer(_meshGeneratorUtProxy, heightPyramidLevelShapeGenerationConfiguration);
                var group = instancer.CreateGroup(perLevelTemplate,level, _pyramidRootGo);

                var heightPyramidLocationParametersUpdaterConfiguration = new HeightPyramidLocationParametersUpdaterConfiguration()
                {
                    PyramidLevelWorldSize = perLevelConfiguration.PyramidLevelWorldSize,
                    TransitionSingleStep = perLevelConfiguration.BiggestShapeObjectInGroupLength * perLevelConfiguration.TransitionSingleStepPercent
                };
                var updater = new HeightPyramidLocationUniformsGenerator(heightPyramidLocationParametersUpdaterConfiguration);
                var transitionResolver = new HeightPyramidGroupTransitionResolver(group, heightPyramidLocationParametersUpdaterConfiguration);

                _perLevelEntites[level] = new HeightPyramidLevelEntities()
                {
                    PerGroundEntities = perGroundTypesEntities,
                    ShapeGroup = group,
                    LocationUniformsGenerator = updater,
                    TransitionResolver = transitionResolver,
                    LevelTemplate = perLevelTemplate,
                    PerLevelConfiguration = perLevelConfiguration,
                };
            }

            var heightmapUniformsSetter = new HeightPyramidSegmentsUniformsSetter();
            var groupMover = new HeightPyramidGroupMover();

            var pyramidLevelsWorldSizes =
                perLevelConfigurations.ToDictionary(c => c.Key, c => c.Value.PyramidLevelWorldSize.Width); // TODO works only for square pyramids - i use width
            ComputeBuffer configurationBuffer = _buffersManager.EPyramidConfigurationBuffer;
            var levelTextures = _perLevelEntites.ToDictionary(c => c.Key, c =>c.Value.PerGroundEntities.Select(k => k.CeilTexture).ToList());
            var bufferReloaderRootGo = Object.FindObjectOfType<BufferReloaderRootGO>();

            var ePyramidPerFrameParametersBuffer = _buffersManager.PyramidPerFrameParametersBuffer;
            foreach (var pair in _perLevelEntites)
            {
                var shapeGroup = pair.Value.ShapeGroup;
                var heightPyramidLevel = pair.Key;
                var ringsPerLevelCount = _startConfiguration.CommonConfiguration.MaxRingsPerLevelCount; //TODO
                heightmapUniformsSetter.InitializePyramidUniforms( shapeGroup, heightPyramidLevel, pyramidLevelsWorldSizes, heightPyramidMapConfiguration, levelTextures, heightLevels.Count, ringsPerLevelCount);
                heightmapUniformsSetter.InitializePerRingUniforms(shapeGroup, heightPyramidLevel, levelTextures, pyramidLevelsWorldSizes);
                heightmapUniformsSetter.PassPyramidBuffers(shapeGroup, configurationBuffer, bufferReloaderRootGo, ePyramidPerFrameParametersBuffer);

                foreach (var perGroundEntities in pair.Value.PerGroundEntities)
                {
                    perGroundEntities.Filler.InitializeField(_startConfiguration.InitialTravellerPosition);
                }
            }

            _pyramidCommonEntites = new HeightPyramidCommonEntites()
            {
                HeightPyramidMapConfiguration = heightPyramidMapConfiguration,
                HeightmapUniformsSetter = heightmapUniformsSetter,
                GroupMover = groupMover,
            };
        }

        public void Update(Vector2 flatPosition)
        {
            var transitionsDict = _perLevelEntites.ToDictionary(c => c.Key, c => c.Value.TransitionResolver.ResolveTransition(flatPosition));
            var uniformsDict = _perLevelEntites.ToDictionary(c => c.Key, c => c.Value.LocationUniformsGenerator.GenerateUniforms(transitionsDict[c.Key]));
            foreach (var pair in _perLevelEntites)
            {
                var entities = pair.Value;
                var groundLevel = pair.Key;
                _pyramidCommonEntites.HeightmapUniformsSetter.UpdateUniforms(entities.ShapeGroup, groundLevel, flatPosition, uniformsDict);
                _pyramidCommonEntites.GroupMover.MoveGroup(pair.Value.ShapeGroup, transitionsDict[groundLevel]);

                foreach (var perGroundEntities in entities.PerGroundEntities)
                {
                    perGroundEntities.Filler.Update(flatPosition);
                }
            }

            var heightLevels = _startConfiguration.HeightPyramidLevels.OrderBy(c=> c.GetIndex()).ToList();
            _pyramidCenterWorldSpacePerLevel = uniformsDict.ToDictionary(c => c.Key, c => c.Value.PyramidCenterWorldSpace);
            _buffersManager.UpdateEPyramidPerFrameParametersBuffer( heightLevels, _startConfiguration.CommonConfiguration.MaxLevelsCount, _pyramidCenterWorldSpacePerLevel);
        }

        public Dictionary<HeightPyramidLevel, Vector2> PyramidCenterWorldSpacePerLevel => _pyramidCenterWorldSpacePerLevel;

        public Dictionary<HeightPyramidLevel, List<EGroundTexture>> CeilTextures =>
            _perLevelEntites.ToDictionary(c => c.Key, c => c.Value.PerGroundEntities.Select(k => k.CeilTexture).ToList());

        public void DisableShapes()
        {
            foreach (var heightPyramidSegmentShapeGroup in _perLevelEntites.Values.Select(c=>c.ShapeGroup))
            {
                heightPyramidSegmentShapeGroup.DisableShapes();
            }
        }

        public void DisableLevelShapes(HeightPyramidLevel level)
        {
            foreach (var heightPyramidSegmentShapeGroup in _perLevelEntites.Where(c=>c.Key==level).Select(c=>c.Value.ShapeGroup))
            {
                heightPyramidSegmentShapeGroup.DisableShapes();
            }
        }

        public void SetShapeRootTransform(MyTransformTriplet myTransformTriplet)
        {
            myTransformTriplet.SetTransformTo(_pyramidRootGo.transform);
        }
    }
}