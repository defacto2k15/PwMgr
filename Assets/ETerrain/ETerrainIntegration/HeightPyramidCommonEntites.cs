using System.Collections.Generic;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Shape;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class HeightPyramidCommonEntites
    {
        public HeightPyramidSegmentsUniformsSetter HeightmapUniformsSetter;
        public HeightPyramidGroupMover GroupMover;
        public HeightPyramidCommonConfiguration HeightPyramidMapConfiguration;
        public List<EGroundTexture> FloorTextureArrays;
    }
}