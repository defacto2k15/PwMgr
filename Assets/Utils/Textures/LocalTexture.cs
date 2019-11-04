using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils.Textures
{
    public class LocalTexture
    {
        private readonly int _width;
        private readonly int _height;
        private readonly Color[] _colors;

        private LocalTexture(int width, int height, Color[] colors)
        {
            _width = width;
            _height = height;
            _colors = colors;
        }

        public Color GetPixel(int x, int y)
        {
            return _colors[x + y * _width];
        }

        public static LocalTexture FromTexture2D(Texture2D tex)
        {
            return new LocalTexture(tex.width, tex.height, tex.GetPixels());
        }

        public Texture2D ToTexture2D()
        {
            var tex = new Texture2D(_width,_height, TextureFormat.ARGB32,false);
            tex.SetPixels(_colors);
            tex.Apply();
            return tex;
        }

        public int Width => _width;

        public int Height => _height;
    }
}
