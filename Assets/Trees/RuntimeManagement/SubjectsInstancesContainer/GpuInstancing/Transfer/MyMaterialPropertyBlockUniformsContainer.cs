using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class MyMaterialPropertyBlockUniformsContainer
    {
        private Dictionary<string, float[]> _floatUniforms = new Dictionary<string, float[]>();
        private Dictionary<string, Vector4[]> _vector4Uniforms = new Dictionary<string, Vector4[]>();
        private Dictionary<string, Texture> _textureUniforms = new Dictionary<string, Texture>();

        public bool AnythingThere
        {
            get { return _floatUniforms.Any() || _vector4Uniforms.Any() || _textureUniforms.Any(); }
        }

        public void SetCopyOfFloatArray(string key, float[] value)
        {
            var newArray = new float[value.Count()];
            Array.Copy(value, newArray, value.Length);
            _floatUniforms[key] = newArray;
        }

        public void SetCopyOfVectorArray(string key, Vector4[] value)
        {
            var newArray = new Vector4[value.Count()];
            Array.Copy(value, newArray, value.Length);
            _vector4Uniforms[key] = newArray;
        }

        public void SetTexture(string key, Texture value)
        {
            _textureUniforms[key] = value;
        }

        public void Clear()
        {
            _floatUniforms.Clear();
            _vector4Uniforms.Clear();
            _textureUniforms.Clear();
        }

        public void CopyMaterialsToPropertyBlock(MaterialPropertyBlock block)
        {
            foreach (var pair in _floatUniforms)
            {
                block.SetFloatArray(pair.Key, pair.Value);
            }

            foreach (var pair in _vector4Uniforms)
            {
                block.SetVectorArray(pair.Key, pair.Value);
            }

            foreach (var pair in _textureUniforms)
            {
                block.SetTexture(pair.Key, pair.Value);
            }
        }

        public void MoveData(MyMaterialPropertyBlockUniformsContainer otherBlock)
        {
            foreach (var pair in _floatUniforms)
            {
                otherBlock._floatUniforms[pair.Key] = pair.Value;
            }

            foreach (var pair in _vector4Uniforms)
            {
                otherBlock._vector4Uniforms[pair.Key] = pair.Value;
            }

            foreach (var pair in _textureUniforms)
            {
                otherBlock._textureUniforms[pair.Key] = pair.Value;
            }
        }

        public Dictionary<string, float[]> FloatUniforms
        {
            get { return _floatUniforms; }
        }

        public Dictionary<string, Vector4[]> Vector4Uniforms
        {
            get { return _vector4Uniforms; }
        }

        public Dictionary<string, Texture> TextureUniforms
        {
            get { return _textureUniforms; }
        }
    }
}