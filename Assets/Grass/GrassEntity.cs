using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ShaderUtils;
using Assets.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Grass
{
    internal class GrassEntity
    {
        private Matrix4x4 _localToWorldMatrix;
        private Vector3 _position;
        private Vector3 _rotation;
        private Vector3 _scale;
        private readonly List<ShaderUniform<float>> _floatUniforms = new List<ShaderUniform<float>>();
        private readonly List<ShaderUniform<Vector4>> _vector4Uniforms = new List<ShaderUniform<Vector4>>();

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                RegenerateLocalToWorldMatrix();
            }
        }

        public Vector3 Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        public Vector3 Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        private void RegenerateLocalToWorldMatrix()
        {
            _localToWorldMatrix = TransformUtils.GetLocalToWorldMatrix(_position, _rotation, _scale);
        }

        public Matrix4x4 LocalToWorldMatrix
        {
            get
            {
                RegenerateLocalToWorldMatrix();
                return _localToWorldMatrix;
            }
        }

        private Vector4 PlantDirection
        {
            get
            {
                var angle = _rotation.y;
                return new Vector4((float) Math.Sin(angle), 0, (float) Math.Cos(angle), 0).normalized;
            }
        }

        public void AddUniform(ShaderUniformName name, float value)
        {
            _floatUniforms.RemoveAll(x => x.Name.Equals(name.ToString()));
            _floatUniforms.Add(new ShaderUniform<float>(name, value));
        }

        public void AddUniform(ShaderUniformName name, Vector4 value)
        {
            _vector4Uniforms.RemoveAll(x => x.Name.Equals(name.ToString()));
            _vector4Uniforms.Add(new ShaderUniform<Vector4>(name, value));
        }

        public void AddUniform(ShaderUniformName name, Color value)
        {
            AddUniform(name, new Vector4(value.r, value.b, value.g, value.a));
        }

        public List<ShaderUniform<float>> GetFloatUniforms()
        {
            return _floatUniforms;
        }

        public List<ShaderUniform<Vector4>> GetVector4Uniforms()
        {
            return _vector4Uniforms
                .Union(new List<ShaderUniform<Vector4>>()
                {
                    new ShaderUniform<Vector4>(ShaderUniformName._PlantDirection, PlantDirection)
                })
                .ToList();
        }
    }
}