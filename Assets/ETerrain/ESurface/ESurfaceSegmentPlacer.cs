using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public class ESurfaceSegmentPlacer : IGroundTextureSegmentPlacer
    {
        private UTTextureRendererProxy _renderer;
        private RenderTexture _floorTextureArray;
        private readonly int _ceilArraySliceIndex;
        private IntVector2 _floorSlotsCount;
        private IntVector2 _floorTextureSize;

        public ESurfaceSegmentPlacer(UTTextureRendererProxy renderer, RenderTexture floorTextureArray
            , int ceilArraySliceIndex, IntVector2 floorSlotsCount, IntVector2 floorTextureSize)
        {
            _renderer = renderer;
            _floorTextureArray = floorTextureArray;
            _ceilArraySliceIndex = ceilArraySliceIndex;
            _floorSlotsCount = floorSlotsCount;
            _floorTextureSize = floorTextureSize;
        }

        public Task PlaceSegmentAsync(Texture segmentTexture, PlacementDetails placementDetails)
        {
            var segmentPlacement0 = CalculateSegmentPlacement(placementDetails.ModuledPositionInGrid);
            UniformsPack uniforms = new UniformsPack();
            uniforms.SetTexture("_SegmentSurfaceTexture", segmentTexture);
            uniforms.SetUniform("_SegmentCoords", segmentPlacement0.Uvs.ToVector4());
            uniforms.SetUniform("_TextureArraySliceIndex", _ceilArraySliceIndex);

            return _renderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                CreateTexture2D = false,
                RenderTextureToModify = _floorTextureArray,
                ShaderName = "Custom/ESurface/SegmentPlacer",
                UniformPack = uniforms,
                RenderingRectangle = segmentPlacement0.Pixels,
                RenderTargetSize = _floorTextureSize,
                RenderTextureArraySlice = _ceilArraySliceIndex
            });
        }

        private SegmentInTexturePlacement CalculateSegmentPlacement(IntVector2 newSegmentPositionInGrid)
        {
            var uvs = RectangleUtils.CalculateSubelementUv(
                new MyRectangle(0, 0, _floorSlotsCount.X, _floorSlotsCount.Y),
                new MyRectangle(newSegmentPositionInGrid.X, newSegmentPositionInGrid.Y, 1, 1));

            var segmentPixels = new IntRectangle(
                Mathf.RoundToInt(uvs.X * _floorTextureSize.X),
                Mathf.RoundToInt(uvs.Y * _floorTextureSize.Y),
                Mathf.RoundToInt(uvs.Width * _floorTextureSize.X),
                Mathf.RoundToInt(uvs.Height * _floorTextureSize.Y)
            );
            return new SegmentInTexturePlacement()
            {
                Pixels = segmentPixels,
                Uvs = uvs
            };
        }

        public class CornerMaskInformation
        {
            public Vector4 CornerToWeldVector;
            public MyRectangle SegmentSubPositionUv;
        }
    }
}