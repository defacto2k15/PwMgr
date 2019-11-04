using UnityEngine;

namespace Assets.Utils.TextureRendering
{
    public class RenderTextureInfo : TextureInfo
    {
        private RenderTextureFormat _format;
        private bool _useMipMaps;

        public RenderTextureInfo(int width, int height, RenderTextureFormat format, bool useMipMaps = true) : base(0, 0,
            width, height)
        {
            this._format = format;
            _useMipMaps = useMipMaps;
        }

        public RenderTextureFormat Format
        {
            get { return _format; }
        }

        public bool UseMipMaps => _useMipMaps;
    }
}