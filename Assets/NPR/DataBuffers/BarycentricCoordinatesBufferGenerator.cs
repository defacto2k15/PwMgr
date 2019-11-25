using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.DataBuffers
{
    public class BarycentricCoordinatesBufferGenerator
    {
        private readonly string _shaderName;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public BarycentricCoordinatesBufferGenerator(String shaderName, UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderName = shaderName;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<float[]> Generate(Mesh mesh)
        {
            var generator = new BufferGeneratorUsingComputeShader(_shaderName, _shaderExecutorObject);
            var array = await generator.Generate(mesh, "OutBarycentric", 2, mesh.vertices.Length);
            return array;
        }
    }


    public class BufferGeneratorUsingComputeShader
    {
        private readonly string _shaderName;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public BufferGeneratorUsingComputeShader(String shaderName, UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderName = shaderName;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<float[]> Generate(Mesh mesh, string outBufferName, int outBufferStrideInFloat, int outBufferCount,  Dictionary<string, ComputeBuffer> additionalComputeBuffers = null, bool meshMustbeUniquefied=true)
        {
            // UWAGA. Program shadera uruchamiany po raz dla każdego trójkąta
            if (meshMustbeUniquefied)
            {
                Preconditions.Assert(mesh.vertices.Length == mesh.triangles.Length,
                    $"Vertices count (${mesh.vertices.Length}) is not equal to triangles array count (${mesh.triangles.Length}). Vertices are most propably shared. Uniquefy them!");
            }

            if (additionalComputeBuffers == null)
            {
                additionalComputeBuffers = new Dictionary<string, ComputeBuffer>();
            }

            var parametersContainer = new ComputeShaderParametersContainer();

            MultistepComputeShader barycentricGeneratorShader =
                new MultistepComputeShader(ComputeShaderUtils.LoadComputeShader(_shaderName), new IntVector2(mesh.triangles.Length/3, 1));

            var kernel = barycentricGeneratorShader.AddKernel("CS_Main");
            var allKernels = new List<MyKernelHandle>()
            {
                kernel
            };

            var triangleBufferTemplate = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                BufferData = mesh.triangles,
                Count = mesh.triangles.Length,
                Stride = sizeof(int),
                Type = ComputeBufferType.Default
            });
            barycentricGeneratorShader.SetBuffer("Triangles", triangleBufferTemplate, allKernels);

            var vertexBufferTemplate = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                BufferData = mesh.vertices,
                Count = mesh.vertices.Length,
                Stride = sizeof(float) * 3,
                Type = ComputeBufferType.Default
            });
            barycentricGeneratorShader.SetBuffer("Vertices", vertexBufferTemplate, allKernels);

            var normalsBufferTemplate = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                BufferData = mesh.normals,
                Count = mesh.normals.Length,
                Stride = sizeof(float) * 3,
                Type = ComputeBufferType.Default
            });
            barycentricGeneratorShader.SetBuffer("Normals", normalsBufferTemplate, allKernels);

            var outBarycentricBufferTemplate = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = outBufferCount,
                Stride = sizeof(float) * outBufferStrideInFloat,
                Type = ComputeBufferType.Default
            });
            barycentricGeneratorShader.SetBuffer(outBufferName, outBarycentricBufferTemplate, allKernels);

            foreach (var pair in additionalComputeBuffers)
            {
                var id = parametersContainer.AddExistingComputeBuffer(pair.Value);
                barycentricGeneratorShader.SetBuffer(pair.Key, id, allKernels);
            }

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters( new List<MyComputeBufferId> { outBarycentricBufferTemplate});
            await _shaderExecutorObject.AddOrder(new ComputeShaderOrder()
            {
                ParametersContainer = parametersContainer,
                OutParameters = outParameters,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = barycentricGeneratorShader, 
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = allKernels
                            }
                        }
                    },
                }
            });
            DebugCheckVariables.Check(outParameters);
            float[] outArray = new float[outBufferStrideInFloat * outBufferCount];
            outParameters.CreatedBuffers[outBarycentricBufferTemplate].GetData(outArray);
            return outArray;
        }
    }
}
