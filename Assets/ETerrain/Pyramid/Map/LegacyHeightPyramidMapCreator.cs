using Assets.Heightmaps.Ring1.RenderingTex;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    //public class LegacyHeightPyramidMapCreator //TODO delete
    //{
    //    public LegacyHeightPyramidMapEntites Create(UTTextureRendererProxy textureRendererProxy, LegacyHeightPyramidMapConfiguration pyramidMapConfiguration)
    //    {
    //        var ceilTexture =
    //            EGroundTextureGenerator.GenerateEmptyGroundTexture(pyramidMapConfiguration.CeilTextureSize, pyramidMapConfiguration.HeightTextureFormat);

    //        var modifiedCornerBuffer =
    //            EGroundTextureGenerator.GenerateModifiedCornerBuffer(pyramidMapConfiguration.SegmentTextureResolution,
    //                pyramidMapConfiguration.HeightTextureFormat);
    //        var segmentsPlacer = new HeightSegmentPlacer(textureRendererProxy, ceilTexture, pyramidMapConfiguration.SlotMapSize,
    //            pyramidMapConfiguration.CeilTextureSize, pyramidMapConfiguration.InterSegmentMarginSize, modifiedCornerBuffer);
    //        var heightMapManager = new LegacyHeightPyramidMapManager(segmentsPlacer, pyramidMapConfiguration);           

    //        return new LegacyHeightPyramidMapEntites()
    //        {
    //            MapManager = heightMapManager,
    //            SegmentPlacer = segmentsPlacer,
    //            CeilTexture = ceilTexture
    //        };
    //    }
    //}

    //public class LegacyHeightPyramidMapEntites
    //{
    //    public HeightSegmentPlacer SegmentPlacer;
    //    public LegacyHeightPyramidMapManager MapManager;
    //    public RenderTexture CeilTexture;
    //}
}
