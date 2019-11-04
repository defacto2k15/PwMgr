using System.Collections.Generic;
using Assets.NPR.Lines;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    public class NprTrimeshPrincipalCurvatureAssetGenerator : INprShaderBufferAssetGenerator
    {
        public ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers)
        {
            var generator = new NPRTrimeshPrincipalCurvatureBufferGenerator();

            var array = generator.Generate(mesh);

            var buffer = ScriptableObject.CreateInstance<ShaderBufferSE>();
            buffer.Data = array;
            MyAssetDatabase.CreateAndSaveAsset(buffer, path);

            var floatsPerVertex = (1 + 3 + 1 + 3 + 4);
            var computeBuffer = new ComputeBuffer(array.Length/floatsPerVertex, sizeof(float) * floatsPerVertex);
            computeBuffer.SetData(buffer.Data);
            return computeBuffer;
        }

        public List<MyShaderBufferType> RequiredBuffers => new List<MyShaderBufferType>();
    }
}