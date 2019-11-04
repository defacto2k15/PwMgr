using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ETerrain.Pyramid.Map;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Utils;

namespace Assets.ETerrain
{
    public static class ETerrainUtils
    {
        public static TerrainCardinalResolution HeightPyramidLevelToTerrainShapeDatabaseResolution(HeightPyramidLevel level)
        {
            switch (level)
            {
                case HeightPyramidLevel.Top:
                    return TerrainCardinalResolution.MAX_RESOLUTION;
                case HeightPyramidLevel.Mid:
                    return TerrainCardinalResolution.MID_RESOLUTION;
                case HeightPyramidLevel.Bottom:
                    return TerrainCardinalResolution.MIN_RESOLUTION;
            }
            Preconditions.Fail($"Unsupported HeightPyramidLevel {level}");
            return null;
        }
    }
}
