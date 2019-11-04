using System;
using System.Collections.Generic;
using Assets.Grass;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread
{
    public class GpuInstancingUniformsArray
    {
        private Dictionary<string, float[]> _floatUniforms = new Dictionary<string, float[]>();
        private Dictionary<string, Vector4[]> _vector4Uniforms = new Dictionary<string, Vector4[]>();
        private Dictionary<string, Texture> _textureUniforms = new Dictionary<string, Texture>();

        public GpuInstancingUniformsArray(GpuInstancingUniformsArrayTemplate template)
        {
            foreach (var uniformTemplate in template.UniformTemplates)
            {
                var type = uniformTemplate.Type;
                var name = uniformTemplate.Name;
                if (type == GpuInstancingUniformType.Float)
                {
                    _floatUniforms[name] = new float[MyConstants.MaxInstancesPerPack];
                }
                else if (type == GpuInstancingUniformType.Texture)
                {
                }
                else if (type == GpuInstancingUniformType.Vector4)
                {
                    _vector4Uniforms[name] = new Vector4[MyConstants.MaxInstancesPerPack];
                }
            }
        }

        public void SetUniform(int index, UniformsPack pack)
        {
            foreach (var floatUniform in pack.FloatUniforms)
            {
                _floatUniforms[floatUniform.Value.Name][index] = floatUniform.Value.Get();
            }
            foreach (var vector4Uniform in pack.Vector4Uniforms)
            {
                _vector4Uniforms[vector4Uniform.Value.Name][index] = vector4Uniform.Value.Get();
            }
            foreach (var textureUniform in pack.Textures)
            {
                _textureUniforms[textureUniform.Key] = textureUniform.Value;
            }
        }

        public void Move(int sourceIndex, int destIndex)
        {
            foreach (var array in _floatUniforms.Values)
            {
                array[sourceIndex] = array[destIndex];
            }
            foreach (var array in _vector4Uniforms.Values)
            {
                array[sourceIndex] = array[destIndex];
            }
        }

        public void FillMaterialPropertyBlock(RenderingDataOfBlockReciever reciever)
        {
            foreach (var floatArray in _floatUniforms)
            {
                reciever.SetBlockFloatArray(floatArray.Key, floatArray.Value);
            }
            foreach (var vectorArray in _vector4Uniforms)
            {
                reciever.SetBlockVectorArray(vectorArray.Key, vectorArray.Value);
            }
            foreach (var textureInfo in _textureUniforms)
            {
                reciever.SetBlockTexture(textureInfo.Key, textureInfo.Value);
            }
        }

        public void MoveToOtherArray(int thisBlockCellIndex, GpuInstancingUniformsArray otherUniformsArray,
            int otherBlockCellIndex)
        {
            foreach (var floatName in _floatUniforms.Keys)
            {
                Preconditions.Assert(otherUniformsArray._floatUniforms.ContainsKey(floatName),
                    "In target array there is not floatArray of name " + floatName);
                var thisFloatArray = _floatUniforms[floatName];
                var thatFloatArray = otherUniformsArray._floatUniforms[floatName];
                thisFloatArray[thisBlockCellIndex] = thatFloatArray[otherBlockCellIndex];
            }
            foreach (var vector4Name in _vector4Uniforms.Keys)
            {
                Preconditions.Assert(otherUniformsArray._vector4Uniforms.ContainsKey(vector4Name),
                    "In target array there is not vector4Array of name " + vector4Name);
                var thisVector4Array = _vector4Uniforms[vector4Name];
                var thatVector4Array = otherUniformsArray._vector4Uniforms[vector4Name];
                thisVector4Array[thisBlockCellIndex] = thatVector4Array[otherBlockCellIndex];
            }
        }
    }
}