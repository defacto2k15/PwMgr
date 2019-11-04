using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2.Billboards
{
    public class Grass2BillboardGenerator
    {
        private UTTextureRendererProxy _textureRenderer;
        private Grass2BillboardGeneratorConfiguration _configuration;

        public Grass2BillboardGenerator(UTTextureRendererProxy textureRenderer,
            Grass2BillboardGeneratorConfiguration configuration)
        {
            _textureRenderer = textureRenderer;
            _configuration = configuration;
        }

        public async Task<Texture2D> GenerateBillboardImageAsync(int bladesCount, float seed)
        {
            UniformsPack uniforms = new UniformsPack();
            uniforms.SetUniform("_Seed", seed);
            uniforms.SetUniform("_BladesCount", bladesCount);

            return (await _textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = new MyRectangle(0, 0, 1, 1),
                CreateTexture2D = true,
                OutTextureInfo = new ConventionalTextureInfo(_configuration.BillboardSize.X,
                    _configuration.BillboardSize.Y, TextureFormat.ARGB32, true),
                Keywords = new ShaderKeywordSet(),
                RenderTextureFormat = RenderTextureFormat.ARGB32,
                RenderTextureMipMaps = false,
                ShaderName = "Custom/Precomputation/GrassBushBillboardGenerator",
                UniformPack = uniforms
            })) as Texture2D;
        }

        public class Grass2BillboardGeneratorConfiguration
        {
            public IntVector2 BillboardSize;
        }
    }
}