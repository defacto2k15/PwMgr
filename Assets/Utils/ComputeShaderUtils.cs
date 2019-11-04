using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils
{
    public static class ComputeShaderUtils
    {
        public static ComputeShader LoadComputeShader(string name)
        {
            var shader = (ComputeShader) Resources.Load("compute_shaders/" + name);
            Preconditions.Assert(shader != null, $"Shader of name {name} cannot be found");
            return shader;
        }
    }
}
