using UnityEngine;

namespace Assets.ShaderUtils
{
    public class UniformsAndComputeBuffersPack
    {
        private UniformsPack _uniformsPack;
        private ComputeBuffersPack _computeBuffersPack;

        public UniformsAndComputeBuffersPack(UniformsPack uniformsPack, ComputeBuffersPack computeBuffersPack)
        {
            _uniformsPack = uniformsPack;
            _computeBuffersPack = computeBuffersPack;
        }

        public void SetToMaterial(Material material)
        {
            _uniformsPack.SetUniformsToMaterial(material);
            _computeBuffersPack.SetBuffersToMaterial(material);
        }
    }
}