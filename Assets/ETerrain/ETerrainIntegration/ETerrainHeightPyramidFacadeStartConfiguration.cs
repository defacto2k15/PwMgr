using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.Pyramid.Shape;
using Assets.ETerrain.SectorFilling;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class ETerrainHeightPyramidFacadeStartConfiguration
    {
        public bool GenerateInitialSegmentsDuringStart = true;
        public Vector2 InitialTravellerPosition;
        public List<HeightPyramidLevel> HeightPyramidLevels; 
        public HeightPyramidCommonConfiguration CommonConfiguration;
        public Dictionary<HeightPyramidLevel, HeightPyramidPerLevelConfiguration> PerLevelConfigurations;

        public static ETerrainHeightPyramidFacadeStartConfiguration DefaultConfiguration
        {
            get
            {
                return new ETerrainHeightPyramidFacadeStartConfiguration()
                {
                    InitialTravellerPosition = Vector2.zero,
                    HeightPyramidLevels = new List<HeightPyramidLevel>()
                    {
                        HeightPyramidLevel.Bottom,HeightPyramidLevel.Mid, HeightPyramidLevel.Top
                    }.OrderBy(c => c).ToList(),
                    CommonConfiguration =
                        new HeightPyramidCommonConfiguration()
                        {
                            SlotMapSize = new IntVector2(6, 6),
                            SegmentTextureResolution = new IntVector2(240, 240), // rozdzielczosc tekstury jednego segmentu
                            HeightTextureFormat = RenderTextureFormat.RFloat,
                            NormalTextureFormat= RenderTextureFormat.ARGB32,
                            SurfaceTextureFormat = RenderTextureFormat.ARGB32,
                            YScale = 50f,
                            RingsUvRange = new Dictionary<int, Vector2>()
                            {
                                {0, new Vector2(0, 0.5f / 6.0f)},
                                {1, new Vector2(0.5f / 6.0f, 1f / 6.0f)},
                                {2, new Vector2(1f / 6.0f, 2f / 6.0f)},
                            },
                            MaxLevelsCount = 3,
                            MaxRingsPerLevelCount = 3,
                            InterSegmentMarginSize = 1/36.0f,
                            ModifyCornersInHeightSegmentPlacer = true,
                            UseNormalTextures = true,
                            MergeShapesInRings  = false
                        },
                    PerLevelConfigurations =
                        new Dictionary<HeightPyramidLevel, HeightPyramidPerLevelConfiguration>()
                        {
                            {
                                HeightPyramidLevel.Top, new HeightPyramidPerLevelConfiguration()
                                {
                                    SegmentFillerStandByMarginsSize = new IntVector2(1, 1),
                                    BiggestShapeObjectInGroupLength = 90f,
                                    TransitionSingleStepPercent = 0.05f / 3.0f,
                                    CreateCenterObject = true,
                                    PerRingConfigurations = new Dictionary<int, HeightPyramidPerRingConfiguration>()
                                    {
                                        [0] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 8 },
                                        [1] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 8 },
                                        [2] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 16 },
                                    },
                                    RingsCount = 3,
                                    CenterObjectMeshVertexLength = new IntVector2(240, 240),
                                    RingObjectMeshVertexLength = new IntVector2(60, 60)
                                }
                            },
                            {
                                HeightPyramidLevel.Mid, new HeightPyramidPerLevelConfiguration()
                                {
                                    SegmentFillerStandByMarginsSize = new IntVector2(1, 1),
                                    BiggestShapeObjectInGroupLength = 90f * 8,
                                    TransitionSingleStepPercent = 0.05f/3.0f,
                                    CreateCenterObject = true,
                                    PerRingConfigurations = new Dictionary<int, HeightPyramidPerRingConfiguration>()
                                    {
                                        [0] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 8 * 4 },
                                        [1] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 8 * 4 },
                                        [2] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 16 * 4 },
                                    },
                                    RingsCount = 3,
                                    CenterObjectMeshVertexLength = new IntVector2(240, 240),
                                    RingObjectMeshVertexLength = new IntVector2(60, 60)
                                }
                            },
                            {
                                HeightPyramidLevel.Bottom, new HeightPyramidPerLevelConfiguration()
                                {
                                    SegmentFillerStandByMarginsSize = new IntVector2(1, 1),
                                    BiggestShapeObjectInGroupLength = 90f * 64,
                                    TransitionSingleStepPercent = 0.05f/3.0f,
                                    CreateCenterObject = true,
                                    PerRingConfigurations = new Dictionary<int, HeightPyramidPerRingConfiguration>()
                                    {
                                        [0] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 8 * 4 * 4 },
                                        [1] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 8 * 4 * 4 },
                                        [2] = new HeightPyramidPerRingConfiguration(){ MergeWidth = 1.5f * 16 * 4 * 4 },
                                    },
                                    RingsCount = 3,
                                    CenterObjectMeshVertexLength = new IntVector2(240, 240),
                                    RingObjectMeshVertexLength = new IntVector2(60, 60)
                                }
                            }
                        }
                };
            }
        } 
    }

    public class HeightPyramidLevelTemplateWithShapeConfiguration
    {
        public HeightPyramidLevelTemplate LevelTemplate;
    }

    public class OneGroundTypeLevelTextureEntitiesGenerator
    {
        public Func<HeightPyramidLevel, List<EGroundTexture>, ISegmentFillingListener> SegmentFillingListenerGeneratorFunc;
        public Func< List<EGroundTexture>> CeilTextureArrayGenerator;
    }

    public class PerGroundTypeEntities
    {
        public SegmentFiller Filler;
    }
}