using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class ThermalErosionTerrainFeatureApplier : ITerrainFeatureApplier
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private CommonExecutorUTProxy _commonExecutor;
        private Dictionary<TerrainCardinalResolution, ThermalErosionTerrainFeatureApplierConfiguration> _configurations;

        public ThermalErosionTerrainFeatureApplier(ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject, CommonExecutorUTProxy commonExecutor,
            Dictionary<TerrainCardinalResolution, ThermalErosionTerrainFeatureApplierConfiguration> configurations)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
            _commonExecutor = commonExecutor;
            _configurations = configurations;
        }

        public async Task<TextureWithCoords> ApplyFeatureAsync(TextureWithCoords texture,
            TerrainCardinalResolution resolution, bool canMultistep)
        {
            IntVector2 textureSize = texture.TextureSize;
            ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer();

            var heightComputeBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 4,
                Type = ComputeBufferType.Default
            });

            var outRenderTexture = parametersContainer.AddComputeShaderTextureTemplate(
                new MyComputeShaderTextureTemplate()
                {
                    Size = textureSize,
                    Depth = 24,
                    EnableReadWrite = true,
                    Format = RenderTextureFormat.RFloat,
                    TexWrapMode = TextureWrapMode.Clamp
                });


            MultistepComputeShader transferComputeShader =
                new MultistepComputeShader(_computeShaderContainer.HeightTransferShaderPlain, textureSize);

            var textureToBufferKernel = transferComputeShader.AddKernel("CSHeightTransform_InputTextureToBuffer");
            var bufferToTextureKernel = transferComputeShader.AddKernel("CSHeightTransform_BufferToOutputTexture");
            transferComputeShader.SetGlobalUniform("g_sideLength", textureSize.X);

            var inputHeightTexture = parametersContainer.AddExistingComputeShaderTexture(texture.Texture);
            transferComputeShader.SetTexture("InputHeightTexture", inputHeightTexture,
                new List<MyKernelHandle>() {textureToBufferKernel});

            transferComputeShader.SetTexture("OutputHeightTexture", outRenderTexture,
                new List<MyKernelHandle>() {bufferToTextureKernel});

            transferComputeShader.SetBuffer("HeightBuffer", heightComputeBuffer,
                new List<MyKernelHandle>() {textureToBufferKernel, bufferToTextureKernel});
            //////////////////////////////

            MultistepComputeShader thermalErosionComputeShader =
                new MultistepComputeShader(_computeShaderContainer.ThermalErosionShader, textureSize);
            var kernel1 = thermalErosionComputeShader.AddKernel("CSThermal_Precalculation");
            var kernel2 = thermalErosionComputeShader.AddKernel("CSThermal_Erosion");
            var kernel3 = thermalErosionComputeShader.AddKernel("CSThermal_TransferHeightToTexture");

            var configuration = _configurations[resolution];
            thermalErosionComputeShader.SetGlobalUniform("g_tParam", configuration.TParam);
            thermalErosionComputeShader.SetGlobalUniform("g_cParam", configuration.CParam);
            thermalErosionComputeShader.SetGlobalUniform("g_sideLength", textureSize.X);

            thermalErosionComputeShader.SetTexture("InputHeightTexture", inputHeightTexture,
                new List<MyKernelHandle>() {kernel1});

            thermalErosionComputeShader.SetBuffer("HeightBuffer0", heightComputeBuffer,
                new List<MyKernelHandle>() {kernel1, kernel2, kernel3});

            var MyHeightBuffer1 = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 4,
                Type = ComputeBufferType.Default
            });
            thermalErosionComputeShader.SetBuffer("HeightBuffer1", MyHeightBuffer1,
                new List<MyKernelHandle>() {kernel1, kernel2});

            thermalErosionComputeShader.SetTexture("OutputHeightSum", outRenderTexture,
                new List<MyKernelHandle>() {kernel1, kernel2, kernel3});

            var MyMidTextureBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 8 * 2,
                Type = ComputeBufferType.Default
            });
            thermalErosionComputeShader.SetBuffer("MidTextureBuffer", MyMidTextureBuffer,
                new List<MyKernelHandle>() {kernel1, kernel2});

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                new List<MyComputeShaderTextureId>()
                {
                    outRenderTexture
                });

            await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                OutParameters = outParameters,
                ParametersContainer = parametersContainer,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = transferComputeShader,
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = new List<MyKernelHandle>() {textureToBufferKernel}
                            }
                        }
                    },
                    new ComputeShaderWorkPack()
                    {
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 30,
                                KernelHandles = new List<MyKernelHandle>() {kernel1, kernel2}
                            },
                        },
                        Shader = thermalErosionComputeShader
                    },
                    new ComputeShaderWorkPack()
                    {
                        Shader = transferComputeShader,
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = new List<MyKernelHandle>() {bufferToTextureKernel}
                            }
                        }
                    },
                }
            });


            return new TextureWithCoords(sizedTexture: new TextureWithSize()
            {
                Texture = outParameters.RetriveTexture(outRenderTexture),
                Size = texture.TextureSize
            }, coords: texture.Coords);
        }
    }

    public class ThermalErosionTerrainFeatureApplierConfiguration
    {
        public float TParam = 0.003f;
        public float CParam = 0.04f;
    }
}