using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils.Textures
{
    public class TextureWithSize
    {
        public Texture Texture;
        public IntVector2 Size;

        public static TextureWithSize FromTex2D(Texture2D tex2D)
        {
            return new TextureWithSize()
            {
                Size = new IntVector2(tex2D.width, tex2D.height),
                Texture = tex2D
            };
        }
    }
}