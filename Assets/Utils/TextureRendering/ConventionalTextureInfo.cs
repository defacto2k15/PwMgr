using UnityEngine;

namespace Assets.Utils.TextureRendering
{
    public class ConventionalTextureInfo : TextureInfo
    {
        private TextureFormat _format;
        private bool _mipmaps;

        public ConventionalTextureInfo(int width, int height, TextureFormat format, bool mipmaps = false) : base(0, 0,
            width, height)
        {
            _format = format;
            _mipmaps = mipmaps;
        }

        public ConventionalTextureInfo(int x, int y, int width, int height, TextureFormat format,
            bool mipmaps) : base(x, y, width, height)
        {
            _format = format;
            _mipmaps = mipmaps;
        }

        public TextureFormat Format
        {
            get { return _format; }
        }

        public bool Mipmaps
        {
            get { return _mipmaps; }
        }
    }
}