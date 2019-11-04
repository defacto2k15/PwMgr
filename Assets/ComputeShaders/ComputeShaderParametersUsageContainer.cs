using System.Collections.Generic;
using Assets.ComputeShaders.Templating;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class ComputeShaderParametersUsageContainer
    {
        private List<MyComputeBufferUsageTemplate> _bufferUsages = new List<MyComputeBufferUsageTemplate>();
        private List<MyRenderTextureUsageTemplate> _textureUsages = new List<MyRenderTextureUsageTemplate>();
        private Dictionary<string, int> _intGlobals = new Dictionary<string, int>();
        private Dictionary<string, float> _floatGlobals = new Dictionary<string, float>();
        private Dictionary<string, Vector4> _vectorGlobals = new Dictionary<string, Vector4>();

        public void AddBuffer(string bufferName, MyComputeBufferId bufferId, List<MyKernelHandle> handles)
        {
            _bufferUsages.Add(new MyComputeBufferUsageTemplate(bufferName, bufferId, handles));
        }

        public void AddTexture(string textureName, MyComputeShaderTextureId textureId, List<MyKernelHandle> handles)
        {
            _textureUsages.Add(new MyRenderTextureUsageTemplate(textureName, textureId, handles));
        }

        public void AddGlobalUniform(string name, float value)
        {
            _floatGlobals[name] = value;
        }

        public void AddGlobalUniform(string name, int value)
        {
            _intGlobals[name] = value;
        }

        public void AddGlobalUniform(string name, Vector4 value)
        {
            _vectorGlobals[name] = value;
        }

        public List<MyComputeBufferUsageTemplate> BufferUsages => _bufferUsages;

        public List<MyRenderTextureUsageTemplate> TextureUsages => _textureUsages;

        public Dictionary<string, int> IntGlobals => _intGlobals;

        public Dictionary<string, float> FloatGlobals => _floatGlobals;

        public Dictionary<string, Vector4> VectorGlobals => _vectorGlobals;
    }
}