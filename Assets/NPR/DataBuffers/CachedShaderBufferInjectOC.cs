using UnityEngine;

namespace Assets.NPR.Lines
{
    [ExecuteInEditMode]
    public class CachedShaderBufferInjectOC : ShaderBufferInjectOC
    {
        private CachedBufferProvider _bufferProvider = new CachedBufferProvider();

        public ShaderBufferSE ShaderBuffer;
        public int StrideInFloats;

        public new void Start()
        {
            base.Start();
        }

        public void Update()
        {
            UpdateVariables();
        }

        private void UpdateVariables()
        {
            _bufferProvider.ShaderBuffer = ShaderBuffer;
            _bufferProvider.StrideInFloats = StrideInFloats;
        }

        protected override ComputeBuffer ProvideBuffer(bool forceReload)
        {
            return _bufferProvider.ProvideBuffer(forceReload);
        }

        public new void OnValidate()
        {
            UpdateVariables();
            base.OnValidate();
        }

        public new void OnEnable()
        {
            UpdateVariables();
            base.OnEnable();
        }
    }
}