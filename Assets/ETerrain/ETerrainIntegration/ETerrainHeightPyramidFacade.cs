﻿using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.SectorFilling;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
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

        private RunOnceBox _fillingInitializationBox;

        public ETerrainHeightPyramidFacade(ETerrainHeightBuffersManager buffersManager,
            MeshGeneratorUTProxy meshGeneratorUtProxy, UTTextureRendererProxy textureRendererProxy,
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration )
        {
            _buffersManager = buffersManager;
            _meshGeneratorUtProxy = meshGeneratorUtProxy;
            _textureRendererProxy = textureRendererProxy;
            _startConfiguration = startConfiguration;
        }

        public Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplate> GenerateLevelTemplates()
        {
            var perLevelConfigurations = _startConfiguration.PerLevelConfigurations;

            var templateGenerator = new HeightPyramidLevelTemplateGenerator();
            return _startConfiguration.HeightPyramidLevels.ToDictionary(c => c, c =>
            {
                var perLevelConfiguration = perLevelConfigurations[c];

                var perLevelTemplate = templateGenerator.CreateGroup( perLevelConfiguration, Vector2.zero, perLevelConfiguration.CreateCenterObject);
                return perLevelTemplate;
            });
        }

        public void Start(
            Dictionary<HeightPyramidLevel, HeightPyramidLevelTemplate> templates,
            Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator> groundEntitiesGenerators
        )
        {
            var heightLevels = _startConfiguration.HeightPyramidLevels.OrderBy(c=> c.GetIndex()).ToList();
            var heightPyramidMapConfiguration = _startConfiguration.CommonConfiguration;
            var perLevelConfigurations = _startConfiguration.PerLevelConfigurations;

            _pyramidRootGo = new GameObject("HeightPyramidRoot");

            var floorTextureArrays = groundEntitiesGenerators.SelectMany( c => c.Value.FloorTextureArrayGenerator()).ToList();

            _perLevelEntites = new Dictionary<HeightPyramidLevel, HeightPyramidLevelEntities>();
            foreach (var level in heightLevels)
            {
                var perLevelConfiguration = perLevelConfigurations[level];

                var perGroundTypesEntities = groundEntitiesGenerators.Select(c =>
                {
                    var generator = c.Value;

                    var fillingListener= generator.SegmentFillingListenerGeneratorFunc(level, floorTextureArrays);

                    var segmentFiller = new SegmentFiller(heightPyramidMapConfiguration.SlotMapSize, perLevelConfiguration.SegmentFillerStandByMarginsSize,
                        perLevelConfiguration.BiggestShapeObjectInGroupLength, fillingListener);

                    return new PerGroundTypeEntities()
                    {
                        Filler = segmentFiller
                    };
                }).ToList();

                var perLevelTemplate = templates[level];

                IPyramidShapeInstancer shapeInstancer = null;
                if (heightPyramidMapConfiguration.MergeShapesOfRings)
                {
                    shapeInstancer = new MergedMeshesPyramidShapeInstancer(_meshGeneratorUtProxy, heightPyramidMapConfiguration, perLevelConfiguration);
                }
                else
                {
                    shapeInstancer = new SeparateMeshPerTemplateShapeInstancer(_meshGeneratorUtProxy, heightPyramidMapConfiguration, perLevelConfiguration);
                }
                var group = shapeInstancer.CreateGroup(perLevelTemplate,level, _pyramidRootGo);

                var heightPyramidLocationParametersUpdaterConfiguration = new HeightPyramidLocationParametersUpdaterConfiguration()
                {
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

            ComputeBuffer configurationBuffer = _buffersManager.EPyramidConfigurationBuffer;
            var bufferReloaderRootGo = Object.FindObjectOfType<BufferReloaderRootGO>();

            var ePyramidPerFrameParametersBuffer = _buffersManager.PyramidPerFrameParametersBuffer;
            foreach (var pair in _perLevelEntites)
            {
                var shapeGroup = pair.Value.ShapeGroup;
                var heightPyramidLevel = pair.Key;
                var ringsPerLevelCount = _startConfiguration.CommonConfiguration.MaxRingsPerLevelCount; //TODO
                heightmapUniformsSetter.InitializePyramidUniforms( shapeGroup, heightPyramidLevel,floorTextureArrays, heightLevels.Count, ringsPerLevelCount);
                heightmapUniformsSetter.PassPyramidBuffers(shapeGroup, configurationBuffer, bufferReloaderRootGo, ePyramidPerFrameParametersBuffer);
            }

            _fillingInitializationBox = new RunOnceBox(() =>
            {
                foreach (var pair in _perLevelEntites)
                {
                    foreach (var perGroundEntities in pair.Value.PerGroundEntities)
                    {
                        perGroundEntities.Filler.InitializeField(_startConfiguration.InitialTravellerPosition);
                    }
                }
            });
            if (_startConfiguration.GenerateInitialSegmentsDuringStart)
            {
                _fillingInitializationBox.Update(); 
            }

            _pyramidCommonEntites = new HeightPyramidCommonEntites()
            {
                HeightPyramidMapConfiguration = heightPyramidMapConfiguration,
                HeightmapUniformsSetter = heightmapUniformsSetter,
                GroupMover = groupMover,
                FloorTextureArrays = floorTextureArrays
            };
        }

        public void Update(Vector2 flatPosition)
        {
            _fillingInitializationBox.Update();
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

        public List<EGroundTexture> FloorTextureArrays => _pyramidCommonEntites.FloorTextureArrays;

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