using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = System.Drawing.Color;

namespace Assets.Utils
{
    public static class SystemDrawingUtilsX
    {
        public static Bitmap Texture2DToBitmapRGBA(Texture2D tex)
        {
            var b = new Bitmap(tex.width, tex.height, PixelFormat.Format32bppPArgb);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    b.SetPixel(x,y, UnityColorToSystemColor(tex.GetPixel(x,y))); 
                }
            }

            return b;
        }

        public static Texture2D BitmapToTexture2DRGBA(Bitmap b)
        {
            var tex = new Texture2D(b.Size.Width, b.Size.Height, TextureFormat.ARGB32, false);
            for (int x = 0; x <b.Size.Width; x++)
            {
                for (int y = 0; y < b.Size.Height; y++)
                {
                    tex.SetPixel(x,y, SystemColorToUnityColor(b.GetPixel(x,y)));
                }
            }
            tex.Apply();
            return tex;
        }

        public static UnityEngine.Color SystemColorToUnityColor(Color systemColor)
        {
            return new UnityEngine.Color(
                systemColor.R / 255f,
                systemColor.G / 255f,
                systemColor.B / 255f,
                systemColor.A / 255f
            );
        }

        public static Color UnityColorToSystemColor(UnityEngine.Color unityColor)
        {
            return Color.FromArgb(
                Mathf.RoundToInt(unityColor.a * 255f),
                Mathf.RoundToInt(unityColor.r * 255f),
                Mathf.RoundToInt(unityColor.g * 255f),
                Mathf.RoundToInt(unityColor.b * 255f)
            );
        }
    }
}
