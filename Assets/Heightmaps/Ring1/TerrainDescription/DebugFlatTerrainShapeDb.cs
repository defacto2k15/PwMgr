using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class DebugFlatTerrainShapeDb : ITerrainShapeDb
    {
        private TextureWithSize _blankTexture;
        private TextureWithSize _normalTexture;

        public DebugFlatTerrainShapeDb(UTTextureRendererProxy textureRenderer)
        {
            var size = new IntVector2(241, 241);
            var tex = new Texture2D(size.X, size.Y, TextureFormat.ARGB32, true, true);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();

            _blankTexture = new TextureWithSize()
            {
                Texture = tex,
                Size = size
            };

            var pack = new UniformsPack();
            pack.SetTexture("_HeightmapTex", tex);
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
                if (requestedType == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
                {
                    outDict.Add(requestedType, new TerrainDetailElementOutput()
                    {
                        TokenizedElement = new TokenizedTerrainDetailElement()
                        {
                            DetailElement = new TerrainDetailElement()
                            {
                                DetailArea = query.QueryArea,
                                Resolution = requestedDetail.Resolution,
                                Texture = _blankTexture
                            },
                            Token = new TerrainDetailElementToken( new InternalTerrainDetailElementToken(null, null, requestedType), CornersMergeStatus.NOT_MERGED)
                        },
                        UvBase = new MyRectangle(0, 0, 1, 1)
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
                                DetailArea = query.QueryArea,
                                Resolution = requestedDetail.Resolution,
                                Texture = _normalTexture
                            },
                            Token = new TerrainDetailElementToken( new InternalTerrainDetailElementToken(null, null, requestedType), CornersMergeStatus.NOT_MERGED)
                        },
                        UvBase = new MyRectangle(0, 0, 1, 1)
                    });
                }
            }

            return TaskUtils.MyFromResult(new TerrainDescriptionOutput(outDict));
        }

        public Task DisposeTerrainDetailElement(TerrainDetailElementToken token)
        {
            return TaskUtils.EmptyCompleted();
        }
    }
}