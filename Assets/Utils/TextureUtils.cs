using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public static class TextureUtils
    {
        public static IntVector2 ToMySize(this Texture2D tex)
        {
            return new IntVector2(tex.width, tex.height);
        }

        public static void MirrorX(this Texture2D tex)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width / 2; x++)
                {
                    var c1 = tex.GetPixel(x, y);
                    var c2 = tex.GetPixel(tex.width - 1 - x, y);
                    tex.SetPixel(x, y, c2);
                    tex.SetPixel(tex.width - 1 - x, y, c1);
                }
            }
            tex.Apply(false);
        }

        public static Texture2D RetriveMipmapAsTexture(this Texture2D tex, int mipMapLevel)
        {
            var mipmapSize = new IntVector2((int) (tex.width / Mathf.Pow(2, mipMapLevel)), (int) (tex.height / Mathf.Pow(2, mipMapLevel)));
            var newTex = new Texture2D(mipmapSize.X, mipmapSize.Y, tex.format, false);
            newTex.SetPixels32(tex.GetPixels32(mipMapLevel));
            newTex.Apply();
            return newTex;
        }
    }
}