using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.ComputeShaders.Templating
{
    public class MyComputeShaderTextureTemplate
    {
        public IntVector2 Size;
        public int Depth;
        public RenderTextureFormat Format;
        public bool EnableReadWrite = false;
        public TextureWrapMode TexWrapMode = TextureWrapMode.Repeat;
        public TextureDimension? Dimension = null;
        public int? VolumeDepth = null;
    }
}