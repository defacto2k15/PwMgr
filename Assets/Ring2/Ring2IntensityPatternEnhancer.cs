using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Ring2
{
    public class Ring2IntensityPatternEnhancer
    {
        private UTTextureRendererProxy _textureRenderer;
        private int _sizeMultiplier;

        public Ring2IntensityPatternEnhancer(UTTextureRendererProxy textureRenderer, int sizeMultiplier)
        {
            _textureRenderer = textureRenderer;
            _sizeMultiplier = sizeMultiplier;
        }

        public async Task<Texture> EnhanceIntensityPatternAsync(Texture intensityTexture,
            IntVector2 intensityTextureSize, MyRectangle coords)
        {
            var newSize = intensityTextureSize * _sizeMultiplier;
            var newTextureTemplate = new MyTextureTemplate(newSize.X, newSize.Y, TextureFormat.ARGB32, false,
                FilterMode.Trilinear);
            newTextureTemplate.wrapMode = TextureWrapMode.Clamp;

            ConventionalTextureInfo outTextureInfo =
                new ConventionalTextureInfo(newSize.X, newSize.Y, TextureFormat.ARGB32, false);
            UniformsPack pack = new UniformsPack();
            pack.SetTexture("_OriginalControlTex", intensityTexture);
            pack.SetUniform("_Coords", coords.ToVector4());

            var toReturn = await _textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                Coords = new MyRectangle(0, 0, 1, 1),
                CanMultistep = true,
                CreateTexture2D = false,
                OutTextureInfo = outTextureInfo,
                RenderTextureFormat = RenderTextureFormat.ARGB32,
                UniformPack = pack,
                ShaderName = "Custom/Misc/Ring2IntensityTextureEnhancer"
            });

            return toReturn;
        }
    }
}