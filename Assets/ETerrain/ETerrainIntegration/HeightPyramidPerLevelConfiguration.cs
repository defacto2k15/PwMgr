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
        public Vector2 CeilTextureWorldSize => new Vector2( BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2), BiggestShapeObjectInGroupLength * (OneRingShapeObjectsCount + 2));
        public MyRectangle CeilTextureZeroCenteredWorldArea  => MyRectangle.ZeroCenteredWithSize(CeilTextureWorldSize);

        public float TransitionSingleStepPercent;
        public bool CreateCenterObject;

        public Dictionary<int, float> PerRingMergeWidths;

        public IntVector2 CenterObjectMeshVertexLength = new IntVector2(240, 240);
        public IntVector2 RingObjectMeshVertexLength = new IntVector2(60, 60);
    }
}