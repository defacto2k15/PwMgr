using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
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
    public class TerrainDetailGenerator
    {
        private TerrainDetailGeneratorConfiguration _configuration;
        private UTTextureRendererProxy _rendererProxy;
        private TextureWithCoords _fullFundationTextureData;
        private List<RankedTerrainFeatureApplier> _featureAppliers;
        private CommonExecutorUTProxy _commonExecutor;
        private BaseTerrainDetailProvider _baseTerrainDetailProvider;

        public TerrainDetailGenerator(TerrainDetailGeneratorConfiguration configuration,
            UTTextureRendererProxy rendererProxy, TextureWithCoords fullFundationTextureData,
            List<RankedTerrainFeatureApplier> featureAppliers, CommonExecutorUTProxy commonExecutor)
        {
            _configuration = configuration;
            _rendererProxy = rendererProxy;
            _fullFundationTextureData = fullFundationTextureData;
            _featureAppliers = featureAppliers;
            _commonExecutor = commonExecutor;
        }

        public void SetBaseTerrainDetailProvider(BaseTerrainDetailProvider provider)
        {
            _baseTerrainDetailProvider = provider;
        }

        public async Task<TextureWithSize> GenerateHeightDetailElementAsync(MyRectangle requestedArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus)
        {
            var textureWithSize = await RetriveFoundationTextureAsync(requestedArea, resolution, cornersMergeStatus);
            var workTexture = new TextureWithCoords(sizedTexture: textureWithSize, coords: requestedArea);

            foreach (var applier in _featureAppliers.Where(c => c.AvalibleResolutions.Contains(resolution))
                .OrderBy(c => c.Rank))
            {
                workTexture = await applier.Applier.ApplyFeatureAsync(workTexture, resolution, false); //todo
                var localTexture = workTexture;
                await _commonExecutor.AddAction(() => localTexture.Texture.wrapMode = TextureWrapMode.Clamp);
            }

            var textureWithMipMaps = await GenerateTextureWithMipMapsAsync(new TextureWithSize()
            {
                Size = textureWithSize.Size,
                Texture = workTexture.Texture
            });

            return new TextureWithSize()
            {
                Size = textureWithSize.Size,
                Texture = textureWithMipMaps
            };
        }

        private async Task<Texture> GenerateTextureWithMipMapsAsync(TextureWithSize tex)
        {
            IntVector2 outTextureSize = tex.Size;
            UniformsPack pack = new UniformsPack();
            pack.SetTexture("_SourceTexture", tex.Texture);
            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(outTextureSize.X, outTextureSize.Y, TextureFormat.ARGB32, true);
            TextureRenderingTemplate template = new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = new MyRectangle(0, 0, 1, 1),
                OutTextureInfo = outTextureInfo,
                RenderTextureFormat = RenderTextureFormat.RFloat,
                ShaderName = "Custom/TerGen/Cutting",
                UniformPack = pack,
                CreateTexture2D = false,
                RenderTextureMipMaps = true
            };
            var outTex = await _rendererProxy.AddOrder(template);
            await _commonExecutor.AddAction(() => outTex.wrapMode = TextureWrapMode.Clamp);
            return outTex;
        }

        private async Task<TextureWithSize> RetriveFoundationTextureAsync(MyRectangle requestedArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus)
        {
            Texture fundationTexture = null;
            MyRectangle renderCoords = null;

            TerrainDetailElementToken retrivedElementToken = null;

            if (resolution == TerrainCardinalResolution.MIN_RESOLUTION)
            {
                fundationTexture = _fullFundationTextureData.Texture;
                renderCoords =
                    TerrainShapeUtils.ComputeUvOfSubElement(requestedArea, _fullFundationTextureData.Coords);
            }
            else
            {
                var lowerResolution = resolution.LowerResolution;
                var fundationQueryArea = TerrainShapeUtils.GetAlignedTerrainArea(requestedArea, lowerResolution,
                    _configuration.TerrainDetailImageSideDisjointResolution);

                var foundationOutput = await _baseTerrainDetailProvider.RetriveTerrainDetailAsync(
                    TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY, fundationQueryArea, lowerResolution, cornersMergeStatus);
                retrivedElementToken = foundationOutput.TokenizedElement.Token;
                var fundationDetailElement = foundationOutput.TokenizedElement.DetailElement;

                renderCoords =
                    TerrainShapeUtils.ComputeUvOfSubElement(requestedArea, fundationDetailElement.DetailArea);
                fundationTexture = fundationDetailElement.Texture.Texture;
                await _commonExecutor.AddAction(() => fundationTexture.filterMode = FilterMode.Bilinear);
            }

            IntVector2 outTextureSize = TerrainShapeUtils.RetriveTextureSize(requestedArea, resolution);
            UniformsPack pack = new UniformsPack();
            pack.SetTexture("_SourceTexture", fundationTexture);
            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(outTextureSize.X, outTextureSize.Y, TextureFormat.ARGB32, true);
            TextureRenderingTemplate template = new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = renderCoords,
                OutTextureInfo = outTextureInfo,
                RenderTextureFormat = RenderTextureFormat.RFloat,
                ShaderName = "Custom/TerGen/Cutting",
                UniformPack = pack,
                CreateTexture2D = false
            };
            var outTex = await _rendererProxy.AddOrder(template);
            await _commonExecutor.AddAction(() => outTex.wrapMode = TextureWrapMode.Clamp);

            if (retrivedElementToken != null)
            {
                await _baseTerrainDetailProvider.RemoveTerrainDetailAsync(retrivedElementToken);
            }

            return new TextureWithSize()
            {
                Size = outTextureSize,
                Texture = outTex
            };
        }

        public async Task<TextureWithSize> GenerateNormalDetailElementAsync(MyRectangle requestedArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus)
        {
            var baseHeightTextureOutput =
                await _baseTerrainDetailProvider.RetriveTerrainDetailAsync(
                    TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY, requestedArea, resolution, cornersMergeStatus);

            var baseHeightTexture = baseHeightTextureOutput.TokenizedElement.DetailElement;

            IntVector2 outTextureSize = TerrainShapeUtils.RetriveTextureSize(requestedArea, resolution);
            UniformsPack pack = new UniformsPack();

            float heightMultiplier = 0;
            if (resolution == TerrainCardinalResolution.MIN_RESOLUTION)
            {
                heightMultiplier = 1.25f;
            }
            else if (resolution == TerrainCardinalResolution.MID_RESOLUTION)
            {
                heightMultiplier = 10f;
            }
            else
            {
                heightMultiplier = 80f;
            }

            pack.SetTexture("_HeightmapTex", baseHeightTexture.Texture.Texture);
            //pack.SetUniform("_HeightMultiplier", heightMultiplier);
            pack.SetUniform("_HeightMultiplier", 1);
            pack.SetUniform("_GlobalCoords", requestedArea.ToVector4());
            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(outTextureSize.X, outTextureSize.Y, TextureFormat.ARGB32, true);

            var renderCoords =
                TerrainShapeUtils.ComputeUvOfSubElement(requestedArea, baseHeightTexture.DetailArea);

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
            var outTex = await _rendererProxy.AddOrder(template);
            await _commonExecutor.AddAction(() => outTex.filterMode = FilterMode.Trilinear);
            await _baseTerrainDetailProvider.RemoveTerrainDetailAsync(baseHeightTextureOutput.TokenizedElement.Token);

            //SavingFileManager.SaveTextureToPngFile(@"C:\temp\norm1.png", outTex as Texture2D);

            return new TextureWithSize()
            {
                Size = new IntVector2(outTextureSize.X, outTextureSize.Y),
                Texture = outTex
            };
        }
    }

    public class TerrainDetailGeneratorConfiguration
    {
        public int TerrainDetailImageSideDisjointResolution;
    }

    public abstract class BaseTerrainDetailProvider
    {
        public abstract Task<TerrainDetailElementOutput> RetriveTerrainDetailAsync(
            TerrainDescriptionElementTypeEnum type, MyRectangle queryArea, TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus);

        public abstract Task RemoveTerrainDetailAsync(TerrainDetailElementToken token);

        public static BaseTerrainDetailProvider CreateFrom(TerrainDetailProvider provider)
        {
            return new FromTerrainProviderBaseTerrainDetailProvider(provider);
        }

        public static BaseTerrainDetailProvider CreateFrom(TerrainShapeDb db)
        {
            return new FromTerrainDbBaseTerrainDetailProvider(db);
        }
    }

    public class FromTerrainProviderBaseTerrainDetailProvider : BaseTerrainDetailProvider
    {
        private TerrainDetailProvider _provider;

        public FromTerrainProviderBaseTerrainDetailProvider(TerrainDetailProvider provider)
        {
            _provider = provider;
        }

        public override async Task<TerrainDetailElementOutput> RetriveTerrainDetailAsync(
            TerrainDescriptionElementTypeEnum type, MyRectangle queryArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus)
        {
            TerrainDetailElement detailElement = null;
            if (type == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
            {
                detailElement = await _provider.GenerateHeightDetailElementAsync(queryArea, resolution, cornersMergeStatus);
            }
            else if (type == TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
            {
                detailElement = await _provider.GenerateNormalDetailElementAsync(queryArea, resolution, cornersMergeStatus);
            }
            else
            {
                Preconditions.Fail("Not supported type: " + type);
            }

            return new TerrainDetailElementOutput()
            {
                TokenizedElement = new TokenizedTerrainDetailElement()
                {
                    DetailElement = detailElement,
                    Token = null
                },
                UvBase = null
            };
        }

        public override Task RemoveTerrainDetailAsync(TerrainDetailElementToken token)
        {
            return TaskUtils.MyFromResult<object>(null);
        }
    }

    public class FromTerrainDbBaseTerrainDetailProvider : BaseTerrainDetailProvider
    {
        private TerrainShapeDb _db;

        public FromTerrainDbBaseTerrainDetailProvider(TerrainShapeDb db)
        {
            _db = db;
        }

        public override async Task<TerrainDetailElementOutput> RetriveTerrainDetailAsync(
            TerrainDescriptionElementTypeEnum type, MyRectangle queryArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus)
        {
            var queryOutput = await _db.QueryAsync(new TerrainDescriptionQuery()
            {
                QueryArea = queryArea,
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Resolution = resolution,
                        Type = type,
                        RequiredMergeStatus = cornersMergeStatus
                    }
                }
            });

            return queryOutput.GetElementOfType(type);
        }

        public override Task RemoveTerrainDetailAsync(TerrainDetailElementToken token)
        {
            return _db.RemoveTerrainDetailElementAsync(token);
        }
    }
}