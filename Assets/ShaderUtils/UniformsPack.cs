using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass;
using Assets.Ring2.Devising;
using UnityEngine;

namespace Assets.ShaderUtils
{
    public class UniformsPack
    {
        private readonly  Dictionary<string, ShaderUniform<float>> _floatUniforms =
            new Dictionary<string, ShaderUniform<float>>();

        private readonly  Dictionary<string, ShaderUniform<Vector4>> _vector4Uniforms =
            new Dictionary<string, ShaderUniform<Vector4>>();

        private readonly  Dictionary<string, ShaderUniform<Vector4[]>> _vector4ArrayUniforms =
            new Dictionary<string, ShaderUniform<Vector4[]>>();

        private readonly  Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();

        public UniformsPack()
        {
        }

        public UniformsPack(
            Dictionary<string, ShaderUniform<float>> floatUniforms,
            Dictionary<string, ShaderUniform<Vector4>> vector4Uniforms,
            Dictionary<string, ShaderUniform<Vector4[]>> vector4ArrayUniforms,
            Dictionary<string, Texture> textures
        )
        {
            _floatUniforms = floatUniforms;
            _vector4Uniforms = vector4Uniforms;
            _vector4ArrayUniforms = vector4ArrayUniforms;
            _textures = textures;
        }

        public void SetUniform(string name, float value)
        {
            _floatUniforms[name] = new ShaderUniform<float>(name, value);
        }

        public void SetUniform(string name, Vector4 value)
        {
            _vector4Uniforms[name] = new ShaderUniform<Vector4>(name, value);
        }

        public void SetUniform(string name, Vector4[] value)
        {
            _vector4ArrayUniforms[name] = new ShaderUniform<Vector4[]>(name, value);
        }

        public void SetUniformsToMaterial(Material material)
        {
            foreach (var uniform in _floatUniforms.Values)
            {
                material.SetFloat(uniform.Name, uniform.Get());
            }
            foreach (var uniform in _vector4Uniforms.Values)
            {
                material.SetVector(uniform.Name, uniform.Get());
            }
            foreach (var texturePair in _textures)
            {
                material.SetTexture(texturePair.Key, texturePair.Value);
            }
            foreach (var uniform in _vector4ArrayUniforms.Values)
            {
                material.SetVectorArray(uniform.Name, uniform.Get());
            }
        }

        public void SetTexture(string name, Texture texture)
        {
            _textures[name] = texture;
        }

        public Dictionary<string, ShaderUniform<float>> FloatUniforms => _floatUniforms;

        public Dictionary<string, ShaderUniform<Vector4>> Vector4Uniforms => _vector4Uniforms;

        public Dictionary<string, ShaderUniform<Vector4[]>> Vector4ArrayUniforms => _vector4ArrayUniforms;

        public Dictionary<string, Texture> Textures => _textures;

        public void MergeWith(UniformsPack otherPack)
        {
            foreach (var uniform in otherPack._floatUniforms.Values)
            {
                SetUniform(uniform.Name, uniform.Get());
            }
            foreach (var uniform in otherPack._vector4Uniforms.Values)
            {
                SetUniform(uniform.Name, uniform.Get());
            }
            foreach (var texturePair in otherPack._textures)
            {
                SetTexture(texturePair.Key, texturePair.Value);
            }

            foreach (var texturePair in otherPack._vector4ArrayUniforms)
            {
                SetUniform(texturePair.Key, texturePair.Value.Get());
            }
        }

        public UniformsPack Clone()
        {
            var newPack = new UniformsPack();
            newPack.MergeWith(this);
            return newPack;
        }

        public MaterialPropertyBlockTemplate ToPropertyBlockTemplate()
        {
            var outTemplate = new MaterialPropertyBlockTemplate();
            foreach (var uniform in _floatUniforms.Values)
            {
                outTemplate.SetFloat(uniform.Name, uniform.Get());
            }
            foreach (var uniform in _vector4Uniforms.Values)
            {
                outTemplate.SetVector(uniform.Name, uniform.Get());
            }
            foreach (var texturePair in _textures)
            {
                outTemplate.SetTexture(texturePair.Key, texturePair.Value);
            }
            foreach (var uniform in _vector4ArrayUniforms.Values)
            {
                outTemplate.SetVectorArray(uniform.Name, uniform.Get());
            }

            return outTemplate;
        }

        public static UniformsPack MergeTwo(UniformsPack main, UniformsPack additional)
        {
            var pack = additional.Clone();
            pack.MergeWith(main);
            return pack;
        }

        public UniformsPack WithoutDebugUniforms()
        {
            return new UniformsPack(
                FloatUniforms.Where(c => !c.Key.Contains("Debug")).ToDictionary(c => c.Key, c => c.Value),
                Vector4Uniforms.Where(c => !c.Key.Contains("Debug")).ToDictionary(c => c.Key, c => c.Value),
                Vector4ArrayUniforms.Where(c => !c.Key.Contains("Debug")).ToDictionary(c => c.Key, c => c.Value),
                Textures.Where(c => !c.Key.Contains("Debug")).ToDictionary(c => c.Key, c => c.Value));
        }
    }
}