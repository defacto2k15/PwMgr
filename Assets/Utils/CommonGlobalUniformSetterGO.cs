using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils
{
    public class CommonGlobalUniformSetterGO : MonoBehaviour
    {
        public string UniformName;
        [Range(0, 1)] public float UniformValue;

        public void Update()
        {
            if (!string.IsNullOrEmpty(UniformName))
            {
                Shader.SetGlobalFloat(UniformName, UniformValue);
            }
        }

    }
}
