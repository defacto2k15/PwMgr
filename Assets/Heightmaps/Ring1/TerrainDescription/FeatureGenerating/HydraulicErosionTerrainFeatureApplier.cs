using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps.Ring1.Erosion;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class HydraulicErosionTerrainFeatureApplier : ITerrainFeatureApplier
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private Dictionary<TerrainCardinalResolution, HydraulicEroderConfiguration> _configurations;

        public HydraulicErosionTerrainFeatureApplier(ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject,
            Dictionary<TerrainCardinalResolution, HydraulicEroderConfiguration> configurations)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
            _configurations = configurations;
        }

        public async Task<TextureWithCoords> ApplyFeatureAsync(TextureWithCoords texture,
            TerrainCardinalResolution resolution, bool canMultistep)
        {
            ComputeShaderParametersContainer parametersContainer = new ComputeShaderParametersContainer();
            IntVector2 textureSize = texture.TextureSize;

            var heightComputeBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 4,
                Type = ComputeBufferType.Default
            });
            var outRenderTexture = parametersContainer.AddComputeShaderTextureTemplate(
                new MyComputeShaderTextureTemplate()
                {
                    Depth = 24,
                    EnableReadWrite = true,
                    Format = RenderTextureFormat.RFloat,
                    Size = textureSize,
                    TexWrapMode = TextureWrapMode.Clamp
                });

            MultistepComputeShader transferComputeShader =
                new MultistepComputeShader(_computeShaderContainer.HeightTransferShaderPlain, textureSize);
            var textureToBufferKernel = transferComputeShader.AddKernel("CSHeightTransform_InputTextureToBuffer");
            var bufferToTextureKernel = transferComputeShader.AddKernel("CSHeightTransform_BufferToOutputTexture");
            transferComputeShader.SetGlobalUniform("g_sideLength", textureSize.X);

            var transferInputHeightTexture = parametersContainer.AddExistingComputeShaderTexture(texture.Texture);
            transferComputeShader.SetTexture("InputHeightTexture", transferInputHeightTexture,
                new List<MyKernelHandle>() {textureToBufferKernel});

            transferComputeShader.SetTexture("OutputHeightTexture", outRenderTexture,
                new List<MyKernelHandle>() {bufferToTextureKernel});

            transferComputeShader.SetBuffer("HeightBuffer", heightComputeBuffer,
                new List<MyKernelHandle>() {textureToBufferKernel, bufferToTextureKernel});
            //////////////////////////////

            //var configuration = 
            //    new HydraulicEroderConfiguration()
            //    {
            //        StepCount = 20,
            //        kr_ConstantWaterAddition = 0.000002f,  // 0.0001f,
            //        ks_GroundToSedimentFactor = 1f,
            //        ke_WaterEvaporationFactor = 0.05f,
            //        kc_MaxSedimentationFactor = 0.8f,
            //    };
            var configuration = _configurations[resolution];

            MultistepComputeShader computeShader =
                new MultistepComputeShader(_computeShaderContainer.HydraulicErosionShader, textureSize);
            var kernel_water = computeShader.AddKernel("CSHydraulicErosion_Water");
            var kernel_erostion = computeShader.AddKernel("CSHydraulicErosion_Erosion");
            var kernel_deltaSum = computeShader.AddKernel("CSHydraulicErosion_DeltaSum");
            var kernel_clearDelta = computeShader.AddKernel("CSHydraulicErosion_ClearDelta");
            var kernel_evaporation = computeShader.AddKernel("CSHydraulicErosion_Evaporation");
            var kernel_sedimentationToGround = computeShader.AddKernel("CSHydraulicErosion_SedimentationToGround");

            computeShader.SetGlobalUniform("g_sideLength", textureSize.X);
            computeShader.SetGlobalUniform("g_krParam", configuration.kr_ConstantWaterAddition);
            computeShader.SetGlobalUniform("g_ksParam", configuration.ks_GroundToSedimentFactor);
            computeShader.SetGlobalUniform("g_keParam", configuration.ke_WaterEvaporationFactor);
            computeShader.SetGlobalUniform("g_kcParam", configuration.kc_MaxSedimentationFactor);

            var allKernels =
                new List<MyKernelHandle>()
                {
                    kernel_water,
                    kernel_erostion,
                    kernel_deltaSum,
                    kernel_clearDelta,
                    kernel_evaporation,
                    kernel_sedimentationToGround
                };

            computeShader.SetBuffer("HeightMap", heightComputeBuffer,
                new List<MyKernelHandle>()
                {
                    kernel_water,
                    kernel_erostion,
                    kernel_evaporation,
                    kernel_sedimentationToGround
                });


            var waterMap = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 4,
                Type = ComputeBufferType.Default
            });
            computeShader.SetBuffer("WaterMap", waterMap,
                new List<MyKernelHandle>()
                {
                    kernel_water,
                    kernel_erostion,
                    kernel_deltaSum,
                    kernel_evaporation,
                });

            var deltaBuffer = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 4 * 2 * 9,
                Type = ComputeBufferType.Default
            });
            computeShader.SetBuffer("DeltaBuffer", deltaBuffer,
                new List<MyKernelHandle>()
                {
                    kernel_erostion,
                    kernel_deltaSum,
                    kernel_clearDelta
                });

            var sedimentMap = parametersContainer.AddComputeBufferTemplate(new MyComputeBufferTemplate()
            {
                Count = textureSize.X * textureSize.Y,
                Stride = 4,
                Type = ComputeBufferType.Default
            });
            computeShader.SetBuffer("SedimentMap", sedimentMap,
                new List<MyKernelHandle>()
                {
                    kernel_water,
                    kernel_erostion,
                    kernel_deltaSum,
                    kernel_evaporation,
                    kernel_sedimentationToGround
                });

            var debugTexture = parametersContainer.AddComputeShaderTextureTemplate(new MyComputeShaderTextureTemplate()
            {
                Depth = 24,
                EnableReadWrite = true,
                Format = RenderTextureFormat.ARGB32,
                Size = textureSize,
                TexWrapMode = TextureWrapMode.Clamp
            });
            computeShader.SetTexture("DebugTexture", debugTexture,
                new List<MyKernelHandle>()
                {
                    kernel_water,
                    kernel_erostion,
                    kernel_deltaSum,
                    kernel_clearDelta,
                    kernel_evaporation,
                    kernel_sedimentationToGround
                });


            var loopedKernels = new List<MyKernelHandle>()
            {
                kernel_water,
                kernel_erostion,
                kernel_deltaSum,
                kernel_clearDelta,
                kernel_evaporation,
            };

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters(
                new List<MyComputeShaderTextureId>()
                {
                    outRenderTexture
                });
            await _shaderExecutorObject.AddOrder(new ComputeShaderOrder()
            {
                ParametersContainer = parametersContainer,
                OutParameters = outParameters,
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
                                DispatchCount = configuration.StepCount,
                                KernelHandles = loopedKernels,
                            },
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = new List<MyKernelHandle>() {kernel_sedimentationToGround}
                            }
                        },
                        Shader = computeShader
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
}