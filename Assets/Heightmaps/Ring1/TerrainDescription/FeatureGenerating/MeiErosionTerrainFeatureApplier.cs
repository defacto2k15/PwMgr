using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class MeiErosionTerrainFeatureApplier : ITerrainFeatureApplier
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public MeiErosionTerrainFeatureApplier(ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public async Task<TextureWithCoords> ApplyFeatureAsync(TextureWithCoords texture,
            TerrainCardinalResolution resolution,
            bool canMultistep)
        {
            //IntVector2 textureSize = texture.TextureSize;

            //var heightComputeBuffer = new ComputeBuffer(textureSize.X * textureSize.Y, 4, ComputeBufferType.Default);
            //var outRenderTexture = new RenderTexture(textureSize.X, textureSize.Y, 24, RenderTextureFormat.ARGB32);
            //outRenderTexture.enableRandomWrite = true;
            //outRenderTexture.Create();

            ////MultistepComputeShader transferComputeShader =
            ////    new MultistepComputeShader(_computeShaderContainer.HeightTransferShader, textureSize, 8);
            ////var textureToBufferKernel = transferComputeShader.AddKernel("CSHeightTransform_InputTextureToBuffer");
            ////var bufferToTextureKernel = transferComputeShader.AddKernel("CSHeightTransform_BufferToOutputTexture");
            ////transferComputeShader.SetGlobalUniform("g_sideLength", textureSize.X);

            //var inputHeightArray = HeightmapUtils.CreateHeightmapArrayFromTexture(texture.Texture as Texture2D);
            //var inExtremes = MyArrayUtils.CalculateExtremes(inputHeightArray.HeightmapAsArray);
            //MyArrayUtils.Normalize(inputHeightArray.HeightmapAsArray, inExtremes);
            //MyArrayUtils.Multiply(inputHeightArray.HeightmapAsArray, 800f);

            ////var transferInputHeightTexture = new MyRenderTexture("InputHeightTexture", texture.Texture);
            ////transferComputeShader.SetTexture(transferInputHeightTexture,
            ////    new List<MyKernelHandle>() {textureToBufferKernel});

            ////var transferOutputHeightTexture = new MyRenderTexture("OutputHeightTexture", outRenderTexture);
            ////transferComputeShader.SetTexture(transferOutputHeightTexture,
            ////    new List<MyKernelHandle>() {bufferToTextureKernel});

            ////var transferHeightBuffer = new MyComputeBuffer("HeightBuffer", heightComputeBuffer);
            ////transferComputeShader.SetBuffer(transferHeightBuffer,
            ////    new List<MyKernelHandle>() {textureToBufferKernel, bufferToTextureKernel});
            ////////////////////////////////


            //MultistepComputeShader computeShader =
            //    new MultistepComputeShader(_computeShaderContainer.MeiErosionShader, textureSize, 8);

            //var kernel_bufferInitialization = computeShader.AddKernel("CSMei_InitializeBuffers");
            //var kernel_waterIncrement = computeShader.AddKernel("CSMei_WaterIncrement");
            //var kernel_flowSimulation = computeShader.AddKernel("CSMei_FlowSimulation");
            //var kernel_velocityCalculation = computeShader.AddKernel("CSMei_VelocityCalculation");
            //var kernel_sedimentCalculation = computeShader.AddKernel("CSMei_SedimentCalculation");
            //var kernel_sedimentTransportation = computeShader.AddKernel("CSMei_SedimentTransportation");
            //var kernel_evaporation = computeShader.AddKernel("CSMei_Evaporation");


            //int StepCount = 50;
            //float A_PipeCrossSection = 0.05f;
            //float ConstantWaterAdding = 1f / 64;
            //float GravityAcceleration = 9.81f;
            //float DeltaT = 1f;
            //float DepositionConstant = 0.0001f * 12;
            //float DissolvingConstant = 0.0001f * 12;
            //float EvaporationConstant = 0.05f * 10;
            //Vector2 GridSize = new Vector2(1, 1);
            //float L_PipeLength = 1;
            //float SedimentCapacityConstant = 150;

            //computeShader.SetGlobalUniform("g_sideLength", textureSize.X);
            //computeShader.SetGlobalUniform("deltaT", DeltaT);
            //computeShader.SetGlobalUniform("constantWaterAdding", ConstantWaterAdding);
            //computeShader.SetGlobalUniform("A_pipeCrossSection", A_PipeCrossSection);
            //computeShader.SetGlobalUniform("l_pipeLength", L_PipeLength);
            //computeShader.SetGlobalUniform("g_GravityAcceleration", GravityAcceleration);
            //computeShader.SetGlobalUniform("ks_DissolvingConstant", DissolvingConstant);
            //computeShader.SetGlobalUniform("kd_DepositionConstant", DepositionConstant);
            //computeShader.SetGlobalUniform("ke_EvaporationConstant", EvaporationConstant);
            //computeShader.SetGlobalUniform("kc_SedimentCapacityConstant", SedimentCapacityConstant);
            //computeShader.SetGlobalUniform("gridSideSize", GridSize.x);

            //var allKernels =
            //    new List<MyKernelHandle>()
            //    {
            //        kernel_waterIncrement,
            //        kernel_flowSimulation,
            //        kernel_velocityCalculation,
            //        kernel_sedimentCalculation,
            //        kernel_sedimentTransportation,
            //        kernel_evaporation,
            //        kernel_bufferInitialization
            //    };

            //var heightMap = new MyComputeBuffer("HeightMap", heightComputeBuffer);
            //heightMap.SetData(inputHeightArray.HeightmapAsArray); //TODODODOD
            //computeShader.SetBuffer(heightMap, allKernels);

            //var waterMap = new MyComputeBuffer("WaterMap", textureSize.X * textureSize.Y, 4);
            //computeShader.SetBuffer(waterMap, allKernels);

            //var waterMap1 = new MyComputeBuffer("WaterMap_1", textureSize.X * textureSize.Y, 4);
            //computeShader.SetBuffer(waterMap1, allKernels);

            //var waterMap2 = new MyComputeBuffer("WaterMap_2", textureSize.X * textureSize.Y, 4);
            //computeShader.SetBuffer(waterMap2, allKernels);

            //var fluxMap = new MyComputeBuffer("FluxMap", textureSize.X * textureSize.Y, 4 * 4);
            //computeShader.SetBuffer(fluxMap, allKernels);

            //var velocityMap = new MyComputeBuffer("VelocityMap", textureSize.X * textureSize.Y, 4 * 2);
            //computeShader.SetBuffer(velocityMap, allKernels);

            //var sedimentMap = new MyComputeBuffer("SedimentMap", textureSize.X * textureSize.Y, 4);
            //computeShader.SetBuffer(sedimentMap, allKernels);

            //var sedimentMap1 = new MyComputeBuffer("SedimentMap_1", textureSize.X * textureSize.Y, 4);
            //computeShader.SetBuffer(sedimentMap1, allKernels);

            //var DebugTexture = new MyRenderTexture("DebugTexture",
            //    new RenderTexture(textureSize.X, textureSize.Y, 24, RenderTextureFormat.ARGB32), true);
            //computeShader.SetTexture(DebugTexture, allKernels);

            //var loopedKernels =
            //    new List<MyKernelHandle>()
            //    {
            //        kernel_waterIncrement,
            //        kernel_flowSimulation,
            //        kernel_velocityCalculation,
            //        kernel_sedimentCalculation,
            //        kernel_sedimentTransportation,
            //        kernel_evaporation,
            //    };

            //await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            //{
            //    WorkPacks = new List<ComputeShaderWorkPack>()
            //    {
            //        //new ComputeShaderWorkPack()
            //        //{
            //        //    Shader    = transferComputeShader,
            //        //    DispatchLoops = new List<ComputeShaderDispatchLoop>()
            //        //    {
            //        //        new ComputeShaderDispatchLoop()
            //        //        {
            //        //            DispatchCount = 1,
            //        //            KernelHandles = new List<MyKernelHandle>() {textureToBufferKernel }
            //        //        }
            //        //    }
            //        //},
            //        new ComputeShaderWorkPack()
            //        {
            //            DispatchLoops = new List<ComputeShaderDispatchLoop>()
            //            {
            //                new ComputeShaderDispatchLoop()
            //                {
            //                    DispatchCount = StepCount,
            //                    KernelHandles = loopedKernels,

            //                }
            //            },
            //            Shader = computeShader
            //        },
            //        //new ComputeShaderWorkPack()
            //        //{
            //        //    Shader    = transferComputeShader,
            //        //    DispatchLoops = new List<ComputeShaderDispatchLoop>()
            //        //    {
            //        //        new ComputeShaderDispatchLoop()
            //        //        {
            //        //            DispatchCount = 1,
            //        //            KernelHandles = new List<MyKernelHandle>() {bufferToTextureKernel}
            //        //        }
            //        //    }
            //        //},
            //    }
            //});

            //var outHeightMap = new float[textureSize.X, textureSize.Y];
            //heightMap.GetData(outHeightMap);

            //MyArrayUtils.Multiply(outHeightMap, 1/800f);
            //MyArrayUtils.DeNormalize(outHeightMap, inExtremes);

            //var outTexture = HeightmapUtils.CreateTextureFromHeightmap(new HeightmapArray(outHeightMap));

            //return new TextureWithCoords(sizedTexture: new TextureWithSize()
            //{
            //    Texture = outTexture,
            //    Size = texture.TextureSize
            //},   coords: texture.Coords);
            throw new NotImplementedException(); //todo
        }
    }
}