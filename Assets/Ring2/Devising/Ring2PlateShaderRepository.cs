using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2PlateShaderRepository
    {
        private Dictionary<string, Shader> _shaders;

        public Ring2PlateShaderRepository(Dictionary<string, Shader> shaders)
        {
            _shaders = shaders;
        }

        public Shader GetShader(string name)
        {
            Preconditions.Assert(_shaders.ContainsKey(name), "There is no shader of name " + name);
            return _shaders[name];
        }

        public static Ring2PlateShaderRepository Create()
        {
            var dict = Ring2ShaderNames.ShaderNames.ToDictionary(name => name, name => Shader.Find(name));
            return new Ring2PlateShaderRepository(dict);
        }
    }
}