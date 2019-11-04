using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public static class NormalUtils
    {
        public static Texture2D CreateTextureFromNormalArray(NormalArray normalArray)
        {
            var inputTexture = new Texture2D(normalArray.Width, normalArray.Height, TextureFormat.RGB24, false);
            byte[] rawTextureArray = new byte[normalArray.Width * normalArray.Height * 3];
            for (int y = 0; y < normalArray.Height; y++)
            {
                for (int x = 0; x < normalArray.Width; x++)
                {
                    Vector3 normal = EncodeNormal(normalArray.NormalsAsArray[x, y]);
                    var idx = 3 * (y * normalArray.Height + x);
                    rawTextureArray[idx] = (byte) (normal.x * 255);
                    rawTextureArray[idx + 1] = (byte) (normal.y * 255);
                    rawTextureArray[idx + 2] = (byte) (normal.z * 255);
                }
            }

            inputTexture.LoadRawTextureData(rawTextureArray);
            inputTexture.Apply();
            return inputTexture;
        }

        private static Vector3 EncodeNormal(Vector3 input)
        {
            return (input + Vector3.one) / 2;
        }
    }
}