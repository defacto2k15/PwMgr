using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public class LegacyHeightPyramidMapConfiguration
    {
        public int HeightPyramidLevelsCount { get; set; }
        public IntVector2 SlotMapSize { get; set; }
        public IntVector2 SegmentTextureResolution { get; set; }
        public float InterSegmentMarginSize { get; set; }
        public RenderTextureFormat HeightTextureFormat { get; set; }

        public IntVector2 CeilTextureSize => SlotMapSize * SegmentTextureResolution;
    }
}