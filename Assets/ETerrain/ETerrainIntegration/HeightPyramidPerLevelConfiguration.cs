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
        public int OneRingShapeObjectsCount = 4; //todo
        public Vector2 FloorTextureWorldSize => new Vector2( BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2), BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2));
        public MyRectangle FloorTextureZeroCenteredWorldArea  => MyRectangle.ZeroCenteredWithSize(FloorTextureWorldSize);

        public float TransitionSingleStepPercent;
        public bool CreateCenterObject;

        public Dictionary<int, HeightPyramidPerRingConfiguration> PerRingConfigurations;

        public IntVector2 CenterObjectMeshVertexLength;
        public IntVector2 RingObjectMeshVertexLength;

        public int RingsCount;
    }

    public class HeightPyramidPerRingConfiguration
    {
        public float MergeWidth;
    }
}