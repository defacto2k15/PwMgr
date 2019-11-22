using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.GroundTexture
{
    public class EGroundTexture
    {
        private RenderTexture _texture;
        private EGroundTextureType _textureType;

        public EGroundTexture(RenderTexture texture, EGroundTextureType textureType)
        {
            _texture = texture;
            _textureType = textureType;
        }

        public RenderTexture Texture => _texture;

        public string Name => _textureType.GetName();

        public EGroundTextureType TextureType => _textureType;
    }

    public enum EGroundTextureType
    {
        HeightMap, SurfaceTexture, NormalTexture
    }

    public static class EGroundTextureTypeUtils
    {
        public static string GetName(this EGroundTextureType type)
        {
            switch (type)
            {
                case EGroundTextureType.HeightMap:
                    return "HeightMap";
                case EGroundTextureType.SurfaceTexture:
                    return "SurfaceTexture";
                case EGroundTextureType.NormalTexture:
                    return "NormalTexture";
            }
            Preconditions.Fail("Not supproted texture type "+type);
            return "ERROR E951";
        }
    }
}
