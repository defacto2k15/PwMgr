using System.Collections.Generic;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class EPyramidShaderBuffersGeneratorPerRingInput
    {
        public Dictionary<int, Vector2> RingUvRanges;
        public float CeilSliceWorldSize;
        public Dictionary<int, Vector2> HeightMergeRanges;
        public int FloorTextureResolution;
    }
}