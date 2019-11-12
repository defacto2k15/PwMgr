using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ETerrain.ETerrainIntegration;
using Assets.ETerrain.Pyramid.Map;
using Assets.FinalExecution;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

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

        public static FlatLod HeightPyramidLevelToSurfaceTextureFlatLod(HeightPyramidLevel level)
        {
            switch (level)
            {
                case HeightPyramidLevel.Top:
                    return new FlatLod(0,0);
                case HeightPyramidLevel.Mid:
                    return new FlatLod(1,0);
                case HeightPyramidLevel.Bottom:
                    return new FlatLod(2,0);
            }
            Preconditions.Fail($"Unsupported HeightPyramidLevel {level}");
            return new FlatLod(-1,-1);
        }

        public static MyRectangle TerrainShapeSegmentAlignedPositionToWorldSpaceArea(HeightPyramidLevel level, HeightPyramidPerLevelConfiguration perLevelConfiguration,
            IntVector2 segmentAlignedPosition)
        {
            var segmentLength = perLevelConfiguration.BiggestShapeObjectInGroupLength;
            if (level == HeightPyramidLevel.Mid)
            {
                segmentAlignedPosition = segmentAlignedPosition + new IntVector2(-1, 0);
            }
            else if (level == HeightPyramidLevel.Top)
            {
                segmentAlignedPosition = segmentAlignedPosition + new IntVector2(-8, -4);
            }

            var surfaceWorldSpaceRectangle = new MyRectangle(segmentAlignedPosition.X * segmentLength, segmentAlignedPosition.Y * segmentLength
                , segmentLength, segmentLength);
            return surfaceWorldSpaceRectangle;
        }


        public static MyRectangle SurfaceTextureSegmentAlignedPositionToWorldSpaceArea(HeightPyramidLevel level, HeightPyramidPerLevelConfiguration perLevelConfiguration,
            IntVector2 segmentAlignedPosition)
        {
            var segmentLength = perLevelConfiguration.BiggestShapeObjectInGroupLength;
            var floatSegmentAlignedPosition = segmentAlignedPosition.ToFloatVec();
            if (level == HeightPyramidLevel.Mid)
            {
                floatSegmentAlignedPosition = floatSegmentAlignedPosition + new Vector2(-1, -0.5f);
            }
            else if (level == HeightPyramidLevel.Top)
            {
                floatSegmentAlignedPosition = floatSegmentAlignedPosition + new Vector2(-8, -4);
            }

            var surfaceWorldSpaceRectangle = new MyRectangle( floatSegmentAlignedPosition.x * segmentLength, floatSegmentAlignedPosition.y * segmentLength
                , segmentLength, segmentLength);
            return surfaceWorldSpaceRectangle;
        }


    }
}
