using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.Curvature;
using Assets.NPR.Lines;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    public class NPRPrincipalCurvatureAssetGenerator : INprShaderBufferAssetGenerator
    {
        public ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers)
        {
            var generator = new NPRPrincipalCurvatureBufferGenerator();

            var array = generator.Generate(mesh);

            var buffer = ScriptableObject.CreateInstance<ShaderBufferSE>();
            buffer.Data = array;
            MyAssetDatabase.CreateAndSaveAsset(buffer, path);

            var computeBuffer = new ComputeBuffer(array.Length/8, sizeof(float) * 8);
            computeBuffer.SetData(buffer.Data);
            return computeBuffer;
        }

        public List<MyShaderBufferType> RequiredBuffers => new List<MyShaderBufferType>();
    }
}
