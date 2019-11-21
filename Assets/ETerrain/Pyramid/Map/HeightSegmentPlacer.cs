using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public class HeightSegmentPlacer : IGroundTextureSegmentPlacer
    {
        private UTTextureRendererProxy _renderer;
        private readonly CommonExecutorUTProxy _commonExecutor;
        private RenderTexture _heightMap;
        private readonly int _heightMapSliceIndex;
        private readonly bool _modifyCorners;
        private IntVector2 _floorSlotsCount;
        private IntVector2 _floorTextureSize;
        private float _interSegmentMarginSize;
        private readonly IntVector2 _modifiedCornerBufferSize;

        private Dictionary<SegmentNeighbourhoodCorner, CornerMaskInformation> _cornerMaskInformations =
            new Dictionary<SegmentNeighbourhoodCorner, CornerMaskInformation>()
            {
                {
                    SegmentNeighbourhoodCorner.TopLeft, new CornerMaskInformation()
                    {
                        CornerToWeldVector = new Vector4(1, 0, 0, 0),
                        SegmentSubPositionUv = new MyRectangle(0, 0.5f, 0.5f, 0.5f)
                    }
                },
                {
                    SegmentNeighbourhoodCorner.TopRight, new CornerMaskInformation()
                    {
                        CornerToWeldVector = new Vector4(0, 1, 0, 0),
                        SegmentSubPositionUv = new MyRectangle(0.5f, 0.5f, 0.5f, 0.5f)
                    }
                },
                {
                    SegmentNeighbourhoodCorner.BottomRight, new CornerMaskInformation()
                    {
                        CornerToWeldVector = new Vector4(0, 0, 1, 0),
                        SegmentSubPositionUv = new MyRectangle(0.5f, 0.0f, 0.5f, 0.5f)
                    }
                },
                {
                    SegmentNeighbourhoodCorner.BottomLeft, new CornerMaskInformation()
                    {
                        CornerToWeldVector = new Vector4(0, 0, 0, 1),
                        SegmentSubPositionUv = new MyRectangle(0f, 0f, 0.5f, 0.5f)
                    }
                },
            };

        public HeightSegmentPlacer(UTTextureRendererProxy renderer, CommonExecutorUTProxy commonExecutor, RenderTexture heightMap, int heightMapSliceIndex,
            IntVector2 floorSlotsCount, IntVector2 floorTextureSize, float interSegmentMarginSize, IntVector2 modifiedCornerBufferSize,  bool modifyCorners = true)
        {
            _renderer = renderer;
            _commonExecutor = commonExecutor;
            _heightMap = heightMap;
            _heightMapSliceIndex = heightMapSliceIndex;
            _floorSlotsCount = floorSlotsCount;
            _floorTextureSize = floorTextureSize;
            _interSegmentMarginSize = interSegmentMarginSize;
            _modifiedCornerBufferSize = modifiedCornerBufferSize;
            _modifyCorners = modifyCorners;
        }

        public async Task PlaceSegmentAsync(Texture segmentTexture, PlacementDetails placementDetails)
        {
            var segmentPlacement0 = CalculateSegmentPlacement(placementDetails.ModuledPositionInGrid);
            UniformsPack uniforms0 = new UniformsPack();
            uniforms0.SetTexture("_SegmentHeightTexture", segmentTexture);
            uniforms0.SetUniform("_SegmentCoords", segmentPlacement0.Uvs.ToVector4());

            await _renderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                CreateTexture2D = false,
                RenderTextureToModify = _heightMap,
                ShaderName = "Custom/ETerrain/SegmentPlacer",
                UniformPack = uniforms0,
                RenderingRectangle = segmentPlacement0.Pixels,
                RenderTargetSize = _floorTextureSize,
                RenderTextureArraySlice = _heightMapSliceIndex
            });

            if (_modifyCorners)
            {
                var modifiedCornerBuffer = await _commonExecutor.AddAction( () =>
                {
                    var tex = new RenderTexture(_modifiedCornerBufferSize.X, _modifiedCornerBufferSize.Y, 0, _heightMap.format);
                    tex.Create();
                    return tex;
                });

                foreach (var cornerModification in placementDetails.CornersToModify)
                {
                    var cornerMask = _cornerMaskInformations[cornerModification.Corner];
                    var segmentPlacement1 = CalculateSegmentPlacement(cornerModification.ModuledPositionOfSegment);

                    var uniforms1 = new UniformsPack();
                    uniforms1.SetTexture("_HeightMap", _heightMap);
                    uniforms1.SetUniform("_WeldingAreaCoords",
                        RectangleUtils.CalculateSubPosition(segmentPlacement1.Uvs, cornerMask.SegmentSubPositionUv)
                            .ToVector4());
                    uniforms1.SetUniform("_MarginSize", _interSegmentMarginSize);
                    uniforms1.SetUniform("_CornerToWeld", cornerMask.CornerToWeldVector);
                    uniforms1.SetUniform("_PixelSizeInUv", 1f / _floorTextureSize.X);
                    uniforms1.SetUniform("_HeightMapSliceIndex", _heightMapSliceIndex);

                    await _renderer.AddOrder(new TextureRenderingTemplate()
                    {
                        CanMultistep = false,
                        CreateTexture2D = false,
                        RenderTextureToModify = modifiedCornerBuffer,
                        ShaderName = "Custom/ETerrain/GenerateNewCorner",
                        UniformPack = uniforms1,
                        RenderingRectangle = new IntRectangle(0, 0, segmentPlacement1.Pixels.Width, segmentPlacement1.Pixels.Height),
                        RenderTargetSize = new IntVector2(segmentPlacement1.Pixels.Width, segmentPlacement1.Pixels.Height)
                    });

                    var uniforms2 = new UniformsPack();
                    uniforms2.SetTexture("_ModifiedCornerBuffer", modifiedCornerBuffer);

                    await _renderer.AddOrder(new TextureRenderingTemplate()
                    {
                        CanMultistep = false,
                        CreateTexture2D = false,
                        RenderTextureToModify = _heightMap,
                        ShaderName = "Custom/ETerrain/CornerPlacer",
                        UniformPack = uniforms2,
                        RenderingRectangle = RectangleUtils.CalculateSubPosition(segmentPlacement1.Pixels.ToFloatRectangle(),
                            cornerMask.SegmentSubPositionUv).ToIntRectange(),
                        RenderTargetSize = _floorTextureSize,
                        RenderTextureArraySlice = _heightMapSliceIndex
                    });
                }

                await _commonExecutor.AddAction(() => GameObject.Destroy(modifiedCornerBuffer));
            }
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

    public class SegmentInTexturePlacement
    {
        public MyRectangle Uvs;
        public IntRectangle Pixels;
    }
}