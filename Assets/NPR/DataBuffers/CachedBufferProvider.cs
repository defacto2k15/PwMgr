using System.Linq;
using UnityEngine;

namespace Assets.NPR.Lines
{
    public class CachedBufferProvider : ShaderBufferProvider
    {
        public ShaderBufferSE ShaderBuffer;
        public int StrideInFloats;

        private ShaderBufferSE _lastShaderBufferSe;
        private ComputeBuffer _lastProviderBuffer;

        public override ComputeBuffer ProvideBuffer(bool forceReload)
        {
            if (ShaderBuffer != _lastShaderBufferSe || forceReload)
            {
                _lastShaderBufferSe = ShaderBuffer;
                _lastProviderBuffer?.Release();
                _lastProviderBuffer?.Dispose();

                //ShaderBuffer.Data = Enumerable.Range(0, ShaderBuffer.Data.Length).Select(c => 1f).ToArray();
                _lastProviderBuffer = new ComputeBuffer(ShaderBuffer.Data.Length / StrideInFloats, sizeof(float) * StrideInFloats, ComputeBufferType.Default);

                _lastProviderBuffer.SetData(_lastShaderBufferSe.Data);
            }
            return _lastProviderBuffer;
        }
    }
}