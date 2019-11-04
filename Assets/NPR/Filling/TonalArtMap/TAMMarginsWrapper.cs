using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMMarginsWrapper
    {
        private UTTextureRendererProxy _textureRenderer;
        private TAMMarginsWrapperConfiguration _configuration;

        public TAMMarginsWrapper(UTTextureRendererProxy textureRenderer, TAMMarginsWrapperConfiguration configuration)
        {
            _textureRenderer = textureRenderer;
            _configuration = configuration;
        }

        public Texture2D WrapTexture(Texture2D inputTexture)
        {
            UniformsPack uniforms = new UniformsPack();
            uniforms.SetUniform("_Margin", _configuration.Margin);
            uniforms.SetTexture("_InputTex", inputTexture);

            var outSize = (new Vector2(inputTexture.width, inputTexture.height) / (1+2*_configuration.Margin)).ToIntVector2();
            outSize = new IntVector2(Mathf.ClosestPowerOfTwo(outSize.X), Mathf.ClosestPowerOfTwo(outSize.Y));

            return (Texture2D)_textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                Coords = new MyRectangle(0, 0, 1, 1),
                CreateTexture2D = true,
                OutTextureInfo = new ConventionalTextureInfo(outSize.X, outSize.Y, inputTexture.format, false),
                UniformPack = uniforms,
                ShaderName = "Custom/NPR/ArtMapWrapping"
            }).Result; //todo async
        }
    }
}