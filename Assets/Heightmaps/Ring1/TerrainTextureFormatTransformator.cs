using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class TerrainTextureFormatTransformator
    {
        private CommonExecutorUTProxy _commonExecutor;

        public TerrainTextureFormatTransformator(CommonExecutorUTProxy commonExecutor)
        {
            _commonExecutor = commonExecutor;
        }

        public async Task<RenderTexture> EncodedHeightTextureToPlainAsync(TextureWithSize encodedTexture)
        {
            return await _commonExecutor.AddAction(() => { return EncodedHeightTextureToPlain(encodedTexture); });
        }

        public RenderTexture EncodedHeightTextureToPlain(TextureWithSize encodedTexture)
        {
            var renderMaterial = new Material(Shader.Find("Custom/TerGen/RgbaToRFloat"));
            renderMaterial.SetTexture("_SourceTexture", encodedTexture.Texture);
            var textureSize = encodedTexture.Size;
            var renderTextureInfo = new RenderTextureInfo(textureSize.X, textureSize.Y, RenderTextureFormat.RFloat,true);

            var tex = UltraTextureRenderer.CreateRenderTexture(renderMaterial, renderTextureInfo);
            return tex;
        }

        public async Task<Texture2D> PlainToEncodedHeightTextureAsync(TextureWithSize plainTexture)
        {
            return await _commonExecutor.AddAction(() =>
            {
                var renderMaterial = new Material(Shader.Find("Custom/TerGen/RFloatToRgba"));
                renderMaterial.SetTexture("_SourceTexture", plainTexture.Texture);
                var textureSize = plainTexture.Size;
                var renderTextureInfo = new RenderTextureInfo(textureSize.X, textureSize.Y, RenderTextureFormat.ARGB32);
                ConventionalTextureInfo outTextureinfo =
                    new ConventionalTextureInfo(textureSize.X, textureSize.Y, TextureFormat.ARGB32, false);
                return UltraTextureRenderer.RenderTextureAtOnce(renderMaterial, renderTextureInfo, outTextureinfo);
            });
        }

        public Texture MirrorHeightTexture(TextureWithSize textureWithSize)
        {
            var renderMaterial = new Material(Shader.Find("Custom/TerGen/MirrorHeightTexture"));
            renderMaterial.SetTexture("_SourceTexture", textureWithSize.Texture);
            var textureSize = textureWithSize.Size;
            return UltraTextureRenderer.CreateRenderTexture(renderMaterial,
                new RenderTextureInfo(textureSize.X, textureSize.Y, RenderTextureFormat.ARGB32, true));
        }

        public async Task<Texture> MirrorHeightTextureAsync(TextureWithSize textureWithSize)
        {
            return await _commonExecutor.AddAction(() =>
            {
                var renderMaterial = new Material(Shader.Find("Custom/TerGen/MirrorHeightTexture"));
                renderMaterial.SetTexture("_SourceTexture", textureWithSize.Texture);
                var textureSize = textureWithSize.Size;
                return UltraTextureRenderer.CreateRenderTexture(renderMaterial,
                    new RenderTextureInfo(textureSize.X, textureSize.Y, RenderTextureFormat.ARGB32, true));
            });
        }
    }
}