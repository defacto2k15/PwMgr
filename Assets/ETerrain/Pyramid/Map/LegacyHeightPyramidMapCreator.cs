using Assets.Heightmaps.Ring1.RenderingTex;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    //public class LegacyHeightPyramidMapCreator //TODO delete
    //{
    //    public LegacyHeightPyramidMapEntites Create(UTTextureRendererProxy textureRendererProxy, LegacyHeightPyramidMapConfiguration pyramidMapConfiguration)
    //    {
    //        var floorTexture =
    //            EGroundTextureGenerator.GenerateEmptyGroundTexture(pyramidMapConfiguration.FloorTextureSize, pyramidMapConfiguration.HeightTextureFormat);

    //        var modifiedCornerBuffer =
    //            EGroundTextureGenerator.GenerateModifiedCornerBuffer(pyramidMapConfiguration.SegmentTextureResolution,
    //                pyramidMapConfiguration.HeightTextureFormat);
    //        var segmentsPlacer = new HeightSegmentPlacer(textureRendererProxy, floorTexture, pyramidMapConfiguration.SlotMapSize,
    //            pyramidMapConfiguration.FloorTextureSize, pyramidMapConfiguration.InterSegmentMarginSize, modifiedCornerBuffer);
    //        var heightMapManager = new LegacyHeightPyramidMapManager(segmentsPlacer, pyramidMapConfiguration);           

    //        return new LegacyHeightPyramidMapEntites()
    //        {
    //            MapManager = heightMapManager,
    //            SegmentPlacer = segmentsPlacer,
    //            FloorTexture = floorTexture
    //        };
    //    }
    //}

    //public class LegacyHeightPyramidMapEntites
    //{
    //    public HeightSegmentPlacer SegmentPlacer;
    //    public LegacyHeightPyramidMapManager MapManager;
    //    public RenderTexture FloorTexture;
    //}
}
