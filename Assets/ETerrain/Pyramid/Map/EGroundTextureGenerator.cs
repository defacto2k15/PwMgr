using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Map
{
    public static class EGroundTextureGenerator
    {
        public static Texture GenerateDummyTextureArray(IntVector2 textureSize, int textureArrayDepth, TextureFormat textureArrayFormat)
        {
            var ceilHeightTexture = new Texture2DArray(textureSize.X, textureSize.Y,
                textureArrayDepth, textureArrayFormat, false, true);

            for (int layer = 0; layer < textureArrayDepth; layer++)
            {
                ceilHeightTexture.SetPixels(Enumerable
                    .Range(0, textureSize.X * textureSize.Y)
                    .Select(c => new Color(layer/(float)(textureArrayDepth-1), 0, 0, 0)).ToArray(), layer);
            }
            ceilHeightTexture.Apply();
            return ceilHeightTexture;
        }

        public static RenderTexture GenerateEmptyGroundTexture(IntVector2 textureSize, RenderTextureFormat textureFormat)
        {
            var texture = new RenderTexture(textureSize.X, textureSize.Y, 0, textureFormat, RenderTextureReadWrite.Linear);
            texture.enableRandomWrite = true;
            texture.useMipMap = true;
            texture.autoGenerateMips = true;
            texture.filterMode = FilterMode.Trilinear;
            return texture;
        }

        public static RenderTexture GenerateModifiedCornerBuffer(IntVector2 segmentTextureSize, RenderTextureFormat textureFormat)
        {
            var cornerBufferSize =  new IntVector2(segmentTextureSize.X / 2, segmentTextureSize.Y / 2);
            var texture = new RenderTexture(cornerBufferSize.X, cornerBufferSize.Y, 0, textureFormat, RenderTextureReadWrite.Linear);
            texture.enableRandomWrite = true;
            texture.useMipMap = true;
            texture.autoGenerateMips = true;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }
    }
}