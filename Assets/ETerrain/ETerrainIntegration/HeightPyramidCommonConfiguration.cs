﻿using System.Collections.Generic;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class HeightPyramidCommonConfiguration
    {
        public IntVector2 SlotMapSize { get; set; }
        public IntVector2 SegmentTextureResolution { get; set; }
        public float InterSegmentMarginSize { get; set; }
        public RenderTextureFormat HeightTextureFormat { get; set; }
        public RenderTextureFormat SurfaceTextureFormat { get; set; }

        public IntVector2 CeilTextureSize => SlotMapSize * SegmentTextureResolution;
        public float YScale { get; set; }

        public Dictionary<int, Vector2> RingsUvRange;

        public int MaxLevelsCount;
        public int MaxRingsPerLevelCount;
    }
}