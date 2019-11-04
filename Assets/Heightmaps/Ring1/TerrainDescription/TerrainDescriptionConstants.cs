using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public static class TerrainDescriptionConstants
    {
        public static readonly Dictionary<TerrainCardinalResolution, float> DetailCellSizesPerResolution =
            new Dictionary<TerrainCardinalResolution, float>()
            {
                {TerrainCardinalResolution.MAX_RESOLUTION, 90},
                {TerrainCardinalResolution.MID_RESOLUTION, 90 * 8},
                {TerrainCardinalResolution.MIN_RESOLUTION, 90 * 8 * 8},
            };
    }
}