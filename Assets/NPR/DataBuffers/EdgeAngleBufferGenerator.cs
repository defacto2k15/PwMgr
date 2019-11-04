using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.NPR.Lines;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    public class EdgeAngleBufferGenerator
    {
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public EdgeAngleBufferGenerator(UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<float[]> Generate(Mesh mesh, ComputeBuffer adjacencyBuffer)
        {
            var generator = new BufferGeneratorUsingComputeShader("npr_edgeAngleBufferGenerator_comp", _shaderExecutorObject);
            var array = await generator.Generate(mesh, "OutEdgeAngle", 1, mesh.vertices.Length,
                new Dictionary<string, ComputeBuffer>() {{"Adjacency", adjacencyBuffer}});
            return array;
        }
    }

    public class EdgeAngleAssetGenerator : INprShaderBufferAssetGenerator
    {
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public EdgeAngleAssetGenerator(UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderExecutorObject = shaderExecutorObject;
        }

        public ComputeBuffer CreateAndSave(string path, Mesh mesh, Dictionary<MyShaderBufferType, ComputeBuffer> createdBuffers)
        {
            var bufferGenerator = new EdgeAngleBufferGenerator(_shaderExecutorObject);

            var dataArray = bufferGenerator.Generate(mesh, createdBuffers[MyShaderBufferType.Adjacency]).Result;

            var buffer = ScriptableObject.CreateInstance<ShaderBufferSE>();
            buffer.Data = dataArray;
            MyAssetDatabase.CreateAndSaveAsset(buffer, path);

            var computeBuffer = new ComputeBuffer(dataArray.Length/1, sizeof(float) * 1);
            computeBuffer.SetData(buffer.Data);
            return computeBuffer;
        }

        public List<MyShaderBufferType> RequiredBuffers => new List<MyShaderBufferType>() { MyShaderBufferType.Adjacency};
    }
}