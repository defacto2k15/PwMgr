using System.Collections.Generic;
using Assets.ETerrain.Pyramid.Shape;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class HeightPyramidPerLevelConfiguration
    {
        public IntVector2 SegmentFillerStandByMarginsSize;
        public float BiggestShapeObjectInGroupLength;
        public int OneRingShapeObjectsCount =>  HeightPyramidLevelShapeGenerationConfiguration.OneRingShapeObjectsCount;
        public MyRectangle PyramidLevelWorldSize => (new MyRectangle(0, 0, BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2), BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2)))
            .SubRectangle(new MyRectangle(-0.5f, -0.5f, 1, 1));

        public float TransitionSingleStepPercent;
        public bool CreateCenterObject;

        public Dictionary<int, float> PerRingMergeWidths;
    }
}