using System.Collections.Generic;
using Assets.ETerrain.Pyramid.Shape;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class HeightPyramidPerLevelConfiguration
    {
        public IntVector2 SegmentFillerStandByMarginsSize;
        public float BiggestShapeObjectInGroupLength;
        public int OneRingShapeObjectsCount =>  HeightPyramidLevelShapeGenerationConfiguration.OneRingShapeObjectsCount;
        public Vector2 CeilTextureWorldSize => new Vector2( BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2), BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2));
        public float TransitionSingleStepPercent;
        public bool CreateCenterObject;

        public Dictionary<int, float> PerRingMergeWidths;
    }
}