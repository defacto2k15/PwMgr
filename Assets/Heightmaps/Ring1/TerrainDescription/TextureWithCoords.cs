using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TextureWithCoords
    {
        private TextureWithSize _sizedTexture;
        private MyRectangle _coords;

        public TextureWithCoords(TextureWithSize sizedTexture, MyRectangle coords)
        {
            _sizedTexture = sizedTexture;
            _coords = coords;
        }

        public Texture Texture
        {
            get { return _sizedTexture.Texture; }
        }

        public MyRectangle Coords
        {
            get { return _coords; }
        }

        public IntVector2 TextureSize
        {
            get { return _sizedTexture.Size; }
        }
    }
}