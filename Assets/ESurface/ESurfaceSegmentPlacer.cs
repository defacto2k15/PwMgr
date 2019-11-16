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
        private RenderTexture _floorHeightTexture;
        private IntVector2 _floorSlotsCount;
        private IntVector2 _floorTextureSize;

        public ESurfaceSegmentPlacer(UTTextureRendererProxy renderer, RenderTexture floorHeightTexture, IntVector2 floorSlotsCount, IntVector2 floorTextureSize)
        {
            _renderer = renderer;
            _floorHeightTexture = floorHeightTexture;
            _floorSlotsCount = floorSlotsCount;
            _floorTextureSize = floorTextureSize;
        }

        public Task PlaceSegmentAsync(Texture segmentTexture, PlacementDetails placementDetails)
        {
            var segmentPlacement0 = CalculateSegmentPlacement(placementDetails.ModuledPositionInGrid);
            UniformsPack uniforms0 = new UniformsPack();
            uniforms0.SetTexture("_SegmentSurfaceTexture", segmentTexture);
            uniforms0.SetUniform("_SegmentCoords", segmentPlacement0.Uvs.ToVector4());

            return _renderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                CreateTexture2D = false,
                RenderTextureToModify = _floorHeightTexture,
                ShaderName = "Custom/ESurface/SegmentPlacer",
                UniformPack = uniforms0,
                RenderingRectangle = segmentPlacement0.Pixels,
                RenderTargetSize = _floorTextureSize
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