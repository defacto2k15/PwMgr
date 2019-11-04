using System;
using Assets.Utils.Editor;
using UnityEngine;

namespace Assets.NPR.Lines
{
    // Non-OC version of CachedShaderBufferInjectOC
    public class CachedShaderBufferInjector : ShaderBufferInjector
    {
        private CachedBufferProvider _bufferProvider = new CachedBufferProvider();

        private ShaderBufferSE _shaderBuffer;
        private int _strideInFloats;
        private Func<bool> _isEnabledFunc;

        public CachedShaderBufferInjector(ShaderBufferSE shaderBuffer, int strideInFloats, Func<bool> isEnabledFunc, string bufferName, bool automaticReset, string objectName, EditorUpdate2GO editorUpdate2Go, Material material) 
            : base(automaticReset, bufferName, objectName, editorUpdate2Go, material) 
        {
            _shaderBuffer = shaderBuffer;
            _strideInFloats = strideInFloats;
            _isEnabledFunc = isEnabledFunc;
            UpdateVariables();
        }

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
            _bufferProvider.ShaderBuffer = _shaderBuffer;
            _bufferProvider.StrideInFloats = _strideInFloats;
        }

        protected override ComputeBuffer ProvideBuffer(bool forceReload)
        {
            return _bufferProvider.ProvideBuffer(forceReload);
        }

        protected override bool Enabled => _isEnabledFunc();

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