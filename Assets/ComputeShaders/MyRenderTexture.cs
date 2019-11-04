using Assets.Utils;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class MyRenderTexture
    {
        private readonly string _textureName;
        private Texture _passedTexture;

        public MyRenderTexture(string textureName, RenderTexture renderTexture, bool enableRandomWrite,
            bool create = true)
        {
            _textureName = textureName;
            _passedTexture = renderTexture;
            renderTexture.enableRandomWrite = enableRandomWrite;
            if (create)
            {
                renderTexture.Create();
            }
        }

        public MyRenderTexture(string textureName, Texture texture)
        {
            _textureName = textureName;
            _passedTexture = texture;
        }

        public string TextureName
        {
            get { return _textureName; }
        }

        public Texture Texture => _passedTexture;

        public RenderTexture AsRenderTexture
        {
            get
            {
                Preconditions.Assert(_passedTexture is RenderTexture, "Passed textrue is not RenderTexture");
                return _passedTexture as RenderTexture;
            }
        }
    }
}