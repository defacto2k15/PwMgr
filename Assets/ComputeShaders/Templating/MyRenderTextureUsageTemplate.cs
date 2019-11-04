using System.Collections.Generic;

namespace Assets.ComputeShaders.Templating
{
    public class MyRenderTextureUsageTemplate
    {
        private readonly string _textureName;
        private readonly MyComputeShaderTextureId _textureId;
        private readonly List<MyKernelHandle> _handles;

        public MyRenderTextureUsageTemplate(string textureName, MyComputeShaderTextureId textureId,
            List<MyKernelHandle> handles)
        {
            _textureName = textureName;
            _textureId = textureId;
            _handles = handles;
        }

        public string TextureName => _textureName;

        public MyComputeShaderTextureId TextureId => _textureId;

        public List<MyKernelHandle> Handles => _handles;
    }
}