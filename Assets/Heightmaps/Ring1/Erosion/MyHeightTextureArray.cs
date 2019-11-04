using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class MyHeightTextureArray
    {
        private Texture2DArray _textureArray;
        private int _addedTexturesCount = 0;

        public MyHeightTextureArray(int width, int height, int depth, TextureFormat format, bool mipmap, bool linear)
        {
            _textureArray = new Texture2DArray(width, height, depth, format, mipmap, linear);
        }

        public void AddElementArray(HeightmapArray heightmap, int textureIndex)
        {
            var colorArray = HeightmapUtils.CreateHeightTextureArray(heightmap);
            _textureArray.SetPixels(colorArray, textureIndex);
            _addedTexturesCount++;
        }

        public void AddElementArray(HeightmapArray heightmap)
        {
            AddElementArray(heightmap, _addedTexturesCount);
        }

        public Texture2DArray ApplyAndRetrive()
        {
            _textureArray.Apply(false);
            return _textureArray;
        }
    }
}