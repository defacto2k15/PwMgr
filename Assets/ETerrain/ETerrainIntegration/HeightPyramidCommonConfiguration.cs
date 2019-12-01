using System.Collections.Generic;
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
        public RenderTextureFormat NormalTextureFormat { get; set; }
        public RenderTextureFormat SurfaceTextureFormat { get; set; }

        public IntVector2 FloorTextureSize => SlotMapSize * SegmentTextureResolution;
        public float YScale { get; set; }
        public bool MergeShapesOfRings { get; set; }

        public bool ModifyCornersInHeightSegmentPlacer; 

        public Dictionary<int, Vector2> RingsUvRange;

        public int MaxLevelsCount;
        public int MaxRingsPerLevelCount;

        public bool UseNormalTextures;
        public bool MergeSegmentsInFloorTexture;
    }
}