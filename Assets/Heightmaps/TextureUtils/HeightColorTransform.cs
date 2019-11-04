using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Heightmaps.TextureUtils
{
    public static class HeightColorTransform
    {
        public static Color EncodeHeight(float height)
        {
            //Preconditions.Assert(height <= 1, "Height should be normalized");
            return new Color(
                (float) (Math.Floor(height * 256) / 256), Mathf.Repeat(height * 256, 1),
                Mathf.Repeat(height * 256 * 256, 1), Mathf.Repeat(height * 256 * 256 * 256, 1));
        }

        public static float DecodeHeight(Color input)
        {
            float outValue = input.r + input.g / 256 + input.b / (256 * 256) + input.a / (256 * 256 * 256);
            return Mathf.Clamp(outValue, 0, 1);
        }
    }
}