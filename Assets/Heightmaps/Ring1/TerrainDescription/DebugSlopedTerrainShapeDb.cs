using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.TextureUtils;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class DebugSlopedTerrainShapeDb : ITerrainShapeDb
    {
        private TextureWithSize _blankTexture;
        private TextureWithSize _heightTexture;
        private TextureWithSize _normalTexture;

        public DebugSlopedTerrainShapeDb(UTTextureRendererProxy textureRenderer)
        {
            var size = new IntVector2(241, 241);
            var tex = new Texture2D(size.X, size.Y, TextureFormat.ARGB32, true, true);
            var encodedHeightTex = new Texture2D(size.X, size.Y, TextureFormat.ARGB32, true, true);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));

                    //var distanceToCenter = 1 - (Vector2.Distance(new Vector2(120, 120), new Vector2(x, y)) /
                    //                            Mathf.Sqrt(120 * 120)) / 2;
                    //encodedHeightTex.SetPixel(x, y, HeightColorTransform.EncodeHeight(distanceToCenter / 100));

                    //var distanceToCenter = Mathf.Clamp01(
                    //    Mathf.Min(
                    //        Mathf.Abs(y - 100) / 50.0f,
                    //        Mathf.Abs(x - 100) / 50.0f)
                    //        ) / 300;

                    //encodedHeightTex.SetPixel(x, y, HeightColorTransform.EncodeHeight(distanceToCenter));


                    var heightInUnits = HeightDenormalizer.Default.Normalize(5);
                    var encodedHeight = HeightColorTransform.EncodeHeight(heightInUnits);
                    encodedHeightTex.SetPixel(x, y, encodedHeight);
                }
            }
            tex.Apply();
            encodedHeightTex.Apply();

            _blankTexture = new TextureWithSize()
            {
                Texture = tex,
                Size = size
            };

            var transformator = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
            var encodedHeightTexture = new TextureWithSize()
            {
                Texture = encodedHeightTex,
                Size = new IntVector2(241, 241)
            };
            var plainTex = transformator.EncodedHeightTextureToPlain(encodedHeightTexture);
            _heightTexture = new TextureWithSize()
            {
                Size = new IntVector2(241, 241),
                Texture = plainTex
            };

            var pack = new UniformsPack();
            pack.SetTexture("_HeightmapTex", plainTex);
            pack.SetUniform("_HeightMultiplier", 80);
            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(size.X, size.Y, TextureFormat.ARGB32, true);

            var renderCoords = new MyRectangle(0, 0, 1, 1);
            TextureRenderingTemplate template = new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = renderCoords,
                OutTextureInfo = outTextureInfo,
                RenderTextureFormat = RenderTextureFormat.ARGB32,
                ShaderName = "Custom/Terrain/NormalmapGenerator",
                UniformPack = pack,
                CreateTexture2D = false
            };
            var outNormalTex = textureRenderer.AddOrder(template).Result;
            _normalTexture = new TextureWithSize()
            {
                Size = size,
                Texture = outNormalTex
            };
        }

        public Task<TerrainDescriptionOutput> Query(TerrainDescriptionQuery query)
        {
            var outDict = new Dictionary<TerrainDescriptionElementTypeEnum, TerrainDetailElementOutput>();

            foreach (var requestedDetail in query.RequestedElementDetails)
            {
                var requestedType = requestedDetail.Type;
                var alignedArea = RetriveAlignedArea(query.QueryArea.DownLeftPoint, requestedDetail.Resolution);
                var uv = RectangleUtils.CalculateSubelementUv(alignedArea, query.QueryArea);
                if (requestedType == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
                {
                    outDict.Add(requestedType, new TerrainDetailElementOutput()
                    {
                        TokenizedElement = new TokenizedTerrainDetailElement()
                        {
                            DetailElement = new TerrainDetailElement()
                            {
                                DetailArea = alignedArea,
                                Resolution = requestedDetail.Resolution,
                                Texture = _heightTexture
                            },
                            Token = new TerrainDetailElementToken( alignedArea,requestedDetail.Resolution, requestedType , CornersMergeStatus.NOT_MERGED)
                        },
                        UvBase = uv.ToRectangle()
                    });
                }
                else
                {
                    outDict.Add(requestedType, new TerrainDetailElementOutput()
                    {
                        TokenizedElement = new TokenizedTerrainDetailElement()
                        {
                            DetailElement = new TerrainDetailElement()
                            {
                                DetailArea = alignedArea,
                                Resolution = requestedDetail.Resolution,
                                Texture = _normalTexture
                            },
                            Token = new TerrainDetailElementToken(  alignedArea,requestedDetail.Resolution, requestedType  , CornersMergeStatus.NOT_MERGED)
                        },
                        UvBase = uv.ToRectangle()
                    });
                }
            }

            return TaskUtils.MyFromResult(new TerrainDescriptionOutput(outDict));
        }

        private MyRectangle GetAlignedUv(MyRectangle queryArea,
            TerrainCardinalResolution resolution)
        {
            float alignLength = TerrainDescriptionConstants.DetailCellSizesPerResolution[resolution];
            var alignedBox = new MyRectangle(
                Mathf.FloorToInt(queryArea.X / alignLength) * alignLength,
                Mathf.FloorToInt(queryArea.Y / alignLength) * alignLength,
                alignLength,
                alignLength
            );
            return RectangleUtils.CalculateSubelementUv(alignedBox, queryArea);
        }

        public Task DisposeTerrainDetailElement(TerrainDetailElementToken token)
        {
            return TaskUtils.EmptyCompleted();
        }

        private MyRectangle RetriveAlignedArea(Vector2 startPosition, TerrainCardinalResolution resolution)
        {
            var cellSize = TerrainDescriptionConstants.DetailCellSizesPerResolution[resolution];
            return new MyRectangle(
                Mathf.FloorToInt(startPosition.x / cellSize) * cellSize,
                Mathf.FloorToInt(startPosition.y / cellSize) * cellSize,
                cellSize,
                cellSize
            );
        }
    }
}