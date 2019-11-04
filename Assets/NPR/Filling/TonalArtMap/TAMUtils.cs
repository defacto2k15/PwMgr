using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Assets.NPRResources.TonalArtMap
{
    public static class TAMUtils
    {
        public static Texture2D ImageToTexture2D(Image img)
        {
            var tex = new Texture2D(img.Width, img.Height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            using (Bitmap b = new Bitmap(img))
            {
                for (int x = 0; x < b.Width; x++)
                {
                    for (int y = 0; y < b.Height; y++)
                    {
                        tex.SetPixel(x, y, ToUnityColor(b.GetPixel(x, y)));
                    }
                }
            }
            tex.Apply(false);

            return tex;
        }

        private static Color ToUnityColor(System.Drawing.Color netColor)
        {
            return new Color(netColor.R / 255f, netColor.G / 255f, netColor.B / 255f, netColor.A / 255f);
        }
    }
}