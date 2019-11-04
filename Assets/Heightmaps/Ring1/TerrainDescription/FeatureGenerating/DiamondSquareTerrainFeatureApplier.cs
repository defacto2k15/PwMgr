using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random;
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
    public class DiamondSquareTerrainFeatureApplier : ITerrainFeatureApplier
    {
        private readonly RandomProviderGenerator _randomProviderGenerator;
        private CommonExecutorUTProxy _commonExecutor;
        private UTTextureRendererProxy _rendererProxy;
        private Dictionary<TerrainCardinalResolution, DiamondSquareTerrainFeatureApplierConfiguration> _configurations;

        public DiamondSquareTerrainFeatureApplier(RandomProviderGenerator randomProviderGenerator,
            CommonExecutorUTProxy commonExecutor, UTTextureRendererProxy rendererProxy,
            Dictionary<TerrainCardinalResolution, DiamondSquareTerrainFeatureApplierConfiguration> configurations)
        {
            _randomProviderGenerator = randomProviderGenerator;
            _commonExecutor = commonExecutor;
            _rendererProxy = rendererProxy;
            _configurations = configurations;
        }

        public async Task<TextureWithCoords> ApplyFeatureAsync(TextureWithCoords texture,
            TerrainCardinalResolution resolution, bool canMultistep)
        {
            var configuration = _configurations[resolution];
            var detailedHeightmapArray = await TaskUtils
                .RunInThreadPool(
                    () =>
                    {
                        var creator = new DiamondSquareCreator(_randomProviderGenerator.GetRandom());
                        var initialArray = creator.CreateDiamondSquareNoiseArray(
                            texture.TextureSize,
                            configuration.DiamondSquareWorkingArrayLength);
                        return initialArray;
                    });

            var detailedTexture = await _commonExecutor
                .AddAction(() => HeightmapUtils.CreateTextureFromHeightmap(detailedHeightmapArray));


            UniformsPack pack = new UniformsPack();
            pack.SetTexture("_Texture1", texture.Texture);
            pack.SetTexture("_Texture2", detailedTexture);
            pack.SetUniform("_Texture2Weight", configuration.DiamondSquareWeight);

            var renderCoords = new MyRectangle(0, 0, 1, 1);

            var outTextureSize = texture.TextureSize;

            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(outTextureSize.X, outTextureSize.Y, TextureFormat.ARGB32, true);
            TextureRenderingTemplate template = new TextureRenderingTemplate()
            {
                CanMultistep = canMultistep,
                Coords = renderCoords,
                OutTextureInfo = outTextureInfo,
                RenderTextureFormat = RenderTextureFormat.RFloat,
                ShaderName = "Custom/TerrainCreation/DiamondSquareTextureAddingPlain",
                UniformPack = pack,
                CreateTexture2D = false
            };

            return new TextureWithCoords(sizedTexture: new TextureWithSize()
            {
                Texture = await _rendererProxy.AddOrder(template),
                Size = texture.TextureSize
            }, coords: texture.Coords);
        }
    }

    public class DiamondSquareTerrainFeatureApplierConfiguration
    {
        public int DiamondSquareWorkingArrayLength = 32;
        public float DiamondSquareWeight = 0.2f;
    }
}