using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.MT;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Utils.Textures
{
    public class TextureConcieverUTProxy : BaseUTTransformProxy<Texture, TextureConcieverUTProxy.TextureConcievingOrder>
    {
        //toko join with StainResourceCreator? Doint similar thing!

        public async Task<Texture2D> ConcieveTextureAsync(MyTextureTemplate template)
        {
            var texture = await BaseUtAddOrder(new TextureConcievingOrder(template));
            return (Texture2D) texture;
        }

        public async Task<RenderTexture> ConcieveRenderTextureAsync(MyRenderTextureTemplate template)
        {
            var texture = await BaseUtAddOrder(new TextureConcievingOrder(template));
            return (RenderTexture) texture;
        }

        protected override Texture ExecuteOrder(TextureConcievingOrder template)
        {
            if (template.StandardTextureTemplate != null)
            {
                return UnityThreadTextureConciever.Concieve(template.StandardTextureTemplate);
            }
            else
            {
                return UnityThreadTextureConciever.Concieve(template.RenderTextureTemplate);
            }
        }

        public class TextureConcievingOrder
        {
            private MyTextureTemplate _standardTextureTemplate;
            private MyRenderTextureTemplate _renderTextureTemplate;

            public TextureConcievingOrder(MyTextureTemplate standardTextureTemplate)
            {
                _standardTextureTemplate = standardTextureTemplate;
            }

            public TextureConcievingOrder(MyRenderTextureTemplate renderTextureTemplate)
            {
                _renderTextureTemplate = renderTextureTemplate;
            }

            public MyTextureTemplate StandardTextureTemplate => _standardTextureTemplate;

            public MyRenderTextureTemplate RenderTextureTemplate => _renderTextureTemplate;
        }
    }

    public class UnityThreadTextureConciever
    {
        public static Texture2D Concieve(MyTextureTemplate template)
        {
            var texture = new Texture2D(template.Width, template.Height, template.Format, template.Mipmap);
            texture.filterMode = template.FilterMode;
            texture.SetPixels(template.Array);
            if (template.wrapMode != null)
            {
                texture.wrapMode = template.wrapMode.Value;
            }
            texture.Apply();
            return texture;
        }

        public static RenderTexture Concieve(MyRenderTextureTemplate template)
        {
            var texture = new RenderTexture(template.Width, template.Height, 0, template.Format);
            if (template.Mipmap)
            {
                texture.useMipMap = true;
                texture.autoGenerateMips = true;
            }
            texture.filterMode = template.FilterMode;
            if (template.wrapMode != null)
            {
                texture.wrapMode = template.wrapMode.Value;
            }
            texture.Create();

            if (template.SourceTexture != null)
            {
                Graphics.CopyTexture(template.SourceTexture, texture);
            }
            return texture;
        }
    }
}