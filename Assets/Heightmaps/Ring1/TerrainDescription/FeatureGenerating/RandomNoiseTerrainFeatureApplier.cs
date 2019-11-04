using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class RandomNoiseTerrainFeatureApplier : ITerrainFeatureApplier
    {
        private UTTextureRendererProxy _rendererProxy;
        private CommonExecutorUTProxy _commonExecutor;
        private Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration> _configurations;

        public RandomNoiseTerrainFeatureApplier(UTTextureRendererProxy rendererProxy,
            CommonExecutorUTProxy commonExecutor,
            Dictionary<TerrainCardinalResolution, RandomNoiseTerrainFeatureApplierConfiguration> configurations)
        {
            _rendererProxy = rendererProxy;
            _commonExecutor = commonExecutor;
            _configurations = configurations;
        }

        public async Task<TextureWithCoords> ApplyFeatureAsync(TextureWithCoords inputTexture,
            TerrainCardinalResolution resolution, bool CanMultistep = false)
        {
            if (!TaskUtils.GetGlobalMultithreading())
            {
                Preconditions.Assert(inputTexture.Texture.width == inputTexture.Texture.height,
                    "Only square inputTextures are supported");
            }
            UniformsPack pack = new UniformsPack();
            await _commonExecutor.AddAction(() => inputTexture.Texture.filterMode = FilterMode.Point);
            pack.SetTexture("_SourceTexture", inputTexture.Texture);
            pack.SetUniform("_InputGlobalCoords", inputTexture.Coords.ToVector4());
            pack.SetUniform("_QuantingResolution", inputTexture.TextureSize.X - 1);


            var configuration = _configurations[resolution];
            pack.SetUniform("_DetailResolutionMultiplier", configuration.DetailResolutionMultiplier);
            pack.SetUniform("_NoiseStrengthMultiplier", configuration.NoiseStrengthMultiplier);

            var renderCoords = new MyRectangle(0, 0, 1, 1);

            var outTextureSize = inputTexture.TextureSize;

            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(outTextureSize.X, outTextureSize.Y, TextureFormat.ARGB32, true);
            TextureRenderingTemplate template = new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = renderCoords,
                OutTextureInfo = outTextureInfo,
                RenderTextureFormat = RenderTextureFormat.RFloat,
                ShaderName = "Custom/TerrainCreation/NoiseAddingPlain",
                UniformPack = pack,
                CreateTexture2D = false
            };
            return new TextureWithCoords(sizedTexture: new TextureWithSize()
            {
                Texture = await _rendererProxy.AddOrder(template),
                Size = inputTexture.TextureSize
            }, coords: inputTexture.Coords);
        }
    }

    public class RandomNoiseTerrainFeatureApplierConfiguration
    {
        public float DetailResolutionMultiplier = 1;
        public float NoiseStrengthMultiplier = 1;
    }
}