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
                            SegmentTextureResolution = new IntVector2(240, 240), // rozdzielczosc tekstury segmentow - rowna Ilosci vierzechołkow centralnego mesha
                            HeightTextureFormat = RenderTextureFormat.RFloat,
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
                            InterSegmentMarginSize = 1/6.0f
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
                                    PerRingMergeWidths = new Dictionary<int, float>()
                                    {
                                        {0, 1.5f * 8},
                                        {1, 1.5f * 8},
                                        {2, 1.5f * 16},
                                    }
                                }
                            },
                            {
                                HeightPyramidLevel.Mid, new HeightPyramidPerLevelConfiguration()
                                {
                                    SegmentFillerStandByMarginsSize = new IntVector2(1, 1),
                                    BiggestShapeObjectInGroupLength = 90f * 8,
                                    TransitionSingleStepPercent = 0.05f/3.0f,
                                    CreateCenterObject = true,
                                    PerRingMergeWidths = new Dictionary<int, float>()
                                    {
                                        {0, 1.5f * 8 * 4 },
                                        {1, 1.5f * 8 * 4},
                                        {2, 1.5f * 16 * 4}
                                    }
                                }
                            },
                            {
                                HeightPyramidLevel.Bottom, new HeightPyramidPerLevelConfiguration()
                                {
                                    SegmentFillerStandByMarginsSize = new IntVector2(1, 1),
                                    BiggestShapeObjectInGroupLength = 90f * 64,
                                    TransitionSingleStepPercent = 0.05f/3.0f,
                                    CreateCenterObject = true,
                                    PerRingMergeWidths = new Dictionary<int, float>()
                                    {
                                        {0, 1.5f * 8 * 4 * 4 },
                                        {1, 1.5f * 8 * 4 * 4},
                                        {2, 1.5f * 16 * 4 * 4}
                                    }
                                }
                            }
                        }
                };
            }
        } 
    }

    public class HeightPyramidLevelTemplateWithShapeConfiguration
    {
        public HeightPyramidLevelShapeGenerationConfiguration ShapeConfiguration;
        public HeightPyramidLevelTemplate LevelTemplate;
    }

    public class OneGroundTypeLevelTextureEntitiesGenerator
    {
        public Func< RenderTexture> CeilTextureGenerator;
        public Func<HeightPyramidLevel, SoleLevelGroundTextureSegmentModificationsManager, LambdaSegmentFillingListener> LambdaSegmentFillingListenerGenerator;
        public Func< RenderTexture, IGroundTextureSegmentPlacer> SegmentPlacerGenerator;
    }

    public class PerGroundTypeEntities
    {
        public EGroundTexture CeilTexture;
        public SegmentFiller Filler;
    }
}