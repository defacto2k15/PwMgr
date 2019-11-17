using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.ESurface
{
    public class MipmapExtractor
    {
        private UTTextureRendererProxy _textureRenderer;

        public MipmapExtractor(UTTextureRendererProxy textureRenderer)
        {
            _textureRenderer = textureRenderer;
        }

        public async Task<TextureWithSize> ExtractMipmapAsync(TextureWithSize inputTexture, RenderTextureFormat format, int mipmapLevelToExtract )
        {
            var pack = new UniformsPack();
            pack.SetTexture("_InputTexture", inputTexture.Texture);
            pack.SetUniform("_MipmapLevelToExtract", mipmapLevelToExtract);

            var outSize = ComputeMipmappedOutSize(inputTexture.Size, mipmapLevelToExtract);

            var newTexture = await _textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = true,
                Coords = new MyRectangle(0, 0, 1, 1),
                CreateTexture2D = false,
                OutTextureInfo = new ConventionalTextureInfo(outSize.X, outSize.Y, TextureFormat.ARGB32, true),
                RenderTextureFormat = format,
                RenderTextureMipMaps = true,
                ShaderName = "Custom/Tool/ExtractMipmap",
                UniformPack = pack,
            });
            return new TextureWithSize()
            {
                Texture = newTexture,
                Size = outSize
            };
        }

        private IntVector2 ComputeMipmappedOutSize(IntVector2 inputTextureSize, int mipmapLevelToExtract)
        {
            var divisor = Mathf.Pow(2f, mipmapLevelToExtract);
            return new IntVector2(Mathf.RoundToInt(inputTextureSize.X/divisor), Mathf.RoundToInt(inputTextureSize.Y/divisor));
        }
    }
}